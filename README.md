# SafeVault

SafeVault is a security-focused web application developed as a .NET 10 full-stack project. It demonstrates practical application-security controls for a fictional financial-record vault, including input validation, SQL injection prevention, XSS protection, authentication, authorization, and CSRF protection.

The project consolidates the security concepts covered throughout the course activities into one coherent application.

## Project Overview

SafeVault allows authenticated users to:

- Register and log in securely.
- Access their authenticated profile.
- Create, view, search, update, and delete vault records.
- Access only records that belong to their own account.
- Use role-based authorization for protected endpoints.

The application uses a separate Blazor WebAssembly client and ASP.NET Core Web API server.

## Technology Stack

- **.NET 10**
- **ASP.NET Core Web API**
- **Blazor WebAssembly**
- **ASP.NET Core Identity**
- **Entity Framework Core**
- **SQLite**
- **Cookie-based authentication**
- **Swagger / OpenAPI** for development
- **Git / GitHub**

## Project Structure

```text
SafeVault/
├── SafeVault.sln
├── src/
│   ├── SafeVault.Client/       # Blazor WebAssembly frontend
│   ├── SafeVault.Server/       # ASP.NET Core Web API
│   └── SafeVault.Shared/       # Shared DTOs
├── tests/
│   ├── SafeVault.UnitTests/
│   └── SafeVault.IntegrationTests/
└── docs/
    ├── security-audit.md
    ├── vulnerability-debugging.md
    └── copilot-usage.md
```

> The `tests` directories are part of the planned project structure. The current security verification documented in this repository is primarily manual runtime testing.

## Architecture

```text
                    Browser
                       │
                       ▼
             Blazor WebAssembly
                    Client
                       │
                       │ HTTPS
                       │ Authentication Cookie
                       │ CSRF Token
                       ▼
              ASP.NET Core Web API
                       │
          ┌────────────┼────────────┐
          │            │            │
          ▼            ▼            ▼
   Authentication  Authorization  Validation
          │            │            │
          └────────────┼────────────┘
                       │
                       ▼
                Resource Ownership
                       │
                       ▼
                 Entity Framework
                    Core / LINQ
                       │
                       ▼
                    SQLite
```

## Security Features

### 1. Input Validation

User-controlled vault data is received through dedicated request DTOs rather than directly binding database entities.

Validation includes:

- Required fields.
- Minimum and maximum string lengths.
- Four-digit validation for `LastFourDigits`.
- ASP.NET Core model validation on API requests.

Example:

```csharp
[Required]
[StringLength(100, MinimumLength = 1)]
public string Title { get; set; } = string.Empty;

[Required]
[RegularExpression(@"^\d{4}$")]
public string LastFourDigits { get; set; } = string.Empty;
```

Validation is used to establish acceptable data boundaries and reject malformed input.

It is **not** treated as the primary defense against SQL injection.

---

### 2. SQL Injection Prevention

Database access is performed through Entity Framework Core LINQ queries rather than dynamically constructed SQL strings.

For example, vault search uses a LINQ expression:

```csharp
.Where(record =>
    record.OwnerId == user.Id &&
    record.Title.Contains(query))
```

Entity Framework Core translates the expression into a parameterized database operation.

The project does not attempt to prevent SQL injection by removing characters such as:

```text
'
--
;
```

Instead, the application uses parameterized database access through EF Core.

This is important because input filtering alone is not a reliable SQL injection defense.

---

### 3. Cross-Site Scripting (XSS) Prevention

Vault fields such as titles, institutions, account types, and notes contain user-controlled data.

The Blazor client displays these values using normal Razor/Blazor rendering.

For example:

```razor
@record.Notes
```

The application does not intentionally convert untrusted vault content into raw HTML using `MarkupString`.

The application therefore relies on the framework's normal HTML encoding behavior rather than treating user-provided text as trusted HTML.

---

### 4. Authentication

ASP.NET Core Identity manages user accounts and password security.

The application uses cookie-based authentication with a dedicated authentication cookie.

Security-related cookie settings include:

