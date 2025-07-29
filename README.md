# Workflow Engine

A powerful, enterprise-grade workflow management system built with .NET 9.0 and C# 13. Features a hierarchical job system with job groups, jobs, and tasks, plus a comprehensive web dashboard, robust API workflow system with SFTP support, and advanced performance optimization for large-scale operations.

## Features

### Hierarchical Job System
- **Job Groups**: Organize related jobs together for better management
- **Jobs**: Container for multiple tasks with execution ordering
- **Tasks**: Individual units of work with specific functionality
- Define data pipeline jobs with tasks like: fetch API data → create file → upload to SFTP
- Support for different job types: DataPipeline, ApiIntegration, FileProcessing
- Execution order control within job groups and jobs
- Progress tracking and cancellation support across all levels

### Core Engine (Legacy Support)
- Define workflows with a series of steps
- Each step can specify actions to take on success or failure
- Execute workflows with progress tracking and cancellation support
- Robust error handling and retry mechanisms
- Performance optimization for large-scale operations

### API Workflow System
- **API Data Fetching**: Retrieve data from REST APIs with authentication support
- **File Processing**: Create JSON files from API responses with validation
- **Compression**: ZIP file creation with configurable compression levels
- **File Upload**: Automated upload to target APIs with progress tracking
- **SFTP Support**: Secure file transfer via SFTP with key-based and password authentication
- **Job Builder**: Easy-to-use builder pattern for creating data pipeline jobs
- **Performance Optimized**: Handles large datasets efficiently with streaming and async operations

### Web Dashboard
- Real-time job and workflow monitoring and execution tracking
- Interactive dashboard with Bootstrap 5 UI
- Job and workflow statistics and performance metrics
- Progress tracking with live updates
- Error reporting and debugging tools
- Responsive design for mobile and desktop

### Database Integration
- Entity Framework Core with SQLite
- User management with secure password hashing
- Job execution history and audit trails
- Performance metrics storage and analysis
- Support for both new hierarchical models and legacy workflow models

## Technology Stack
- **.NET 9.0** with C# 13 syntax
- **ASP.NET Core** for web API and static file serving
- **Entity Framework Core** with SQLite database
- **HttpClient** with retry policies and timeout handling
- **System.IO.Compression** for ZIP file operations
- **SSH.NET** for secure SFTP file transfers
- **Bootstrap 5** and **Font Awesome** for UI
- **Dependency Injection** following SOLID principles
- **Structured logging** with performance monitoring

## Quick Start

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code (optional)

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/WorkflowEngine.git
   cd WorkflowEngine
   ```

2. **Build the solution**:
   ```bash
   dotnet build
   ```

3. **Run database migrations**:
   ```bash
   cd src
   dotnet ef database update
   ```

## Getting Started

### Creating Data Pipeline Jobs

```csharp
// Initialize services
var httpClient = new HttpClient();
var apiDataService = new ApiDataService(httpClient);
var sftpService = new SftpService();
var jobBuilder = new JobBuilder(apiDataService, sftpService);
var jobExecutor = new JobExecutor();

// Create a job group
var jobGroup = jobBuilder.CreateJobGroup(
    "Data Processing Pipeline", 
    "Complete data processing pipeline");

// Create a data pipeline job with tasks
var (apiConfig, fileConfig, zipConfig, sftpConfig) = jobBuilder.CreatePipelineConfigurations(
    apiUrl: "https://api.example.com/data",
    outputDirectory: "/path/to/output",
    sftpHost: "sftp.example.com",
    sftpUsername: "username",
    sftpPassword: "password",
    sftpRemoteDirectory: "/uploads"
);

var job = jobBuilder.CreateDataPipelineJob(
    "Customer Data Pipeline",
    apiConfig, fileConfig, zipConfig, sftpConfig);

// Add job to group and execute
jobGroup.AddJob(job);
var success = await jobExecutor.ExecuteJobGroupAsync(jobGroup);
```

### Creating Individual Tasks

```csharp
// Create a job
var job = Job.CreateDataPipelineJob("My Pipeline", "Processes customer data");

