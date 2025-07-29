using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Data;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=workflow.db"));

// Register application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();
builder.Services.AddScoped<IStepHandler, StepHandler>();
builder.Services.AddScoped<DataSeedingService>();

// Add CORS for dashboard frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

app.UseHttpsRedirection();
app.UseCors("DashboardPolicy");
app.UseAuthorization();
app.MapControllers();

// Serve static files for dashboard
app.UseDefaultFiles();
app.UseStaticFiles();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
    var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
    
    await dbContext.EnsureDatabaseCreatedAsync();
    await seedingService.SeedSampleDataAsync();
}

app.Run();