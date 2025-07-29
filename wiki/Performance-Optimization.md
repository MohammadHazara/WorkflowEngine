# Performance Optimization

This guide covers strategies for optimizing WorkflowEngine performance at scale, including database tuning, memory management, and execution optimization.

## Performance Benchmarks

### Current Performance Characteristics
- **Throughput**: 856.90 steps/second sustained execution
- **Large Workflows**: 1,000 steps executed in 1,167ms
- **Memory Efficiency**: <50MB increase for 10,000 step executions
- **Database**: SQLite optimized for embedded scenarios
- **Concurrent Execution**: 100+ workflows simultaneously

### Scaling Targets
- **Small Scale**: 1-100 workflows, <1,000 executions/day
- **Medium Scale**: 100-1,000 workflows, <10,000 executions/day
- **Large Scale**: 1,000+ workflows, 10,000+ executions/day
- **Enterprise Scale**: Custom optimization required

## Database Performance Optimization

### SQLite Optimization

#### Connection Configuration
```csharp
services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlite(connectionString, sqlite =>
    {
        sqlite.CommandTimeout(30);
    })
    .EnableSensitiveDataLogging(false)
    .EnableServiceProviderCaching()
    .EnableDetailedErrors(false));
```

#### Pragmas for Performance
```sql
PRAGMA journal_mode = WAL;           -- Write-Ahead Logging
PRAGMA synchronous = NORMAL;         -- Balanced durability/performance
PRAGMA cache_size = 10000;           -- 10MB cache
PRAGMA temp_store = MEMORY;          -- In-memory temp tables
PRAGMA mmap_size = 268435456;        -- 256MB memory map
```

#### Database Indexing
```sql
-- Optimize workflow queries
CREATE INDEX IX_Workflows_IsActive ON Workflows(IsActive);
CREATE INDEX IX_Workflows_UpdatedAt ON Workflows(UpdatedAt DESC);

-- Optimize execution queries
CREATE INDEX IX_WorkflowExecutions_Status ON WorkflowExecutions(Status);
CREATE INDEX IX_WorkflowExecutions_StartedAt ON WorkflowExecutions(StartedAt DESC);
CREATE INDEX IX_WorkflowExecutions_WorkflowId_Status ON WorkflowExecutions(WorkflowId, Status);
```

### Query Optimization

#### Read-Only Queries
```csharp
// Use AsNoTracking() for read-only operations
var workflows = await _dbContext.Workflows
    .AsNoTracking()
    .Where(w => w.IsActive)
    .OrderByDescending(w => w.UpdatedAt)
    .Take(pageSize)
    .ToListAsync();
```

#### Efficient Aggregations
```csharp
// Single query for statistics instead of multiple counts
var stats = await _dbContext.WorkflowExecutions
    .AsNoTracking()
    .GroupBy(we => 1)
    .Select(g => new
    {
        Total = g.Count(),
        Running = g.Count(we => we.Status == WorkflowExecutionStatus.Running),
        Completed = g.Count(we => we.Status == WorkflowExecutionStatus.Completed),
        Failed = g.Count(we => we.Status == WorkflowExecutionStatus.Failed),
        AverageTimeMs = g.Where(we => we.DurationMs.HasValue)
                         .Average(we => (double?)we.DurationMs) ?? 0
    })
    .FirstOrDefaultAsync();
```

#### Pagination Optimization
```csharp
// Efficient pagination with offset
var skip = (page - 1) * pageSize;
var items = await query
    .Skip(skip)
    .Take(pageSize)
    .ToListAsync();

// Count optimization for large datasets
var totalCount = await query.CountAsync();
```

### Entity Framework Optimization

#### DbContext Configuration
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseSqlite(connectionString)
        .EnableSensitiveDataLogging(false)
        .EnableServiceProviderCaching()
        .ConfigureWarnings(warnings => 
            warnings.Ignore(RelationalEventId.AmbientTransactionWarning));
}
```

#### Change Tracking Optimization
```csharp
// Disable change tracking for read operations
_dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

// Enable for specific operations only
var workflow = await _dbContext.Workflows
    .AsTracking()
    .FirstAsync(w => w.Id == id);
```

## Memory Management

### Workflow Execution Optimization

#### Efficient Step Processing
```csharp
public sealed class OptimizedWorkflowExecutor : IWorkflowExecutor
{
    private readonly SemaphoreSlim _concurrencyLimit;
    
