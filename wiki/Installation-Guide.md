# Installation Guide

This guide walks you through installing and setting up WorkflowEngine on your development or production environment.

## Prerequisites

### Required Software
- **.NET 9.0 SDK** or later
- **Git** for source code management
- **Modern web browser** (Chrome, Firefox, Safari, Edge)

### Optional Tools
- **Visual Studio Code** or **Visual Studio 2022** for development
- **SQL Server Management Studio** or **DB Browser for SQLite** for database inspection

## Installation Methods

### Option 1: Clone from GitHub (Recommended)

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/WorkflowEngine.git
   cd WorkflowEngine
   ```

2. **Restore dependencies**:
   ```bash
   cd src
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

### Option 2: Download ZIP

1. Download the latest release from GitHub
2. Extract to your desired location
3. Follow steps 2-4 from Option 1

## Verification

### Check Installation
After running `dotnet run`, you should see output similar to:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Access the Dashboard
1. Open your browser to: http://localhost:5000
2. You should see the WorkflowEngine dashboard with sample data
3. The dashboard should display statistics, workflows, and recent executions

### Test API Endpoints
- **Swagger Documentation**: http://localhost:5000/swagger (development mode)
- **Dashboard Stats**: http://localhost:5000/api/dashboard/stats
- **Workflows List**: http://localhost:5000/api/workflows

## Database Setup

### Automatic Database Creation
The application automatically:
- Creates the SQLite database (`workflow.db`) on first run
- Applies Entity Framework migrations
- Seeds sample data for demonstration

### Manual Database Reset
To reset the database with fresh sample data:
```bash
# Stop the application (Ctrl+C)
# Delete the database file
rm workflow.db
# Restart the application
dotnet run
```

## Configuration

### Default Configuration
The application uses these default settings:
- **Port**: 5000 (HTTP)
- **Database**: SQLite (`workflow.db` in project root)
- **Environment**: Development
- **Logging**: Console and Debug output

### Custom Configuration
Create an `appsettings.json` file to override defaults:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=custom_workflow.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Urls": "http://localhost:5001"
}
```

### Environment Variables
You can also configure using environment variables:
```bash
export ASPNETCORE_URLS="http://localhost:5001"
export ConnectionStrings__DefaultConnection="Data Source=prod_workflow.db"
dotnet run
```

## Platform-Specific Instructions

### Windows
```cmd
# Clone repository
git clone https://github.com/your-username/WorkflowEngine.git
cd WorkflowEngine\src

# Restore and run
dotnet restore
dotnet run
```

### macOS
```bash
# Install .NET 9.0 if not already installed
brew install dotnet

# Clone and run
git clone https://github.com/your-username/WorkflowEngine.git
cd WorkflowEngine/src
dotnet restore
dotnet run
```

### Linux (Ubuntu/Debian)
```bash
# Install .NET 9.0
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0

# Clone and run
git clone https://github.com/your-username/WorkflowEngine.git
cd WorkflowEngine/src
dotnet restore
dotnet run
```

## Docker Installation (Optional)

### Using Docker
Create a `Dockerfile` in the project root:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/WorkflowEngine.csproj", "src/"]
RUN dotnet restore "src/WorkflowEngine.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "WorkflowEngine.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WorkflowEngine.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkflowEngine.dll"]
```

Build and run:
```bash
docker build -t workflowengine .
docker run -p 5000:5000 workflowengine
```

## Troubleshooting Installation

### Common Issues

#### Port Already in Use
```bash
# Use a different port
dotnet run --urls "http://localhost:5001"
```

#### .NET SDK Not Found
```bash
# Check .NET installation
dotnet --version
# Should show 9.0.x or later
```

#### Database Permission Issues
```bash
# Ensure write permissions in project directory
chmod 755 .
# Or run from a directory with write access
```

#### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Verification Commands
Run these commands to verify your installation:
```bash
# Check .NET version
dotnet --version

# Check project compiles
dotnet build

# Check tests pass
cd ../tests
dotnet test

# Check application starts
cd ../src
dotnet run
```

## Next Steps

After successful installation:
1. 📖 Follow the [Quick Start Tutorial](Quick-Start-Tutorial) to create your first workflow
2. 🎛️ Explore the [Dashboard Overview](Dashboard-Overview) to understand the interface
3. 🔧 Review [Configuration](Configuration) for environment-specific settings
4. 🚀 Check [Performance Optimization](Performance-Optimization) for production deployments

## Getting Help

If you encounter issues:
1. Check the [Common Issues](Common-Issues) page
2. Review the [FAQ](FAQ) for frequently asked questions
3. Search existing GitHub Issues
4. Create a new issue with detailed error information

---

**Installation complete!** Your WorkflowEngine is ready to use. Access the dashboard at http://localhost:5000 and start creating workflows.