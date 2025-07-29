# WorkflowEngine Wiki

Welcome to the **WorkflowEngine** wiki! This comprehensive documentation covers everything you need to know about the enterprise-grade workflow management system with real-time dashboard capabilities.

## 📚 Documentation Sections

### Getting Started
- [Installation Guide](Installation-Guide) - Quick setup and installation instructions
- [Quick Start Tutorial](Quick-Start-Tutorial) - Build your first workflow in 5 minutes
- [Configuration](Configuration) - Environment setup and configuration options

### Core Features
- [Dashboard Overview](Dashboard-Overview) - Web-based monitoring and management interface
- [Workflow Management](Workflow-Management) - Creating, editing, and organizing workflows
- [Execution Monitoring](Execution-Monitoring) - Real-time progress tracking and management
- [API Reference](API-Reference) - Complete REST API documentation

### Development
- [Architecture Guide](Architecture-Guide) - System design and component overview
- [Development Setup](Development-Setup) - Setting up the development environment
- [Testing Guide](Testing-Guide) - Running and writing tests
- [Contributing Guidelines](Contributing-Guidelines) - How to contribute to the project

### Advanced Topics
- [Performance Optimization](Performance-Optimization) - Scaling and performance tuning
- [Database Schema](Database-Schema) - Complete database structure documentation
- [Security Considerations](Security-Considerations) - Security best practices and guidelines
- [Deployment Guide](Deployment-Guide) - Production deployment strategies

### Troubleshooting
- [Common Issues](Common-Issues) - Frequently encountered problems and solutions
- [FAQ](FAQ) - Frequently asked questions
- [Support](Support) - Getting help and support

## 🚀 Key Features

### Dashboard Capabilities
- **Real-time Monitoring**: Live workflow execution tracking with auto-refresh
- **Interactive Management**: Create, edit, delete, and execute workflows
- **Progress Visualization**: Real-time progress bars and status indicators
- **History Tracking**: Comprehensive execution history with error details
- **Mobile Responsive**: Bootstrap-based interface for desktop and mobile

### Core Engine
- **High Performance**: 856+ steps/second execution throughput
- **Async Processing**: Non-blocking operations with cancellation support
- **Error Resilience**: Automatic retry logic with exponential backoff
- **Scalable Architecture**: SOLID principles with dependency injection
- **Entity Framework**: Code-first database design with SQLite

### Technology Stack
- **.NET 9.0** with C# 13 syntax
- **ASP.NET Core** for web API and static file serving
- **Entity Framework Core** with SQLite database
- **Bootstrap 5** and Font Awesome for modern UI
- **MSTest** and NSubstitute for comprehensive testing

## 📖 Quick Navigation

| Section | Description |
|---------|-------------|
| [Installation Guide](Installation-Guide) | Get up and running in minutes |
| [Quick Start Tutorial](Quick-Start-Tutorial) | Your first workflow |
| [Dashboard Overview](Dashboard-Overview) | Web interface tour |
| [API Reference](API-Reference) | REST endpoints |
| [Architecture Guide](Architecture-Guide) | System design |
| [Performance Optimization](Performance-Optimization) | Scaling tips |

## 🎯 Latest Updates

**Version 2.0.0 - Dashboard Release**
- Added comprehensive web dashboard with real-time monitoring
- Implemented REST API with Swagger documentation
- Enhanced database schema with execution tracking
- Added responsive Bootstrap 5 interface
- Integrated background execution with progress tracking

## 💡 Quick Examples

### Creating a Workflow
```csharp
var workflow = new Workflow("Data Processing", "ETL pipeline");
var step1 = new Step(1, "Extract", async () => { /* logic */ return true; });
workflow.AddStep(step1);
```

### Using the Dashboard
1. Start the application: `dotnet run`
2. Open browser to: `http://localhost:5000`
3. View statistics, create workflows, monitor executions

### API Usage
```http
GET /api/dashboard/stats
POST /api/workflows/{id}/execute
GET /api/workflows?page=1&pageSize=10
```

## 🤝 Community

- **Issues**: Report bugs and request features on GitHub Issues
- **Discussions**: Join community discussions for questions and ideas
- **Contributing**: Check the [Contributing Guidelines](Contributing-Guidelines)
- **License**: MIT License - see LICENSE file for details

---

**Last Updated**: July 29, 2025  
**Version**: 2.0.0 - Dashboard Release