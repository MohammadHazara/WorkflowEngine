# Workflow Engine

A powerful, enterprise-grade workflow management system built with .NET 9.0 and C# 13. Features a comprehensive web dashboard, robust API workflow system with SFTP support, and advanced performance optimization for large-scale operations.

## Features

### Core Engine
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
- **Workflow Builder**: Easy-to-use builder pattern for common API workflows
- **Performance Optimized**: Handles large datasets efficiently with streaming and async operations

### Web Dashboard
- Real-time workflow monitoring and execution tracking
- Interactive dashboard with Bootstrap 5 UI
- Workflow statistics and performance metrics
- Progress tracking with live updates
- Error reporting and debugging tools
- Responsive design for mobile and desktop

### Database Integration
- Entity Framework Core with SQLite
- User management with secure password hashing
- Workflow execution history and audit trails
- Performance metrics storage and analysis

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

### Basic Workflow Creation
```csharp
// Create a simple workflow
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

#### Run API Workflow Examples
```bash
cd examples
dotnet run
```

This console application demonstrates:
1. **Complete API-to-File-to-SFTP Workflow**: Fetches data from JSONPlaceholder API, creates JSON file, compresses to ZIP, and uploads via SFTP
2. **Simple API-to-File Workflow**: Basic data fetching and file creation
3. **Performance Test**: Large dataset processing with timing metrics

#### Features Available
- View real-time workflow statistics
- Browse and manage workflows with pagination
- Execute workflows and monitor progress
- Cancel running executions
- View execution history and error details
- Create new workflows through the web interface
- Execute API workflows programmatically with SFTP support

## API Workflow Examples

### Complete API-to-File-to-SFTP Pipeline
```csharp
var apiDataService = new ApiDataService(httpClient);
var sftpService = new SftpService();
var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

var (apiConfig, fileConfig, zipConfig, sftpConfig) = workflowBuilder.CreateSftpConfigurations(
    apiUrl: "https://api.example.com/data",
    outputDirectory: "/path/to/output",
    sftpHost: "sftp.example.com",
    sftpUsername: "username",
    sftpPassword: "password", // or use key-based auth
    sftpRemoteDirectory: "/uploads"
);

var workflow = workflowBuilder.CreateApiToFileToSftpWorkflow(
    "Data Processing Pipeline",
    apiConfig, fileConfig, zipConfig, sftpConfig
);

// Execute with progress tracking
foreach (var step in workflow.GetSteps())
{
    var success = await step.ExecuteAsync();
    if (!success) break;
}
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
    PrivateKeyPassphrase = "key-passphrase", // if key is encrypted
    LocalFilePath = "data.zip",
    RemoteDirectoryPath = "/uploads",
    VerifyHostKey = true,
    HostKeyFingerprint = "aa:bb:cc:dd:ee:ff..." // for security
};
```

### Simple API Data Fetching
```csharp
var apiConfig = new ApiDataFetchConfig
{
    ApiUrl = "https://jsonplaceholder.typicode.com/posts",
    TimeoutSeconds = 30,
    MaxRetries = 3,
    Headers = new Dictionary<string, string>
    {
        { "Authorization", "Bearer token" },
        { "Accept", "application/json" }
    }
};

var data = await apiDataService.FetchDataAsync(apiConfig);
var success = await apiDataService.CreateJsonFileAsync(data, fileConfig);
```

### File Compression and SFTP Upload
```csharp
var zipConfig = new ZipCompressionConfig
{
    SourceFilePath = "data.json",
    ZipFilePath = "data.zip",
    CompressionLevel = CompressionLevel.Optimal,
    DeleteSourceAfterCompression = true
};

var compressed = await apiDataService.CompressFileAsync(zipConfig);

var sftpConfig = new SftpUploadConfig
{
    Host = "sftp.example.com",
    Username = "user",
    Password = "pass",
    LocalFilePath = "data.zip",
    RemoteDirectoryPath = "/uploads",
    CreateRemoteDirectories = true
};

var uploaded = await sftpService.UploadFileAsync(sftpConfig);
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

## Use Cases

### API Data Processing Workflows
- **Data Migration**: Fetch data from legacy APIs and upload to new systems via SFTP
- **Data Synchronization**: Regular data syncing between systems with secure file transfer
- **Report Generation**: Collect data from multiple APIs, process, and distribute via SFTP
- **Backup Operations**: Download data, compress, and upload to secure SFTP storage
- **ETL Pipelines**: Extract from APIs, transform to JSON, load to target systems via SFTP

### Real-World Examples
1. **E-commerce Data Sync**: Fetch product data from supplier APIs, format as JSON, compress, and upload to inventory system via SFTP
2. **Financial Report Automation**: Collect transaction data from payment APIs, generate reports, compress, and send to accounting system via secure transfer
3. **IoT Data Collection**: Gather sensor data from device APIs, process into standardized format, and upload to analytics platform via SFTP
4. **Healthcare Data Exchange**: Securely transfer patient data between systems using SFTP with encryption
5. **Log File Processing**: Collect, compress, and securely transfer application logs to centralized storage

## Security Features

### SFTP Security
- **SSH Key Authentication**: Support for RSA, DSA, and ECDSA keys
- **Host Key Verification**: Prevent man-in-the-middle attacks
- **Encrypted File Transfer**: All data encrypted in transit
- **Connection Timeout Controls**: Prevent hanging connections
- **Credential Management**: Secure handling of passwords and keys

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