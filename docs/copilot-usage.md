# SafeVault Copilot Usage

## 1. Purpose

Microsoft Copilot was used throughout SafeVault development as a coding assistant, debugging assistant, documentation assistant, and security-review aid.

Copilot was not treated as an authority for security decisions.

Generated suggestions were reviewed, tested, and modified before being incorporated into the project.

This document summarizes the main areas in which Copilot assisted and how the generated output was reviewed.

---

# 2. Project Planning

Copilot was used to help reason about how the three course security activities could be combined into one coherent application rather than implemented as unrelated applications.

The resulting architecture was:

```text
SafeVault.Client
        │
        ▼
SafeVault.Server
        │
        ├── Authentication
        ├── Authorization
        ├── Security validation
        └── EF Core
                │
                ▼
             SQLite
```

The developer retained responsibility for deciding which technologies and features were appropriate for the assignment.

---

# 3. Input Validation

Copilot assisted with implementing DataAnnotations for request DTOs.

Examples included:

```csharp
[Required]
[StringLength(100, MinimumLength = 1)]
public string Title { get; set; } = string.Empty;
```

and:

```csharp
[RegularExpression(@"^\d{4}$")]
public string LastFourDigits { get; set; } = string.Empty;
```

The generated validation was reviewed against the actual purpose of each field.

A key design decision was made not to use character stripping as the primary security mechanism.

Validation is used to enforce valid data formats, while SQL injection and XSS are addressed by the database and rendering layers respectively.

---

# 4. SQL Injection Prevention

Copilot was used to help implement and review database queries.

The final implementation uses EF Core LINQ.

For example:

```csharp
.Where(record =>
    record.OwnerId == user.Id &&
    (record.Title.Contains(query) ||
     record.Institution.Contains(query) ||
     record.AccountType.Contains(query) ||
     record.Notes.Contains(query)))
```

The developer reviewed the generated approach to ensure that user input was not concatenated into SQL.

SQL injection payloads were considered during testing.

The security decision was to use parameterized EF Core access rather than attempting to sanitize SQL input manually.

---

# 5. XSS Prevention

Copilot assisted with the Blazor vault interface.

During review, special attention was given to whether user-controlled vault values were rendered as ordinary text or trusted HTML.

The final implementation uses normal Blazor interpolation:

```razor
@record.Title
@record.Notes
```

Unsafe rendering approaches such as deliberately converting untrusted input into `MarkupString` were not used.

The developer reviewed the security implications rather than accepting generated UI code without inspection.

---

# 6. Authentication

Copilot assisted with ASP.NET Core Identity integration and authentication-related implementation.

The project uses:

```text
ASP.NET Core Identity
IdentityUser/ApplicationUser
UserManager
SignInManager
IdentityRole
```

rather than a custom password hashing implementation.

The developer reviewed the authentication flow to ensure:

- Passwords are not manually stored.
- Identity performs password hashing.
- Invalid credentials are handled generically.
- Authentication cookies use secure settings.
- Logout signs the user out correctly.

Authentication behavior was manually tested after implementation.

---

# 7. Authorization

Copilot assisted with authorization implementation and endpoint structure.

The final application uses:

```csharp
[Authorize]
```

and role-based authorization such as:

```csharp
[Authorize(Roles = "Admin")]
```

Claims/policies were also incorporated where appropriate.

The developer reviewed the generated authorization logic to ensure that authorization was enforced on the server rather than merely hiding UI elements.

---

# 8. Resource-Level Authorization

A significant security review was performed around vault ownership.

The application does not rely only on:

```csharp
[Authorize]
```

because authentication alone does not prove that a user owns a requested record.

Vault queries therefore include the authenticated user's ID:

```csharp
record.OwnerId == user.Id
```

The server also assigns the owner during creation.

The developer specifically reviewed this behavior to prevent IDOR and mass-assignment vulnerabilities.

---

# 9. CSRF Protection

Copilot assisted with the implementation of ASP.NET Core antiforgery protection and the Blazor client token flow.

The final design uses:

```text
ASP.NET Core Antiforgery
        ↓
Request token
        ↓
X-CSRF-TOKEN header
        ↓
ValidateAntiforgeryToken
```

