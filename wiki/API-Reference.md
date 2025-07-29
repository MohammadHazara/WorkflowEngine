# API Reference

Complete documentation for the WorkflowEngine REST API. All endpoints return JSON and follow RESTful conventions.

## Base URL

```
http://localhost:5000/api
```

## Authentication

Currently, the API does not require authentication. In production environments, consider implementing authentication and authorization.

## Response Format

### Success Response
```json
{
  "data": { /* response data */ },
  "status": "success"
}
```

### Error Response
```json
{
  "error": "Error message",
  "status": "error",
  "statusCode": 400
}
```

## Dashboard Endpoints

### Get Dashboard Statistics

Retrieves overall system statistics for the dashboard.

```http
GET /api/dashboard/stats
```

**Response:**
```json
{
  "totalWorkflows": 6,
  "activeWorkflows": 4,
  "totalExecutions": 25,
  "runningExecutions": 2,
  "completedExecutions": 18,
  "failedExecutions": 3,
  "averageExecutionTimeMs": 15420.5,
  "successRate": 72.0
}
```

**Example:**
```bash
curl -X GET http://localhost:5000/api/dashboard/stats
```

### Get Recent Executions

Retrieves recent workflow executions with optional limit.

```http
GET /api/dashboard/recent-executions?limit={limit}
```

**Parameters:**
- `limit` (optional): Number of executions to return (default: 20, max: 100)

**Response:**
```json
[
  {
    "id": 1,
    "workflowId": 2,
    "workflow": {
      "id": 2,
      "name": "Data Processing Pipeline",
      "description": "Processes incoming data files"
    },
    "status": 2,
    "startedAt": "2025-07-29T10:30:00Z",
    "completedAt": "2025-07-29T10:32:15Z",
    "durationMs": 135000,
    "currentStepIndex": 4,
    "totalSteps": 4,
    "progressPercentage": 100,
    "errorMessage": null
  }
]
```

**Status Values:**
- `0`: Pending
- `1`: Running
- `2`: Completed
- `3`: Failed
- `4`: Cancelled

**Example:**
```bash
curl -X GET "http://localhost:5000/api/dashboard/recent-executions?limit=10"
```

## Workflow Management Endpoints

### Get Workflows (Paginated)

Retrieves a paginated list of workflows.

```http
GET /api/workflows?page={page}&pageSize={pageSize}
```

**Parameters:**
- `page` (optional): Page number, 1-based (default: 1)
- `pageSize` (optional): Items per page (default: 10, max: 100)

**Response:**
```json
{
  "items": [
    {
      "id": 1,
      "name": "Data Processing Pipeline",
      "description": "Processes incoming data files and validates content",
      "createdAt": "2025-06-29T10:00:00Z",
      "updatedAt": "2025-07-04T15:30:00Z",
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 6,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

**Example:**
```bash
curl -X GET "http://localhost:5000/api/workflows?page=1&pageSize=5"
```

### Get Single Workflow

Retrieves a specific workflow by ID.

```http
GET /api/workflows/{id}
```

**Parameters:**
- `id`: Workflow ID (integer)

**Response:**
```json
{
  "id": 1,
  "name": "Data Processing Pipeline",
  "description": "Processes incoming data files and validates content",
  "createdAt": "2025-06-29T10:00:00Z",
  "updatedAt": "2025-07-04T15:30:00Z",
  "isActive": true
}
```

**Example:**
```bash
curl -X GET http://localhost:5000/api/workflows/1
```

**Error Responses:**
- `404`: Workflow not found

### Create Workflow

Creates a new workflow.

```http
POST /api/workflows
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "My New Workflow",
  "description": "Description of the workflow",
  "isActive": true
}
```

**Response:**
```json
{
  "id": 7,
  "name": "My New Workflow",
  "description": "Description of the workflow",
  "createdAt": "2025-07-29T14:30:00Z",
  "updatedAt": "2025-07-29T14:30:00Z",
  "isActive": true
}
```

**Example:**
```bash
curl -X POST http://localhost:5000/api/workflows \
  -H "Content-Type: application/json" \
  -d '{
    "name": "API Test Workflow",
    "description": "Created via API",
    "isActive": true
  }'
```

**Validation Rules:**
- `name`: Required, max 255 characters
- `description`: Optional, max 1000 characters
- `isActive`: Optional, defaults to true

### Update Workflow

Updates an existing workflow.

```http
PUT /api/workflows/{id}
Content-Type: application/json
```

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Workflow Name",
  "description": "Updated description",
  "isActive": false
}
```

**Response:**
```json
{
  "id": 1,
  "name": "Updated Workflow Name",
  "description": "Updated description",
  "createdAt": "2025-06-29T10:00:00Z",
  "updatedAt": "2025-07-29T14:35:00Z",
  "isActive": false
}
```

**Example:**
```bash
curl -X PUT http://localhost:5000/api/workflows/1 \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "name": "Updated Name",
    "description": "Updated description",
    "isActive": true
  }'
```

**Error Responses:**
- `400`: Validation errors or ID mismatch
- `404`: Workflow not found

### Delete Workflow

Deletes a workflow and all its executions.

```http
DELETE /api/workflows/{id}
```