- `HttpOnly`
- `Secure`
- `SameSite=Lax`
- 60-minute expiration
- Sliding expiration

The API provides:

- User registration
- Login
- Current-user information
- Logout

Authentication is enforced by the server.

The client does not determine whether a user is authenticated.

---

### 5. Authorization

Authorization is implemented at multiple levels.

#### Role-Based Authorization

Protected endpoints use ASP.NET Core authorization.

Example:

```csharp
[Authorize(Roles = "User,Admin")]
```

Administrative endpoints require the `Admin` role, while user-specific endpoints can require the `User` role.

#### Resource Ownership

Each vault record contains an `OwnerId`.

Vault queries are scoped to the currently authenticated user:

```csharp
.Where(record => record.OwnerId == user.Id)
```

This ownership check is applied to:

- Listing records
- Retrieving a record
- Searching
- Updating
- Deleting

Therefore, knowing another record's numeric ID is not sufficient to access it.

#### Claims-Based Authorization

The application also demonstrates claims-based authorization using a department claim:

```text
Department = IT
```

An IT-only authorization policy can require this claim before allowing access.

---

### 6. Cross-Site Request Forgery (CSRF) Protection

Because SafeVault uses cookie-based authentication, state-changing requests require CSRF protection.

The server configures an antiforgery header:

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
```

The client obtains an antiforgery token from the server and sends it with protected:

- POST
- PUT
- DELETE

requests.

A state-changing request containing the authentication cookie but missing the required CSRF token is rejected by the server.

The negative CSRF test produced:

```text
HTTP 400 Bad Request
```

while the corresponding authenticated request with a valid CSRF token succeeded.

---

### 7. HTTPS

The application is configured to use HTTPS during development.

The development API runs on:

```text
https://localhost:7288
```

The Blazor WebAssembly client runs on:

```text
https://localhost:7158
```

The authentication cookie uses:

```csharp
SecurePolicy.Always
```

The server also uses HTTPS redirection.

---

### 8. Secure Credential Handling

Development administrator credentials are not stored directly in the source code.

The development seeder reads:

```text
AdminSeed:Email
AdminSeed:Password
```

from configuration.

The actual values are stored using .NET User Secrets.

The project previously contained a development configuration file with administrator credentials. This was remediated by:

1. Moving the credentials to .NET User Secrets.
2. Removing the credential-containing configuration file.
3. Removing the file from Git history using `git-filter-repo`.
4. Reconnecting the repository's remote.
5. Force-pushing the cleaned history with `--force-with-lease`.

The current repository does not contain the development password.

---

## API Security Model

The server controls security-sensitive properties instead of accepting them from the client.

When a vault record is created, the client provides:

```text
Title
Institution
AccountType
LastFourDigits
Notes
```

The server determines:

```text
OwnerId
CreatedAt
UpdatedAt
```

Conceptually:

```text
Client-controlled data
        │
        ▼
   Request DTO
        │
        ▼
 Server-side validation
        │
        ▼
Authenticated user
        │
        ├── OwnerId
        ├── CreatedAt
        └── UpdatedAt