    public OptimizedWorkflowExecutor()
    {
        // Limit concurrent executions based on system resources
        var maxConcurrency = Environment.ProcessorCount * 2;
        _concurrencyLimit = new SemaphoreSlim(maxConcurrency);
    }

    public async Task<bool> ExecuteWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        await _concurrencyLimit.WaitAsync(cancellationToken);
        try
        {
            return await ExecuteStepsSequentiallyAsync(workflow, cancellationToken);
        }
        finally
        {
            _concurrencyLimit.Release();
        }
    }
}
```

#### Memory-Efficient Collections
```csharp
// Use collections with known capacity
var steps = new List<Step>(workflow.EstimatedStepCount);

// Use ArrayPool for temporary arrays
var pool = ArrayPool<byte>.Shared;
var buffer = pool.Rent(minimumLength);
try
{
    // Use buffer
}
finally
{
    pool.Return(buffer);
}
```

#### Disposal Pattern
```csharp
public sealed class WorkflowExecutor : IWorkflowExecutor, IDisposable
{
    private readonly Dictionary<int, CancellationTokenSource> _runningExecutions;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        
        foreach (var cts in _runningExecutions.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        
        _runningExecutions.Clear();
        _disposed = true;
    }
}
```

### Garbage Collection Optimization

#### Object Pooling
```csharp
public sealed class WorkflowExecutionPool : ObjectPool<WorkflowExecution>
{
    public override WorkflowExecution Get()
    {
        var execution = base.Get();
        execution.Reset(); // Reset to default state
        return execution;
    }

    public override void Return(WorkflowExecution obj)
    {
        obj.Reset();
        base.Return(obj);
    }
}
```

#### Reduce Allocations
```csharp
// Use StringBuilder for string concatenation
private static readonly StringBuilder _stringBuilder = new(1024);

// Use stackalloc for small arrays
Span<int> buffer = stackalloc int[16];

// Reuse delegates
private static readonly Func<Step, bool> _activeStepPredicate = s => s.IsActive;
```

## Application Performance Optimization

### Async/Await Best Practices

#### ConfigureAwait Usage
```csharp
public async Task<bool> ExecuteStepAsync(Step step, CancellationToken cancellationToken)
{
    // Use ConfigureAwait(false) in library code
    await SomeAsyncOperation().ConfigureAwait(false);
    
    // Process result
    return ProcessStepResult();
}
```

#### Task Parallelization
```csharp
public async Task<bool> ExecuteParallelStepsAsync(IEnumerable<Step> steps, CancellationToken cancellationToken)
{
    var tasks = steps.Select(step => ExecuteStepAsync(step, cancellationToken));
    var results = await Task.WhenAll(tasks).ConfigureAwait(false);
    
    return results.All(result => result);
}
```

#### Efficient Task Creation
```csharp
// Avoid unnecessary Task.Run for CPU-bound work in async context
public async Task<bool> ProcessDataAsync(byte[] data)
{
    // CPU-bound work - use Task.Run if necessary
    var result = await Task.Run(() => ProcessDataSync(data)).ConfigureAwait(false);
    
    // I/O-bound work - use async methods directly
    await SaveResultAsync(result).ConfigureAwait(false);
    
    return true;
}
```

### Caching Strategies

#### In-Memory Caching
```csharp
public sealed class CachedDashboardService : IDashboardService
{
    private readonly IDashboardService _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync("dashboard-stats", async entry =>
        {
            entry.SlidingExpiration = _cacheExpiry;
            return await _inner.GetDashboardStatsAsync(cancellationToken);
        });
    }
}
```

#### Distributed Caching (Redis)
```csharp
public sealed class DistributedCachedService : IDashboardService
{
    private readonly IDistributedCache _distributedCache;
    private readonly JsonSerializerOptions _jsonOptions;

    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "dashboard-stats";
        var cachedJson = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
        
        if (cachedJson is not null)
        {
            return JsonSerializer.Deserialize<DashboardStats>(cachedJson, _jsonOptions)!;
        }

        var stats = await _inner.GetDashboardStatsAsync(cancellationToken);
        var json = JsonSerializer.Serialize(stats, _jsonOptions);
        
        await _distributedCache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        }, cancellationToken);

        return stats;
    }
}
```

## Scaling Strategies

### Horizontal Scaling

#### Load Balancing Configuration
```csharp
// Configure for multiple instances
services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Session state for load balancing
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

#### Database Connection Pooling
```csharp
services.AddDbContext<WorkflowDbContext>(options =>
{
    options.UseSqlite(connectionString, sqlite =>
    {
        sqlite.CommandTimeout(30);
    });
}, ServiceLifetime.Scoped); // Proper lifetime management
```

