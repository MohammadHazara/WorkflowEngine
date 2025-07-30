using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Data;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WorkflowDbContext>("database")
    .AddCheck("api_service", () => HealthCheckResult.Healthy("API service is running"));

// Add memory caching
builder.Services.AddMemoryCache();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=workflow.db"));

// Register application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();
builder.Services.AddScoped<IStepHandler, StepHandler>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<DataSeedingService>();
builder.Services.AddScoped<PerformanceMetricsService>();
builder.Services.AddScoped<SftpService>();

// Register job management services
builder.Services.AddScoped<JobRepositoryService>();
builder.Services.AddScoped<JobBuilder>();
builder.Services.AddScoped<JobExecutor>();

// Register HTTP client and API services
builder.Services.AddHttpClient<ApiDataService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "WorkflowEngine/1.0");
    client.Timeout = TimeSpan.FromMinutes(10); // Default timeout for all operations
});
builder.Services.AddScoped<ApiWorkflowBuilder>();

// Add CORS for dashboard frontend with security
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("https://localhost:5001", "https://yourdomain.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Add logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("DashboardPolicy");

// Serve static files for dashboard with default file mapping
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = { "index.html" }
});
app.UseStaticFiles();

app.UseAuthorization();

// Add health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

// Ensure database is created and seeded with improved error handling
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
    var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Initializing database...");
    await dbContext.Database.EnsureCreatedAsync();
    
    logger.LogInformation("Seeding sample data...");
    await seedingService.SeedSampleDataAsync();
    
    logger.LogInformation("Database initialization completed successfully");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to initialize database. Application will start but may not function correctly.");
}

Console.WriteLine("🚀 WorkflowEngine Web API Starting...");
Console.WriteLine("📊 Dashboard available at: http://localhost:5000");
Console.WriteLine("📋 Swagger UI available at: http://localhost:5000/swagger");
Console.WriteLine("🔗 API endpoints available at: http://localhost:5000/api/");
Console.WriteLine("💚 Health checks available at: http://localhost:5000/health");

app.Run();