```

This prevents clients from assigning records to other users or manipulating server-managed properties.

---

## Protection Against Mass Assignment

The create and update request DTOs do not expose security-sensitive properties such as:

```text
OwnerId
CreatedAt
UpdatedAt
```

For example, the client cannot legitimately send:

```json
{
  "title": "Example",
  "ownerId": "another-user-id"
}
```

and cause the server to assign the record to that user.

The server determines ownership from the authenticated identity.

---

## Security Testing

Security behavior has been verified through manual runtime testing.

The following areas have been tested:

| Security Area          | Verification                                               |
| ---------------------- | ---------------------------------------------------------- |
| Registration           | Valid registration succeeds                                |
| Duplicate registration | Duplicate account is rejected                              |
| Password policy        | Weak passwords are rejected                                |
| Login                  | Valid credentials authenticate                             |
| Invalid login          | Incorrect/nonexistent credentials return a generic failure |
| Authentication cookie  | Cookie is configured as HttpOnly and Secure                |
| Current user           | Authenticated `/api/Auth/me` succeeds                      |
| Logout                 | Session is invalidated                                     |
| Role authorization     | Unauthorized roles receive HTTP 403                        |
| IT claim policy        | Required IT claim grants access                            |
| Vault ownership        | Users can access only their own records                    |
| Create                 | Authenticated users can create records                     |
| Read                   | Authenticated users can read their records                 |
| Update                 | Authenticated users can update their records               |
| Delete                 | Authenticated users can delete their records               |
| Search                 | Search remains scoped to the current user                  |
| CSRF positive case     | Authenticated request with token succeeds                  |
| CSRF negative case     | Authenticated request without token returns HTTP 400       |

Detailed results are documented in:

[`docs/security-testing.md`](docs/security-testing.md)

---

## Vulnerability Debugging

Security-related implementation problems encountered during development were investigated and corrected.

### Identity Database Migration

The application initially failed because the Identity roles table did not exist.

The error indicated that:

```text
AspNetRoles
```

was missing.

EF Core migrations were checked and an initial Identity migration was created:

```text
InitialIdentity
```

The migration was then applied to the database.

After the migration was applied, Identity roles and the development administrator could be seeded successfully.

### Antiforgery Service Registration

Adding:

```csharp
[ValidateAntiForgeryToken]
```

initially produced a service-registration error because the required MVC antiforgery filter services were not registered.

The server configuration was changed from:

```csharp
AddControllers()
```

to:

```csharp
AddControllersWithViews()
```

This provided the required antiforgery MVC services.

### HTTP/HTTPS Configuration

The client was configured to communicate with the HTTPS API:

```text
https://localhost:7288
```

This ensures that the development client and server use the same secure transport expected by the authentication configuration.

---

## Development Administrator Seeding

The development environment can seed:

```text
User
Admin
```

roles.

An administrator can also be created from development configuration when the appropriate User Secrets are present.

The seeder is restricted to the Development environment.

Production environments do not automatically receive the development administrator seed behavior.

---

## Documentation

Additional project documentation is available in the `docs` directory.

### Security Audit

[`docs/security-audit.md`](docs/security-audit.md)

Documents:

- Security risks
- Mitigations
- Authentication security
- Authorization security
- SQL injection prevention
- XSS prevention
- CSRF protection
- Credential exposure remediation
- Remaining hardening opportunities

### Security Testing

[`docs/security-testing.md`](docs/security-testing.md)

Documents:

- Authentication tests
- Authorization tests
- Ownership tests
- CRUD tests
- CSRF tests
- Input validation testing
- SQL injection verification
- XSS verification
- Manual versus automated testing

### Threat Model

[`docs/threat-model.md`](docs/threat-model.md)

Documents:

- Assets
- Actors
- Trust boundaries
- Threats
- Mitigations
- Defense-in-depth principles
- Residual risks

### Copilot Usage

[`docs/copilot-usage.md`](docs/copilot-usage.md)

Documents:

- Areas where Copilot/AI assistance was used
- Representative prompts
- Human review
- Security verification
- Corrections made during development

---

## Running the Project

### Prerequisites

Install:

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Git
- A modern web browser

### Clone the Repository

```bash
git clone https://github.com/shoaib-20193/SafeVault.git
cd SafeVault
```

### Restore Dependencies

```bash
dotnet restore
```

### Apply Database Migration

```bash
dotnet ef database update --project src/SafeVault.Server
```

If the EF CLI is not installed:

```bash
dotnet tool install --global dotnet-ef
```

### Configure Development Secrets

SafeVault uses .NET User Secrets for the development administrator credentials.

Initialize User Secrets if necessary:

```bash
dotnet user-secrets init --project src/SafeVault.Server
```

Set the administrator email:

```bash
dotnet user-secrets set "AdminSeed:Email" "your-admin-email@example.com" --project src/SafeVault.Server
```

Set the administrator password:

```bash
dotnet user-secrets set "AdminSeed:Password" "your-development-password" --project src/SafeVault.Server
```

Use your own development credentials.

Do not commit these values to Git.

### Run the Server

```bash
dotnet run --project src/SafeVault.Server
```

The development HTTPS API is configured for:

```text
https://localhost:7288
```

Swagger/OpenAPI is available when the server is running in the Development environment.

### Run the Client

Open a second terminal:

```bash
dotnet run --project src/SafeVault.Client
```

The development HTTPS client is configured for:

```text
https://localhost:7158
```

The client communicates with the API over HTTPS and includes browser credentials for cookie-based authentication.

---

## Repository Security Practices

The repository should not contain:

- Passwords
- User Secrets
- Private keys
- Development credentials
- Local database files
- Build output
- IDE-generated files

Sensitive development configuration should remain outside source control.

Before committing changes, review:

```bash
git status
```

and inspect staged changes before pushing:

```bash
git diff --cached
```

---

## Assignment Coverage

SafeVault demonstrates the five core security capabilities covered by the project:

### 1. Input Validation

Implemented through DataAnnotations and dedicated request DTOs.

### 2. SQL Injection Prevention

Implemented through Entity Framework Core LINQ and parameterized database access.

### 3. XSS Prevention

Implemented through safe framework rendering and avoiding raw HTML rendering of untrusted input.

### 4. Authentication

Implemented using ASP.NET Core Identity and secure cookie-based authentication.

### 5. Authorization

Implemented using:

- Roles
- Claims
- Resource ownership checks

The project also demonstrates:

- CSRF protection
- HTTPS
- Secure credential handling
- Vulnerability debugging
- Security testing
- Copilot-assisted development
- Human security review

---

## Security Testing: Automated vs Manual

The security verification currently documented in this repository is primarily **manual runtime testing**.

This distinction is intentional.

### Manual Testing

Manual testing verifies actual application behavior by sending requests through the running application and observing the results.

Examples include:

```text
Valid CSRF token
        ↓
