# Workflow Engine

## Overview
The Workflow Engine is a comprehensive .NET 9.0 application designed to manage and execute workflows consisting of multiple steps. It features a modern web-based dashboard for monitoring and managing workflows in real-time, with support for execution tracking, progress monitoring, and workflow management.

## Features

### Core Engine
- Define workflows with a series of steps
- Each step can specify actions to take on success or failure
- Execute workflows with progress tracking and cancellation support
- Robust error handling and retry mechanisms
- Performance optimization for large-scale operations

### Web Dashboard
- **Real-time monitoring**: Live updates of workflow executions every 30 seconds
- **Statistics overview**: Dashboard with key metrics (total workflows, success rate, etc.)
- **Workflow management**: Create, update, delete, and execute workflows
- **Execution tracking**: Monitor running workflows with progress bars
- **History view**: Browse execution history with pagination
- **Interactive UI**: Modern Bootstrap-based responsive interface

### API Endpoints
- RESTful API for all dashboard operations
- Swagger documentation available in development mode
- CORS support for frontend integration
- Comprehensive error handling and logging

## Technology Stack
- **.NET 9.0** with C# 13 syntax
- **ASP.NET Core** for web API and static file serving
- **Entity Framework Core** with SQLite database
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

#### Features Available
- View real-time workflow statistics
- Browse and manage workflows with pagination
- Execute workflows and monitor progress
- Cancel running executions
- View execution history and error details
- Create new workflows through the web interface

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

### Recent Executions
```http
GET /api/dashboard/recent-executions?limit=20
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

### Key Components
- `IDashboardService`: Core dashboard operations
- `IWorkflowExecutor`: Workflow execution engine
- `IStepHandler`: Individual step processing
- `WorkflowDbContext`: Database access layer
- `DataSeedingService`: Sample data management

## Performance Considerations
- Optimized database queries with `AsNoTracking()`
- Pagination for large datasets
- Background execution with cancellation support
- Efficient aggregation queries for statistics
- Memory-conscious data processing

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

### Development Mode
- Enable detailed logging by setting `ASPNETCORE_ENVIRONMENT=Development`
- Access Swagger UI at `/swagger` for API testing
- Check console logs for detailed error information