// Task 1: Fetch data from API
var fetchTask = JobTask.CreateApiFetchTask(1, "Fetch Customer Data", 
    "https://api.example.com/customers");
fetchTask.ExecutionOrder = 1;

// Task 2: Create file
var fileTask = JobTask.CreateFileTask(2, "Create JSON File", 
    "/path/to/customers.json");
fileTask.ExecutionOrder = 2;

// Task 3: Upload to SFTP
var sftpTask = JobTask.CreateSftpUploadTask(3, "Upload to SFTP",
    "sftp.example.com", "/local/file.zip", "/remote/uploads");
sftpTask.ExecutionOrder = 3;

// Add tasks to job
job.AddTask(fetchTask);
job.AddTask(fileTask);
job.AddTask(sftpTask);

// Execute job with progress tracking
var progress = new Progress<int>(p => Console.WriteLine($"Progress: {p}%"));
var execution = await jobExecutor.ExecuteJobWithProgressAsync(job, progress);
```

### Legacy Workflow Support (Backward Compatible)

```csharp
// Create a simple workflow (legacy)
var workflow = new Workflow("Data Processing", "Processes customer data");

var step1 = new Step(1, "Validate Data", async () => {
    // Your validation logic here
    return true;
});

var step2 = new Step(2, "Transform Data", async () => {
    // Your transformation logic here
    return true;
});

workflow.AddStep(step1);
workflow.AddStep(step2);

// Execute the workflow
var executor = new WorkflowExecutor();
var result = await executor.ExecuteWorkflowAsync(workflow);
```

### Running the Application

#### Start the Web Dashboard
```bash
cd src
dotnet run
```

The application will start and be available at:
- **Dashboard**: http://localhost:5000 (main web interface)
- **API**: http://localhost:5000/api (REST endpoints)
- **Swagger**: http://localhost:5000/swagger (API documentation, dev mode only)

#### Run Job System Examples
```bash
cd examples
dotnet run
```

This console application demonstrates:
1. **Complete Data Pipeline Job Group**: Multiple jobs with API fetch, file creation, compression, and SFTP upload
2. **Single Data Pipeline Job**: Individual job execution with progress tracking
3. **Simple API-to-File Job**: Basic data fetching and file creation
4. **Legacy Workflow Examples**: Backward compatibility with existing workflow system

## Hierarchical Structure Overview

The new job system follows this hierarchy:

```
JobGroup (e.g., "Data Processing Pipeline")
├── Job 1 (e.g., "Customer Data Pipeline" - DataPipeline type)
│   ├── Task 1: Fetch API Data (FetchApiData type)
│   ├── Task 2: Create JSON File (CreateFile type)
│   ├── Task 3: Compress to ZIP (CompressFile type)
│   └── Task 4: Upload to SFTP (UploadSftp type)
├── Job 2 (e.g., "Product Data Pipeline" - DataPipeline type)
│   ├── Task 1: Fetch Product Data
│   ├── Task 2: Transform Data
│   └── Task 3: Upload to Database
└── Job 3 (e.g., "Notification Job" - ApiIntegration type)
    ├── Task 1: Send Email Notifications
    └── Task 2: Update Status Dashboard
```

## Job Types and Task Types

### Supported Job Types
- **DataPipeline**: For data processing workflows
- **ApiIntegration**: For API integration workflows  
- **FileProcessing**: For file manipulation workflows
- **General**: For general-purpose workflows

### Supported Task Types
- **FetchApiData**: Retrieve data from REST APIs
- **CreateFile**: Create files from data
- **CompressFile**: Compress files to ZIP format
- **UploadSftp**: Upload files via SFTP
- **General**: General-purpose tasks

## API Workflow Examples

### Complete Data Pipeline Job Group
```csharp
// Create job group with multiple data pipeline jobs
var jobGroup = jobBuilder.CreateJobGroup("E-commerce Data Processing");

