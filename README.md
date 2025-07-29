# Workflow Engine

## Overview
The Workflow Engine is a comprehensive .NET 9.0 application designed to manage and execute workflows consisting of multiple steps. It features a modern web-based dashboard for monitoring and managing workflows in real-time, with support for execution tracking, progress monitoring, and workflow management. **NEW**: Now includes specialized API workflow capabilities for data fetching, file processing, and upload automation.

## Features

### Core Engine
- Define workflows with a series of steps
- Each step can specify actions to take on success or failure
- Execute workflows with progress tracking and cancellation support
- Robust error handling and retry mechanisms
- Performance optimization for large-scale operations

### API Workflow System (NEW)
- **API Data Fetching**: Retrieve data from REST APIs with authentication support
- **File Processing**: Create JSON files from API responses with validation
- **Compression**: ZIP file creation with configurable compression levels
- **File Upload**: Automated upload to target APIs with progress tracking
- **Workflow Builder**: Easy-to-use builder pattern for common API workflows
- **Performance Optimized**: Handles large datasets efficiently with streaming and async operations

### Web Dashboard
- **Real-time monitoring**: Live updates of workflow executions every 30 seconds
- **Statistics overview**: Dashboard with key metrics (total workflows, success rate, etc.)
- **Workflow management**: Create, update, delete, and execute workflows
- **Execution tracking**: Monitor running workflows with progress bars
- **History view**: Browse execution history with pagination
- **Interactive UI**: Modern Bootstrap-based responsive interface

### API Endpoints
- RESTful API for all dashboard operations
- **NEW**: API workflow endpoints for automated data processing
- Swagger documentation available in development mode
- CORS support for frontend integration
- Comprehensive error handling and logging

## Technology Stack
- **.NET 9.0** with C# 13 syntax
- **ASP.NET Core** for web API and static file serving
- **Entity Framework Core** with SQLite database
- **HttpClient** with retry policies and timeout handling
- **System.IO.Compression** for ZIP file operations
- **Bootstrap 5** and **Font Awesome** for UI
- **Dependency Injection** following SOLID principles
- **Structured logging** with performance monitoring

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- A modern web browser
- Code editor (Visual Studio Code recommended)

### Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd WorkflowEngine
   ```

2. Restore dependencies:
   ```bash
   cd src
   dotnet restore
   ```

3. Build the application:
   ```bash
   dotnet build
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

#### Run API Workflow Examples (NEW)
```bash
cd examples
dotnet run
```

This console application demonstrates:
1. **Complete API-to-File-to-Upload Workflow**: Fetches data from JSONPlaceholder API, creates JSON file, compresses to ZIP, and uploads
2. **Simple API-to-File Workflow**: Basic data fetching and file creation
3. **Performance Test**: Large dataset processing with timing metrics

#### Features Available
- View real-time workflow statistics
- Browse and manage workflows with pagination
- Execute workflows and monitor progress
- Cancel running executions
- View execution history and error details
- Create new workflows through the web interface
- **NEW**: Execute API workflows programmatically

### Sample Data
The application automatically seeds the database with sample workflows and executions on first run, including:
- 6 different workflow types (data processing, notifications, reports, etc.)
- 25 sample executions with various statuses
- Realistic execution times and error scenarios

### Database
- Uses SQLite database (`workflow.db`) for persistence
- Entity Framework Code First approach
- Automatic database creation and migration
- Sample data seeding for demonstration

## API Usage

### Dashboard Statistics
```http
GET /api/dashboard/stats
```

### Workflow Management
```http
GET /api/workflows?page=1&pageSize=10
POST /api/workflows
PUT /api/workflows/{id}
DELETE /api/workflows/{id}
```

### Workflow Execution
```http
POST /api/workflows/{id}/execute
GET /api/workflows/{id}/executions
POST /api/executions/{id}/cancel
```

### API Workflows (NEW)
```http
POST /api/api-workflow/execute
POST /api/api-workflow/fetch-and-save
POST /api/api-workflow/compress-and-upload
```

### Recent Executions
```http
GET /api/dashboard/recent-executions?limit=20
```

## API Workflow Examples