During debugging, an initial antiforgery configuration problem was identified when controller antiforgery validation required the appropriate MVC services.

The server configuration was adjusted to use:

```csharp
builder.Services.AddControllersWithViews();
```

The resulting implementation was then tested.

A normal authenticated request with the CSRF token returned:

```text
201 Created
```

A request with the authentication cookie but without the CSRF token returned:

```text
400 Bad Request
```

This manual verification was used to confirm that the security mechanism was actually being enforced.

---

# 10. HTTPS and Cookie Security

Copilot assisted with configuring HTTPS client/API communication and secure authentication cookies.

The final cookie configuration includes:

```text
HttpOnly
Secure
SameSite
Expiration
Sliding expiration
```

The Blazor client communicates with the HTTPS API.

CORS was updated to use the HTTPS client origin.

The developer tested authentication and CRUD operations after the changes.

---

# 11. Secret Management

Copilot assisted with identifying and restructuring development configuration around administrator seeding.

The developer determined that a development administrator password should not be committed to the repository.

The implementation was changed to use ASP.NET Core User Secrets.

The developer then independently performed Git history cleanup to remove the previously committed configuration containing the credential.

The repository was subsequently force-pushed with the cleaned history.

---

# 12. Debugging

Copilot was also used during debugging when implementation issues appeared.

Examples included:

- ASP.NET Core service-registration problems.
- Antiforgery configuration.
- Client/API HTTPS configuration.
- Authentication and authorization behavior.
- Blazor API integration.
- CRUD request handling.

Generated suggestions were tested against the actual application.

When a suggestion conflicted with the project's architecture or security model, it was modified or rejected.

---

# 13. Security Review of Copilot Output

Security-sensitive generated code was not accepted solely because Copilot produced it.

The developer reviewed:

- authentication boundaries
- authorization boundaries
- database queries
- ownership checks
- user-controlled input
- HTML rendering
- cookies
- CSRF tokens
- configuration and secrets

Particular attention was given to avoiding common misconceptions such as:

```text
"Remove malicious characters to stop SQL injection."
```

and:

```text
"Remove <script> to stop XSS."
```

The final implementation instead uses appropriate framework-level security mechanisms.

---

# 14. Developer Responsibility

Copilot was used as an assistant rather than as an autonomous security authority.

The developer remained responsible for:

- selecting the architecture
- deciding which security controls were required
- reviewing generated code
- debugging implementation issues
- testing security behavior
- deciding whether suggested approaches were appropriate
- removing insecure approaches
- documenting security decisions

The final implementation was reviewed against the course requirements before submission.

---

# 15. Example Copilot Prompt Categories

The following are representative categories of prompts used during development:

### Architecture

```text
Review this SafeVault architecture and suggest how the authentication,
authorization, validation, and database layers should interact.
```

### SQL Injection

```text
Review this EF Core query for SQL injection risks.
Explain whether user-controlled input can alter the SQL structure.
```

### XSS

```text
Review this Blazor component for XSS risks involving user-controlled
vault fields. Identify any unsafe rendering patterns.
```

### Authorization

```text
Review this controller for IDOR and broken access-control vulnerabilities.
Verify that users can only access their own vault records.
```

### Authentication

```text
Review this ASP.NET Core Identity login and registration implementation
for authentication security problems.
```

### CSRF

```text
Review this cookie-authenticated API for CSRF vulnerabilities and explain
how antiforgery protection should be integrated with a Blazor WebAssembly client.
```

### Security Review

```text
Audit the SafeVault repository against the course security requirements.
Identify concrete vulnerabilities, missing controls, and evidence for each finding.
```

These prompts illustrate the role of Copilot in development and review. The developer independently evaluated the resulting code and performed runtime verification.

---

# 16. Summary

Copilot contributed to:

- Architecture planning
- Code generation
- Security review
- Debugging
- Authentication implementation
- Authorization implementation
- Input validation
- Database query review
- XSS review
- CSRF implementation
- HTTPS configuration
- Documentation

The developer reviewed and tested security-sensitive suggestions before accepting them.

SafeVault therefore uses Copilot as a development and review tool while retaining human responsibility for security decisions and final implementation.
