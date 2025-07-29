# Quick Start Tutorial

Get started with WorkflowEngine in just 5 minutes! This tutorial will guide you through creating your first workflow, running it, and monitoring its execution using the web dashboard.

## Before You Begin

Ensure you have:
- ✅ Completed the [Installation Guide](Installation-Guide)
- ✅ Application running at http://localhost:5000
- ✅ Dashboard accessible in your browser

## Step 1: Explore the Dashboard

1. **Open the Dashboard**: Navigate to http://localhost:5000
2. **Review Statistics**: Notice the dashboard shows sample data including:
   - Total workflows (6)
   - Active workflows (4)
   - Running executions
   - Success rate percentage

3. **Browse Sample Workflows**: The dashboard displays pre-loaded workflows like:
   - Data Processing Pipeline
   - Email Notification System
   - Database Backup
   - Security Audit

## Step 2: Create Your First Workflow

### Using the Web Interface

1. **Click "Create Workflow"** button in the dashboard
2. **Fill out the form**:
   ```
   Name: My First Workflow
   Description: A simple test workflow for learning
   Active: ✓ (checked)
   ```
3. **Click "Create"** to save your workflow

### Using Code (Alternative)

Create a new workflow programmatically:

```csharp
// Create a new workflow
var workflow = new Workflow("My First Workflow", "A simple test workflow for learning");

// Add steps with async execution logic
var step1 = new Step(1, "Initialize Process", 
    executeFunction: async () => {
        await Task.Delay(1000); // Simulate work
        Console.WriteLine("Step 1: Process initialized");
        return true;
    });

var step2 = new Step(2, "Process Data",
    executeFunction: async () => {
        await Task.Delay(2000); // Simulate work
        Console.WriteLine("Step 2: Data processed");
        return true;
    });

var step3 = new Step(3, "Finalize",
    executeFunction: async () => {
        await Task.Delay(500); // Simulate work
        Console.WriteLine("Step 3: Process finalized");
        return true;
    });

// Add steps to workflow
workflow.AddStep(step1);
workflow.AddStep(step2);
workflow.AddStep(step3);

// Save to database (if using DashboardService)
var createdWorkflow = await dashboardService.CreateWorkflowAsync(workflow);
```

## Step 3: Execute Your Workflow

### Via Dashboard

1. **Find your workflow** in the workflows list
2. **Click the "Execute" button** (play icon)
3. **Watch the execution** appear in the "Recent Executions" panel
4. **Monitor progress** with the real-time progress bar

### Via API

Execute using REST API:

```bash
# Start execution
curl -X POST http://localhost:5000/api/workflows/1/execute

# Check execution status
curl http://localhost:5000/api/dashboard/recent-executions
```

### Via Code

Execute programmatically:

```csharp
// Using WorkflowExecutor
var executor = serviceProvider.GetRequiredService<IWorkflowExecutor>();

// Execute with progress reporting
var progress = new Progress<int>(percentage => 
    Console.WriteLine($"Progress: {percentage}%"));

var result = await executor.ExecuteWorkflowWithProgressAsync(workflow, progress);
Console.WriteLine($"Execution result: {result}");
```

## Step 4: Monitor Execution

### Real-time Monitoring

1. **Dashboard Auto-refresh**: The dashboard updates every 30 seconds
2. **Progress Bars**: Watch real-time progress for running workflows
3. **Status Indicators**: Color-coded badges show execution status:
   - 🔵 **Pending**: Waiting to start
   - 🟡 **Running**: Currently executing
   - 🟢 **Completed**: Successfully finished
   - 🔴 **Failed**: Execution failed
   - ⚫ **Cancelled**: Manually cancelled

### Execution Details

Click on any execution to view:
- Start and completion times
- Execution duration
- Progress percentage
- Error messages (if failed)
- Step-by-step progress

## Step 5: Manage Workflows

### View Execution History

1. **Click on a workflow name** to view its details
2. **Browse execution history** with pagination
3. **Filter by status** or date range
4. **View detailed error logs** for failed executions

### Cancel Running Executions

1. **Find a running execution** in the Recent Executions panel
2. **Click the "Cancel" button** (stop icon)
3. **Confirm cancellation** - the execution will stop gracefully

### Edit or Delete Workflows

1. **Click the edit icon** next to a workflow
2. **Modify properties** like name, description, or active status
3. **Save changes** or **delete the workflow** if no longer needed

## Step 6: Advanced Features

### Workflow with Error Handling

Create a more robust workflow with error handling:

```csharp
var workflow = new Workflow("Robust Data Pipeline", "Pipeline with error handling");

var extractStep = new Step(1, "Extract Data",
    executeFunction: async () => {
        // Simulate data extraction
        var success = Random.Shared.Next(1, 10) > 2; // 80% success rate
        await Task.Delay(1000);
        return success;
    },
    onSuccess: async () => {
        Console.WriteLine("✅ Data extraction successful");
        return true;
    },
    onFailure: async () => {
        Console.WriteLine("❌ Data extraction failed - initiating retry");
        return false;
    });

workflow.AddStep(extractStep);
```

### Bulk Operations

Create multiple workflows efficiently:

```csharp
var workflows = new List<Workflow>();

for (int i = 1; i <= 5; i++)
{
    var workflow = new Workflow($"Batch Workflow {i}", $"Automatically generated workflow {i}");
    
    // Add common steps
    workflow.AddStep(new Step(1, "Prepare", async () => { await Task.Delay(500); return true; }));
    workflow.AddStep(new Step(2, "Execute", async () => { await Task.Delay(1000); return true; }));
    workflow.AddStep(new Step(3, "Cleanup", async () => { await Task.Delay(300); return true; }));
    
    workflows.Add(workflow);
}

// Save all workflows
foreach (var workflow in workflows)
{
    await dashboardService.CreateWorkflowAsync(workflow);
}
```

## Step 7: API Integration

### Dashboard Statistics

Get real-time statistics:

```javascript
// Fetch dashboard stats
fetch('/api/dashboard/stats')
    .then(response => response.json())
    .then(stats => {
        console.log(`Total workflows: ${stats.totalWorkflows}`);
        console.log(`Success rate: ${stats.successRate}%`);
        console.log(`Running executions: ${stats.runningExecutions}`);
    });
```

### Workflow Management

```javascript
// Create new workflow
const newWorkflow = {
    name: "API Created Workflow",
    description: "Created via REST API",
    isActive: true
};

fetch('/api/workflows', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(newWorkflow)
})
.then(response => response.json())
.then(workflow => console.log('Created:', workflow));

// Execute workflow
fetch(`/api/workflows/${workflowId}/execute`, { method: 'POST' })
    .then(response => response.json())
    .then(execution => console.log('Execution started:', execution));
```

## Common Patterns

### Data Processing Pipeline

```csharp
var pipeline = new Workflow("ETL Pipeline", "Extract, Transform, Load data");

pipeline.AddStep(new Step(1, "Extract", async () => {
    // Extract from data source
    await SimulateDataExtraction();
    return true;
}));

pipeline.AddStep(new Step(2, "Transform", async () => {
    // Transform data
    await SimulateDataTransformation();
    return true;
}));

pipeline.AddStep(new Step(3, "Load", async () => {
    // Load to destination
    await SimulateDataLoad();
    return true;
}));
```

### Notification Workflow

```csharp
var notification = new Workflow("Alert System", "Send notifications based on conditions");

notification.AddStep(new Step(1, "Check Conditions", async () => {
    // Check if notification should be sent
    return await ShouldSendNotification();
}));

notification.AddStep(new Step(2, "Send Email", async () => {
    // Send email notification
    await SendEmailNotification();
    return true;
}));

notification.AddStep(new Step(3, "Log Activity", async () => {
    // Log the notification activity
    await LogNotificationActivity();
    return true;
}));
```

## Next Steps

Now that you've created your first workflow:

1. 📖 **Learn More**: Explore the [Dashboard Overview](Dashboard-Overview) for advanced features
2. 🏗️ **Architecture**: Understand the system design in [Architecture Guide](Architecture-Guide)
3. 🔧 **Development**: Set up development environment with [Development Setup](Development-Setup)
4. 🚀 **Performance**: Optimize for production with [Performance Optimization](Performance-Optimization)
5. 🧪 **Testing**: Write tests using the [Testing Guide](Testing-Guide)

## Troubleshooting

### Workflow Not Executing
- ✅ Check that the workflow is marked as "Active"
- ✅ Verify there are no compilation errors in step functions
- ✅ Check the Recent Executions panel for error messages

### Dashboard Not Updating
- ✅ Refresh the browser page
- ✅ Check browser console for JavaScript errors
- ✅ Verify the API endpoints are responding

### Performance Issues
- ✅ Monitor execution times in the dashboard
- ✅ Check database size (SQLite file)
- ✅ Review step complexity and add delays if needed

---

**Congratulations!** 🎉 You've successfully created, executed, and monitored your first workflow using WorkflowEngine. The dashboard provides powerful tools for managing complex workflows at scale.