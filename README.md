# User Management API

A minimal API built with ASP.NET Core that provides user management functionality with authentication, logging, and error handling middleware.

## Features

- **User CRUD Operations**: Create, read, update, and delete users
- **Authentication Middleware**: Token-based authentication using custom header
- **Request Logging**: Logs all HTTP requests and responses for auditing
- **Error Handling**: Global exception handling with structured error responses
- **Data Validation**: Comprehensive input validation using Data Annotations
- **In-Memory Storage**: Simple in-memory user storage for demonstration

## API Endpoints

### Users

| Method | Endpoint | Description | Authentication Required |
|--------|----------|-------------|------------------------|
| GET | `/users` | Get all users | ✅ |
| GET | `/users/{id}` | Get user by ID | ✅ |
| POST | `/users` | Create new user | ✅ |
| PUT | `/users/{id}` | Update user | ✅ |
| DELETE | `/users/{id}` | Delete user | ✅ |

### Logs

| Method | Endpoint | Description | Authentication Required |
|--------|----------|-------------|------------------------|
| GET | `/logs` | Get request logs | ✅ |

## Authentication

All endpoints require authentication using a custom header:

```
x-auth: auth_token
```

Requests without this header or with an invalid token will receive a `401 Unauthorized` response.

## User Model

```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+1234567890",
  "createdAt": "2025-05-31T10:00:00Z",
  "updatedAt": "2025-05-31T12:00:00Z"
}
```

### Validation Rules

- **Name**: Required, maximum 100 characters
- **Email**: Required, valid email format, maximum 100 characters, must be unique
- **Phone**: Optional, valid phone format when provided, maximum 20 characters

## Request/Response Examples

### Create User

**Request:**
```http
POST /users
x-auth: auth_token
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+1234567890"
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+1234567890",
  "createdAt": "2025-05-31T10:00:00Z",
  "updatedAt": null
}
```

### Error Response

**Response (400 Bad Request):**
```json
{
  "error": "Email already exists"
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Unauthorized",
  "statusCode": 401
}
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code

### Running the Application

1. Clone the repository
2. Navigate to the project directory
3. Run the application:

```bash
dotnet run
```

4. The API will be available at:
   - Development: `https://localhost:7000` and `http://localhost:5000`
   - Swagger UI: `https://localhost:7000/swagger` (development only)

### Testing the API

Use the provided `api_tests.http` file to test all endpoints and scenarios. The file includes comprehensive test cases for:

- Authentication scenarios
- CRUD operations
- Validation testing
- Error handling
- Edge cases

## Middleware Pipeline

The application uses the following middleware in order:

1. **Error Handling Middleware**: Catches unhandled exceptions and returns structured error responses
2. **Authentication Middleware**: Validates tokens and enforces authentication
3. **Request Logging Middleware**: Logs all requests and responses
4. **HTTPS Redirection**: Redirects HTTP to HTTPS (production only)

## Logging

The API logs all incoming requests with the following information:

- HTTP Method
- Request Path
- Response Status Code
- Timestamp

Logs can be accessed via the `/logs` endpoint (authentication required).

## Error Handling

The API provides consistent error responses:

- **400 Bad Request**: Invalid input data or validation errors
- **401 Unauthorized**: Missing or invalid authentication token
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Unhandled server errors

## Development Notes

- **In-Memory Storage**: User data is stored in memory and will be lost when the application restarts
- **Environment-Specific Behavior**: HTTPS redirection is disabled in development
- **Swagger Documentation**: Available only in development environment

## Project Structure

```
├── Program.cs                 # Main application file with endpoints and middleware
├── api_tests.http            # HTTP test file for all endpoints
└── README.md                 # This file
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly using the provided HTTP test file
5. Submit a pull request

## License

This project is for demonstration purposes.