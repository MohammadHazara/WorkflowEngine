# Workflow Engine

## Overview
The Workflow Engine is a .NET application designed to manage and execute workflows consisting of multiple steps. Each step can have its own success and failure handling mechanisms, allowing for flexible and robust workflow execution.

## Features
- Define workflows with a series of steps.
- Each step can specify actions to take on success or failure.
- Execute workflows in a defined order.
- Easy integration and extensibility through interfaces.

## Getting Started

### Prerequisites
- .NET SDK (version 6.0 or later)
- A code editor (e.g., Visual Studio Code)

### Installation
1. Clone the repository:
   ```
   git clone <repository-url>
   ```
2. Navigate to the project directory:
   ```
   cd WorkflowEngine/src
   ```
3. Restore the project dependencies:
   ```
   dotnet restore
   ```

### Running the Application
To run the application, execute the following command in the terminal:
```
dotnet run
```

### Running Tests
To run the unit tests, navigate to the tests directory and execute:
```
cd ../tests
dotnet test
```

## Usage
1. Create instances of `Workflow` and `Step`.
2. Define success and failure actions for each step.
3. Add steps to the workflow.
4. Execute the workflow using the `WorkflowExecutor`.

## Contributing
Contributions are welcome! Please open an issue or submit a pull request for any enhancements or bug fixes.

## License
This project is licensed under the MIT License. See the LICENSE file for more details.