// Customer pipeline
var customerJob = jobBuilder.CreateDataPipelineJob("Customer Pipeline", ...);
customerJob.ExecutionOrder = 1;

// Product pipeline  
var productJob = jobBuilder.CreateDataPipelineJob("Product Pipeline", ...);
productJob.ExecutionOrder = 2;

// Order pipeline
var orderJob = jobBuilder.CreateDataPipelineJob("Order Pipeline", ...);
orderJob.ExecutionOrder = 3;

jobGroup.AddJob(customerJob);
jobGroup.AddJob(productJob);
jobGroup.AddJob(orderJob);

// Execute all jobs in sequence
await jobExecutor.ExecuteJobGroupAsync(jobGroup);
```

### SFTP Authentication Options

#### Password Authentication
```csharp
var sftpConfig = new SftpUploadConfig
{
    Host = "sftp.example.com",
    Username = "myuser",
    Password = "mypassword",
    LocalFilePath = "data.zip",
    RemoteDirectoryPath = "/uploads"
};
```

#### Key-Based Authentication
```csharp
var sftpConfig = new SftpUploadConfig
{
    Host = "sftp.example.com",
    Username = "myuser",
    PrivateKeyPath = "/path/to/private/key",
    PrivateKeyPassphrase = "key-passphrase",
    LocalFilePath = "data.zip",
    RemoteDirectoryPath = "/uploads",
    VerifyHostKey = true,
    HostKeyFingerprint = "aa:bb:cc:dd:ee:ff..."
};
```

## Performance Considerations
- Optimized database queries with `AsNoTracking()`
- Pagination for large datasets
- Background execution with cancellation support
- Efficient aggregation queries for statistics
- Memory-conscious data processing
- Streaming file operations for large datasets
- Configurable HTTP timeouts and retry policies
- Compression optimization for bandwidth efficiency
- SFTP connection pooling and reuse
- Progress tracking for long-running operations
- Task-level retry mechanisms and timeouts

## Use Cases

### Data Pipeline Workflows
- **Customer Data Synchronization**: Fetch customer data from CRM API → transform → upload to data warehouse via SFTP
- **Inventory Management**: Retrieve product data from multiple supplier APIs → consolidate → distribute to sales channels
- **Financial Reporting**: Collect transaction data from payment APIs → process → generate reports → upload to accounting system
- **IoT Data Processing**: Gather sensor data from device APIs → analyze → store in time-series database via secure transfer

### Multi-Job Orchestration
- **E-commerce Data Pipeline**: Sequential processing of customers → products → orders → analytics
- **ETL Workflows**: Extract from multiple sources → transform in parallel → load to target systems
- **Backup and Archival**: Data collection → compression → encrypted transfer to long-term storage
- **Compliance Reporting**: Data gathering → validation → report generation → secure delivery

## Security Features

### Job-Level Security
- **Task Isolation**: Each task runs independently with error containment
- **Configuration Encryption**: Sensitive task configuration data can be encrypted
- **Execution Auditing**: Complete audit trail of job and task executions
- **User Authentication**: Integration with user management system

### SFTP Security
- **SSH Key Authentication**: Support for RSA, DSA, and ECDSA keys
- **Host Key Verification**: Prevent man-in-the-middle attacks
- **Encrypted File Transfer**: All data encrypted in transit
- **Connection Timeout Controls**: Prevent hanging connections

### General Security
- **Input Validation**: Comprehensive validation of all inputs
- **SQL Injection Prevention**: Parameterized queries and Entity Framework protection
- **Password Security**: PBKDF2 hashing with salt
- **Error Handling**: Sanitized error messages to prevent information disclosure

## Contributing
1. Fork the repository
2. Create a feature branch
3. Follow the existing code style and SOLID principles
4. Add comprehensive tests using MSTest and NSubstitute
5. Update documentation as needed
6. Submit a pull request

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support
For questions, issues, or contributions, please open an issue on GitHub.