HTTP 201 Created
```

and:

```text
Missing CSRF token
        ↓
HTTP 400 Bad Request
```

Authentication, authorization, CRUD, and ownership behavior were also manually verified.

### Automated Testing

Automated unit or integration tests would execute security checks through a test framework such as xUnit and could be run repeatedly through the command line.

The assignment material used for this project explicitly requires **security testing**, but it does not explicitly state that automated unit/integration tests are a separate mandatory deliverable.

Automated security tests would therefore be an additional engineering improvement unless the final course rubric specifically requires them.

---

## Scope and Limitations

SafeVault is an educational security project and is not intended to be deployed as a production financial-management system.

The application models fictional financial-record metadata rather than storing:

- Real banking credentials
- Full payment-card numbers
- Bank account passwords
- Real financial transactions

A production deployment would require additional security controls, including:

- Production-grade secret management
- Centralized security logging
- Monitoring and alerting
- Rate limiting
- Account lockout and abuse protection
- Security headers
- Dependency vulnerability scanning
- Production certificate management
- Database backup and recovery
- Formal penetration testing
- Production deployment hardening

---

## Future Hardening Opportunities

The following are potential improvements beyond the current educational scope:

- Add a maximum length to the vault search query.
- Consider antiforgery protection for logout as additional defense in depth.
- Add rate limiting to authentication endpoints.
- Add centralized security logging and monitoring.
- Add automated dependency and vulnerability scanning.
- Add automated unit/integration security regression tests.
- Use a dedicated production secret-management service.
- Perform formal penetration testing before handling real sensitive information.

---

## Project Status

The current implementation includes:

- Secure user registration
- Secure login/logout
- ASP.NET Core Identity
- Role-based authorization
- Claims-based authorization
- User-owned vault records
- CRUD operations
- Search
- Input validation
- EF Core database access
- SQL injection mitigation
- XSS mitigation
- CSRF protection
- HTTPS
- Secure development credential handling
- Security testing
- Security documentation

---

## License

This project was created for educational purposes as part of a security-focused software development course.
