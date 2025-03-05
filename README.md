# Authentication_And_Authorization

## Authentication And Authorization With Identity in ASP.NET Core 8

### Overview
This project demonstrates authentication and authorization using Identity in ASP.NET Core 8. It provides two main endpoints for user authentication:

- **Login**  
- **Register**

### Refresh Token Feature
This project includes a **Refresh Token** mechanism to enhance security and user experience in token-based authentication. Refresh tokens allow users to obtain a new access token without re-entering their credentials, maintaining a seamless and secure session.

#### Description
- **Purpose**: Extends the lifetime of an authenticated session by issuing a long-lived refresh token alongside a short-lived access token (JWT).  
- **How It Works**:  
  - Upon successful login, the user receives both an access token and a refresh token.  
  - The access token is used for API authentication and expires quickly (e.g., 15 minutes).  
  - When the access token expires, the client can use the refresh token to request a new access token from a dedicated endpoint (e.g., `/api/auth/refresh`).  
  - Refresh tokens are securely stored and validated server-side, with an expiration period (e.g., 7 days).  
- **Security**: Refresh tokens are revoked upon logout or if compromised, and they use secure storage (e.g., HTTP-only cookies or database).  
- **Endpoint**: `POST /api/auth/refresh`  
  - **Request**: `{ "refreshToken": "your-refresh-token" }`  
  - **Response**: `{ "accessToken": "new-jwt-token", "refreshToken": "new-refresh-token" }`  

### Features
- User authentication using ASP.NET Core Identity  
- Secure password storage and validation  
- JWT-based authentication with refresh token support  
- Role-based authorization  
- User registration and login functionality  

### Technologies Used
- **ASP.NET Core 8**  
- **Identity Framework**  
- **JWT (JSON Web Token)**  
- **Entity Framework Core**  
- **SQL Server**