### Complete API-to-File-to-Upload Pipeline
```csharp
var apiDataService = new ApiDataService(httpClient);
var workflowBuilder = new ApiWorkflowBuilder(apiDataService);

var (apiConfig, fileConfig, zipConfig, uploadConfig) = workflowBuilder.CreateDefaultConfigurations(
    apiUrl: "https://api.example.com/data",
    outputDirectory: "/path/to/output",
    uploadUrl: "https://upload.example.com/api",
    authToken: "Bearer your-token"
);

var workflow = workflowBuilder.CreateApiToFileToUploadWorkflow(
    "Data Processing Pipeline",
    apiConfig, fileConfig, zipConfig, uploadConfig
);

// Execute with progress tracking
foreach (var step in workflow.GetSteps())
{
    var success = await step.ExecuteAsync();
    if (!success) break;
}
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

### File Compression and Upload
```csharp
var zipConfig = new ZipCompressionConfig
{
    SourceFilePath = "data.json",
    ZipFilePath = "data.zip",
    CompressionLevel = CompressionLevel.Optimal,
    DeleteSourceAfterCompression = true
};

var compressed = await apiDataService.CompressFileAsync(zipConfig);

var uploadConfig = new ApiUploadConfig
{
    UploadUrl = "https://api.example.com/upload",
    FilePath = "data.zip",
    FileFieldName = "file",
    AdditionalFields = new Dictionary<string, string>
    {
        { "description", "Processed data file" }
    }
};

var uploaded = await apiDataService.UploadFileAsync(uploadConfig);
```

## Development

### Running Tests
```bash
cd tests
dotnet test
```

### Architecture
The application follows SOLID principles with:
- **Repository pattern** through Entity Framework
- **Service layer** for business logic
- **Dependency injection** for loose coupling
- **Interface segregation** for testability
- **Performance optimization** with async/await patterns
- **NEW**: Specialized API services with retry policies and error handling

### Key Components
- `IDashboardService`: Core dashboard operations
- `IWorkflowExecutor`: Workflow execution engine
- `IStepHandler`: Individual step processing
- `WorkflowDbContext`: Database access layer
- `DataSeedingService`: Sample data management
- **NEW**: `ApiDataService`: API operations and file processing
- **NEW**: `ApiWorkflowBuilder`: Workflow creation utilities

## Performance Considerations
- Optimized database queries with `AsNoTracking()`
- Pagination for large datasets
- Background execution with cancellation support
- Efficient aggregation queries for statistics
- Memory-conscious data processing
- **NEW**: Streaming file operations for large datasets
- **NEW**: Configurable HTTP timeouts and retry policies
- **NEW**: Compression optimization for bandwidth efficiency

## Use Cases

### API Data Processing Workflows
- **Data Migration**: Fetch data from legacy APIs and upload to new systems
- **Data Synchronization**: Regular data syncing between systems
- **Report Generation**: Collect data from multiple APIs, process, and distribute
- **Backup Operations**: Download data, compress, and upload to storage services
- **ETL Pipelines**: Extract from APIs, transform to JSON, load to target systems

### Real-World Examples
1. **E-commerce Data Sync**: Fetch product data from supplier APIs, format as JSON, compress, and upload to inventory system
2. **Financial Report Automation**: Collect transaction data from payment APIs, generate reports, compress, and send to accounting system
3. **IoT Data Collection**: Gather sensor data from device APIs, process into standardized format, and upload to analytics platform

## Contributing
1. Fork the repository
2. Create a feature branch
3. Follow the existing code style and SOLID principles
4. Add comprehensive tests using MSTest and NSubstitute
5. Update documentation as needed
6. Submit a pull request

## License
This project is licensed under the MIT License. See the LICENSE file for more details.

## Troubleshooting

### Common Issues
1. **Port already in use**: Change the port in `dotnet run --urls "http://localhost:5001"`
2. **Database errors**: Delete `workflow.db` to reset the database
3. **CORS issues**: Ensure the API is running on the same origin as the frontend
4. **API timeout errors**: Increase timeout values in API configurations
5. **File permission errors**: Ensure write permissions for output directories

### Development Mode
- Enable detailed logging by setting `ASPNETCORE_ENVIRONMENT=Development`
- Access Swagger UI at `/swagger` for API testing
- Check console logs for detailed error information
- Use the examples console application for testing API workflows

### Performance Tuning
- Adjust HTTP client timeout settings for your API endpoints
- Configure compression levels based on file size vs. speed requirements
- Use appropriate retry policies for unreliable network connections
- Monitor memory usage when processing large datasets