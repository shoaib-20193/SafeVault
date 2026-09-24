# SafeVault Security Audit

## 1. Purpose

SafeVault is a course security project designed to demonstrate secure application development practices using a Blazor WebAssembly client, an ASP.NET Core Web API, ASP.NET Core Identity, Entity Framework Core, and SQLite.

The purpose of this audit is to document the security controls implemented in SafeVault and the vulnerabilities considered during development.

The audit focuses on the security requirements covered by the course:

- Input validation
- SQL injection prevention
- Cross-site scripting (XSS) prevention
- Authentication
- Authorization
- Security testing
- Vulnerability identification and remediation

---

## 2. Application Architecture

SafeVault uses a client/API architecture:

```text
SafeVault
│
├── SafeVault.Client
│   └── Blazor WebAssembly frontend
│
├── SafeVault.Server
│   └── ASP.NET Core Web API
│       ├── ASP.NET Core Identity
│       ├── Authorization
│       ├── Application services
│       └── Entity Framework Core
│
├── SafeVault.Shared
│   └── Shared DTOs
│
└── Database
    └── SQLite
```

The application allows authenticated users to store fictional sensitive financial records in their personal vault.

No real financial information is used.

---

## 3. Input Validation

SafeVault uses server-side DataAnnotations validation on request DTOs.

Examples include:

- Required fields
- String length restrictions
- Email format validation
- Four-digit validation for account identifiers
- Maximum length for notes

Example:

```csharp
[Required]
[StringLength(100, MinimumLength = 1)]
public string Title { get; set; } = string.Empty;
```

For the `LastFourDigits` field:

```csharp
[Required]
[RegularExpression(@"^\d{4}$")]
public string LastFourDigits { get; set; } = string.Empty;
```

Validation is performed on the server rather than relying exclusively on client-side validation.

### Security principle

Input validation is used to enforce the application's expected data format.

It is not used as a replacement for SQL injection or XSS protection.

The application does not attempt to secure SQL queries by removing characters such as quotes, angle brackets, or semicolons.

---

## 4. SQL Injection Prevention

SafeVault uses Entity Framework Core for database access.

Vault searches use LINQ:

```csharp
var records = await _context.VaultRecords
    .AsNoTracking()
    .Where(record =>
        record.OwnerId == user.Id &&
        (record.Title.Contains(query) ||
         record.Institution.Contains(query) ||
         record.AccountType.Contains(query) ||
         record.Notes.Contains(query)))
    .ToListAsync();
```

User-controlled search input is therefore treated as a query value rather than being concatenated into SQL syntax.

The project does not use unsafe SQL construction such as:

```csharp
"SELECT * FROM Users WHERE Name = '" + input + "'"
```

No unsafe `FromSqlRaw`, `ExecuteSqlRaw`, or dynamically concatenated SQL was identified in the inspected application code.

### Security testing

SQL injection payloads considered during testing included:

```text
' OR '1'='1
' OR 1=1 --
admin'--
```

The expected security property is that these values remain ordinary search input and cannot alter the structure of the database query.

---

## 5. Cross-Site Scripting Prevention

SafeVault stores vault fields as ordinary strings.

The Blazor client renders user-controlled values using normal Razor/Blazor interpolation.

For example:

```razor
<h3>@record.Title</h3>
<p>@record.Notes</p>
```

Normal framework rendering encodes the values rather than interpreting them as arbitrary HTML.

The application does not intentionally render user-controlled values through `MarkupString`.

No equivalent unsafe HTML rendering mechanism was identified in the vault UI.

### XSS payloads considered

Examples include:

```html
<script>
  alert("XSS");
</script>
```

and:

```html
<img src=x onerror=alert('XSS')>
```

The security approach is not to remove arbitrary characters from the input. Instead, untrusted content remains data and is safely encoded when rendered.

---

## 6. Authentication

SafeVault uses ASP.NET Core Identity.

Passwords are passed to Identity for password management and hashing rather than being stored directly by the application.

Authentication uses a secure cookie configuration.

Important cookie properties include:

- HttpOnly
- Secure
- SameSite
- Limited expiration lifetime
- Sliding expiration

The application uses HTTPS for client-to-server communication.

Login uses ASP.NET Core Identity's credential validation.

Invalid authentication attempts return a generic authentication error rather than revealing whether a particular email address exists.

Logout signs the user out through the Identity authentication system.

---

## 7. Authorization

SafeVault implements server-side authorization using roles and claims/policies.

Roles include:

```text
User
Admin
```

Administrative endpoints require the appropriate role.

The application also supports claims-based authorization, including the IT department policy used for the relevant administrative functionality.

Authorization is performed by the server and is therefore not dependent on the Blazor client's UI hiding or displaying buttons.

