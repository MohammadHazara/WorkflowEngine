using System.Collections.Concurrent;
using System.Diagnostics;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for collecting and reporting performance metrics across the application
/// </summary>
public sealed class PerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly ConcurrentDictionary<string, PerformanceCounter> _counters = new();
    private readonly Timer _reportingTimer;

    public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger)
    {
        _logger = logger;
        
        // Report metrics every 5 minutes
        _reportingTimer = new Timer(LogPerformanceReport, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Records execution time for a specific operation
    /// </summary>
    /// <param name="operationName">Name of the operation being measured</param>
    /// <param name="duration">Time taken to execute the operation</param>
    public void RecordExecutionTime(string operationName, TimeSpan duration)
    {
        _counters.AddOrUpdate(operationName,
            new PerformanceCounter(operationName, duration),
            (key, existing) => existing.AddSample(duration));
    }

    /// <summary>
    /// Records execution time using a stopwatch measurement
    /// </summary>
    /// <param name="operationName">Name of the operation being measured</param>
    /// <param name="stopwatch">Stopwatch that measured the operation</param>
    public void RecordExecutionTime(string operationName, Stopwatch stopwatch)
    {
        RecordExecutionTime(operationName, stopwatch.Elapsed);
    }

    /// <summary>
    /// Gets current performance statistics for an operation
    /// </summary>
    /// <param name="operationName">Name of the operation to get stats for</param>
    /// <returns>Performance counter or null if operation not found</returns>
    public PerformanceCounter? GetPerformanceStats(string operationName)
    {
        return _counters.TryGetValue(operationName, out var counter) ? counter : null;
    }

    /// <summary>
    /// Gets all current performance statistics
    /// </summary>
    /// <returns>Dictionary of all performance counters</returns>
    public IReadOnlyDictionary<string, PerformanceCounter> GetAllPerformanceStats()
    {
        return _counters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Logs a comprehensive performance report
    /// </summary>
    private void LogPerformanceReport(object? state)
    {
        if (!_counters.Any()) return;

        _logger.LogInformation("=== Performance Report ===");
        
        foreach (var counter in _counters.Values.OrderBy(c => c.Name))
        {
            _logger.LogInformation(
                "Operation: {Operation} | Avg: {Average:F2}ms | Min: {Min:F2}ms | Max: {Max:F2}ms | Count: {Count}",
                counter.Name, counter.AverageMs, counter.MinMs, counter.MaxMs, counter.SampleCount);
        }

        // Reset counters after reporting to prevent memory bloat
        _counters.Clear();
    }

    public void Dispose()
    {
        _reportingTimer?.Dispose();
    }
}

/// <summary>
/// Performance counter for tracking operation execution times
/// </summary>
public sealed class PerformanceCounter
{
    private readonly object _lock = new();
    private readonly List<double> _samples = new();

    public string Name { get; }
    public double AverageMs => _samples.Count > 0 ? _samples.Average() : 0;
    public double MinMs => _samples.Count > 0 ? _samples.Min() : 0;
    public double MaxMs => _samples.Count > 0 ? _samples.Max() : 0;
    public int SampleCount => _samples.Count;

    public PerformanceCounter(string name, TimeSpan initialSample)
    {
        Name = name;
        _samples.Add(initialSample.TotalMilliseconds);
    }

    /// <summary>
    /// Adds a new performance sample to the counter
    /// </summary>
    /// <param name="duration">Duration to add as a sample</param>
    /// <returns>Updated performance counter</returns>
    public PerformanceCounter AddSample(TimeSpan duration)
    {
        lock (_lock)
        {
            _samples.Add(duration.TotalMilliseconds);
            
            // Keep only last 1000 samples to prevent memory bloat
            if (_samples.Count > 1000)
            {
                _samples.RemoveAt(0);
            }
        }
        return this;
    }
}