**Response:**
- `204`: No Content (success)
- `404`: Workflow not found

**Example:**
```bash
curl -X DELETE http://localhost:5000/api/workflows/1
```

**Note:** This operation cascades to delete all associated executions.

## Workflow Execution Endpoints

### Execute Workflow

Starts execution of a workflow.

```http
POST /api/workflows/{id}/execute
```

**Parameters:**
- `id`: Workflow ID to execute

**Response:**
```json
{
  "id": 26,
  "workflowId": 1,
  "status": 0,
  "startedAt": "2025-07-29T14:40:00Z",
  "completedAt": null,
  "durationMs": null,
  "currentStepIndex": 0,
  "totalSteps": 0,
  "errorMessage": null,
  "progressPercentage": 0
}
```

**Example:**
```bash
curl -X POST http://localhost:5000/api/workflows/1/execute
```

**Error Responses:**
- `400`: Workflow not found or inactive
- `404`: Workflow not found

### Get Workflow Executions

Retrieves execution history for a specific workflow.

```http
GET /api/workflows/{id}/executions?page={page}&pageSize={pageSize}
```

**Parameters:**
- `id`: Workflow ID
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 10)

**Response:**
```json
{
  "items": [
    {
      "id": 25,
      "workflowId": 1,
      "workflow": {
        "id": 1,
        "name": "Data Processing Pipeline"
      },
      "status": 2,
      "startedAt": "2025-07-29T13:20:00Z",
      "completedAt": "2025-07-29T13:22:30Z",
      "durationMs": 150000,
      "currentStepIndex": 4,
      "totalSteps": 4,
      "progressPercentage": 100,
      "errorMessage": null
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 8,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

**Example:**
```bash
curl -X GET "http://localhost:5000/api/workflows/1/executions?page=1&pageSize=5"
```

## Execution Management Endpoints

### Cancel Execution

Cancels a running workflow execution.

```http
POST /api/executions/{id}/cancel
```

**Parameters:**
- `id`: Execution ID to cancel

**Response:**
```json
{
  "message": "Execution cancelled successfully"
}
```

**Example:**
```bash
curl -X POST http://localhost:5000/api/executions/25/cancel
```

**Error Responses:**
- `404`: Execution not found or cannot be cancelled

## Error Handling

### HTTP Status Codes

- `200`: Success
- `201`: Created
- `204`: No Content
- `400`: Bad Request (validation errors)
- `404`: Not Found
- `500`: Internal Server Error

### Error Response Format

```json
{
  "error": "Detailed error message",
  "status": "error",
  "statusCode": 400,
  "details": {
    "field": "Specific validation error"
  }
}
```

## Rate Limiting

Currently, no rate limiting is implemented. Consider implementing rate limiting for production use.

## Pagination

All paginated endpoints support:
- `page`: Page number (1-based)
- `pageSize`: Items per page (1-100)

Response includes:
- `totalItems`: Total number of items
- `totalPages`: Total number of pages
- `hasNextPage`: Boolean indicating if next page exists
- `hasPreviousPage`: Boolean indicating if previous page exists

## Examples in Different Languages

### JavaScript/Node.js

```javascript
// Dashboard statistics
const stats = await fetch('/api/dashboard/stats').then(r => r.json());
console.log(`Success rate: ${stats.successRate}%`);

// Create workflow
const workflow = await fetch('/api/workflows', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    name: 'JS Workflow',
    description: 'Created from JavaScript',
    isActive: true
  })
}).then(r => r.json());

// Execute workflow
const execution = await fetch(`/api/workflows/${workflow.id}/execute`, {
  method: 'POST'
}).then(r => r.json());
```

### Python

```python
import requests

# Dashboard statistics
response = requests.get('http://localhost:5000/api/dashboard/stats')
stats = response.json()
print(f"Success rate: {stats['successRate']}%")

# Create workflow
workflow_data = {
    'name': 'Python Workflow',
    'description': 'Created from Python',
    'isActive': True
}
response = requests.post('http://localhost:5000/api/workflows', json=workflow_data)
workflow = response.json()

# Execute workflow
response = requests.post(f'http://localhost:5000/api/workflows/{workflow["id"]}/execute')
execution = response.json()
```

### PowerShell

```powershell
# Dashboard statistics
$stats = Invoke-RestMethod -Uri "http://localhost:5000/api/dashboard/stats"
Write-Host "Success rate: $($stats.successRate)%"

# Create workflow
$workflowData = @{
    name = "PowerShell Workflow"
    description = "Created from PowerShell"
    isActive = $true
} | ConvertTo-Json

$workflow = Invoke-RestMethod -Uri "http://localhost:5000/api/workflows" `
    -Method POST -Body $workflowData -ContentType "application/json"

# Execute workflow
$execution = Invoke-RestMethod -Uri "http://localhost:5000/api/workflows/$($workflow.id)/execute" `
    -Method POST
```

## OpenAPI/Swagger

In development mode, interactive API documentation is available at:
```
http://localhost:5000/swagger
```

This provides:
- Interactive API testing
- Request/response examples
- Schema definitions
- Authentication configuration

---

**API Version**: 2.0.0  
**Last Updated**: July 29, 2025