---

## 8. Resource Ownership / IDOR Protection

SafeVault applies authorization at the resource level in addition to endpoint-level authorization.

Vault records contain:

```text
OwnerId
```

The server determines the owner from the authenticated Identity user.

For example, vault retrieval uses both the requested record ID and the authenticated user's ID:

```csharp
.FirstOrDefaultAsync(record =>
    record.Id == id &&
    record.OwnerId == user.Id);
```

The same ownership restriction is applied to update and delete operations.

This prevents an authenticated user from simply changing a record ID and accessing another user's vault record.

The client cannot assign its own `OwnerId` during record creation.

Security-sensitive values such as ownership and timestamps are controlled by the server.

---

## 9. CSRF Protection

SafeVault uses cookie-based authentication, so cross-site request forgery protection is required for state-changing operations.

ASP.NET Core antiforgery services are configured with:

```text
X-CSRF-TOKEN
```

as the request-header name.

Vault POST, PUT, and DELETE operations require antiforgery validation.

The Blazor client obtains an antiforgery token and sends it in the expected request header while including authentication credentials.

### Manual verification

A normal authenticated POST request containing the required CSRF token was successfully processed:

```text
POST /api/Vault
201 Created
```

A second request was performed using the authentication cookie but intentionally omitting the `X-CSRF-TOKEN` header.

Result:

```text
400 Bad Request
```

The test record was not created.

This demonstrates that an authenticated cookie alone is insufficient to perform the protected state-changing vault operation.

---

## 10. HTTPS and Transport Security

SafeVault uses HTTPS for client/API communication.

The server enables HTTPS redirection.

The authentication cookie requires HTTPS.

The Blazor client communicates with:

```text
https://localhost:7288
```

The CORS policy permits the actual HTTPS Blazor client origin:

```text
https://localhost:7158
```

Credentials are explicitly allowed for the required client/API communication.

---

## 11. Secrets Management

A development administrator account was initially configured using credentials in repository configuration.

This was identified as a security problem because credentials should not be stored directly in source control.

The implementation was changed to use ASP.NET Core User Secrets.

The development seeder reads:

```text
AdminSeed:Email
AdminSeed:Password
```

from configuration rather than containing the password in source code.

The seeder also runs only in the Development environment.

The previously committed configuration containing the credential was removed from Git history and the cleaned repository history was force-pushed.

The current repository does not contain the development administrator password.

---

## 12. Security Testing Performed

Security testing and verification covered the following areas:

### Authentication

- Valid registration
- Duplicate registration
- Weak password rejection
- Valid login
- Incorrect password
- Nonexistent account
- Authentication cookie creation
- Logout
- Access after logout

### Authorization

- Normal user accessing user functionality
- Normal user denied administrative functionality
- Admin accessing administrative functionality
- Claims/policy authorization
- Vault ownership restrictions

### SQL Injection

Search inputs were examined using SQL injection payloads including:

```text
' OR '1'='1
' OR 1=1 --
admin'--
```

The application uses EF Core LINQ rather than dynamically constructed SQL.

### XSS

Payloads including:

```html
<script>
  alert("XSS");
</script>
```

were considered against the vault input and rendering flow.

The client uses normal Blazor rendering rather than rendering untrusted input as raw HTML.

### CSRF

A normal authenticated request with a valid CSRF token returned:

```text
201 Created
```

An authenticated request without the CSRF header returned:

```text
400 Bad Request
```

---

## 13. Security Design Principles

The implementation follows several important principles:

1. Validate input according to the expected field format.
2. Treat user input as data rather than executable code.
3. Use framework-provided password hashing rather than implementing cryptography manually.
4. Use parameterized database access.
5. Encode untrusted output.
6. Perform authorization on the server.
7. Enforce resource ownership at the database query level.
8. Do not trust client-controlled security fields.
9. Protect cookie-authenticated state-changing operations against CSRF.
10. Keep development secrets outside source control.
11. Use HTTPS for authentication and API communication.

---

## 14. Remaining Limitations

SafeVault is a course project rather than a production financial application.

The following are outside the core scope of the course implementation:

- Real financial data
- Production database encryption-at-rest architecture
- Production email infrastructure
- Multi-factor authentication
- Cloud deployment security
- Enterprise monitoring
- Production secret-management infrastructure

These limitations do not change the core security demonstrations required by the project.

---

## 15. Conclusion

SafeVault combines the course security activities into one coherent application.

The project demonstrates:

- Secure input handling
- SQL injection prevention
- XSS prevention
- Authentication
- Authorization
- Resource-level access control
- CSRF protection
- HTTPS
- Secure credential handling
- Security testing and vulnerability remediation

The security controls were implemented in the application rather than relying solely on client-side restrictions.