### Vertical Scaling

#### CPU Optimization
```csharp
// Configure task scheduler for CPU-intensive work
var scheduler = new LimitedConcurrencyLevelTaskScheduler(Environment.ProcessorCount);
var factory = new TaskFactory(scheduler);

// Use for CPU-bound workflow steps
await factory.StartNew(() => ProcessCpuIntensiveStep(step), cancellationToken);
```

#### Memory Optimization
```csharp
// Monitor memory usage
public sealed class MemoryMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var memoryUsed = GC.GetTotalMemory(false);
            
            if (memoryUsed > MaxMemoryThreshold)
            {
                GC.Collect(); // Force collection if needed
                GC.WaitForPendingFinalizers();
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## Frontend Performance

### JavaScript Optimization

#### Efficient DOM Updates
```javascript
// Batch DOM updates
function updateDashboardStats(stats) {
    const fragment = document.createDocumentFragment();
    
    // Update all elements in memory first
    updateElement(fragment, 'total-workflows', stats.totalWorkflows);
    updateElement(fragment, 'active-workflows', stats.activeWorkflows);
    updateElement(fragment, 'running-executions', stats.runningExecutions);
    updateElement(fragment, 'success-rate', stats.successRate + '%');
    
    // Single DOM update
    document.getElementById('stats-container').appendChild(fragment);
}
```

#### Debounced API Calls
```javascript
const debouncedRefresh = debounce(async () => {
    await Promise.all([
        updateDashboardStats(),
        updateRecentExecutions(),
        updateWorkflowsList()
    ]);
}, 300);

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}
```

### Network Optimization

#### Request Batching
```javascript
// Combine multiple API calls
async function fetchDashboardData() {
    const [stats, executions, workflows] = await Promise.all([
        fetch('/api/dashboard/stats').then(r => r.json()),
        fetch('/api/dashboard/recent-executions').then(r => r.json()),
        fetch('/api/workflows?page=1&pageSize=10').then(r => r.json())
    ]);
    
    return { stats, executions, workflows };
}
```

#### Response Compression
```csharp
// Enable response compression
services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.EnableForHttps = true;
});
```

## Monitoring and Profiling

### Performance Metrics

#### Custom Metrics Collection
```csharp
public sealed class PerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly ConcurrentDictionary<string, PerformanceCounter> _counters;

    public void RecordExecutionTime(string operationName, TimeSpan duration)
    {
        _counters.AddOrUpdate(operationName, 
            new PerformanceCounter(operationName, duration),
            (key, existing) => existing.AddSample(duration));
    }

    public void LogPerformanceReport()
    {
        foreach (var counter in _counters.Values)
        {
            _logger.LogInformation(
                "Operation: {Operation}, Average: {Average}ms, Count: {Count}",
                counter.Name, counter.AverageMs, counter.SampleCount);
        }
    }
}
```

#### Health Checks
```csharp
services.AddHealthChecks()
    .AddDbContextCheck<WorkflowDbContext>()
    .AddCheck<WorkflowExecutorHealthCheck>("workflow-executor")
    .AddCheck<MemoryHealthCheck>("memory-usage");
```

### Profiling Tools

#### Built-in Diagnostics
```csharp
// Enable diagnostics in development
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Add diagnostic middleware
    app.UseMiddleware<PerformanceDiagnosticsMiddleware>();
}
```

#### Performance Counters
```csharp
public sealed class PerformanceDiagnosticsMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await next(context);
        
        stopwatch.Stop();
        
        if (stopwatch.ElapsedMilliseconds > 1000) // Log slow requests
        {
            _logger.LogWarning("Slow request: {Path} took {Duration}ms", 
                context.Request.Path, stopwatch.ElapsedMilliseconds);
        }
    }
}
```

## Production Deployment Optimization

### Configuration for Scale
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=production.db;Cache=Shared;Journal Mode=WAL;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "WorkflowEngine": {
    "MaxConcurrentExecutions": 50,
    "ExecutionTimeoutMinutes": 30,
    "CacheExpiryMinutes": 5
  }
}
```

### Container Optimization
```dockerfile
# Multi-stage build for smaller image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 5000

# Optimize for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY ["WorkflowEngine.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkflowEngine.dll"]
```

---

These optimizations can significantly improve performance at scale. Monitor your specific use case and apply optimizations incrementally, measuring the impact of each change.