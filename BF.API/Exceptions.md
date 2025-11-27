# Exception types & when to use them

Use these everywhere to keep things simple:

### ArgumentException (or ArgumentNullException)
Bad input / contract violation:
- Missing required fields
- Invalid enum values
- Malformed IDs

Never put PHI in the message.
> Example: "Invalid visit date range.", not "Invalid visit date range for patient John Doe."

### UnauthorizedAccessException

Tenant mismatch / access violation:

- Tenant id in claims doesn’t match the resource tenant
- User lacks role to perform the operation

> Example: "User not authorized to access this practice."

### KeyNotFoundException

Resource doesn’t exist (404):

- Patient not found
- Encounter not found
- Coverage enrollment not found

Again: no PHI in messages.

> Example: "Patient not found." or "Coverage enrollment not found."

### Custom Exceptions

Optionally, you can introduce one or two domain exceptions:

- **ValidationException** (if you want richer validation semantics)
→ Map to 400 in middleware, same as ArgumentException.
- **ConflictException** (for concurrency/duplicate issues)
→ Map to 409 in middleware.

But you don’t need custom types to get started—the three built-ins above are enough.

# Middleware mapping rules (tightened)

In your ExceptionHandlingMiddleware.HandleExceptionAsync, use a simple mapping:

```c#
switch (exception)
{
    case ArgumentException:
        statusCode = HttpStatusCode.BadRequest;
        clientMessage = "The request was invalid.";
        break;

    // If you add a ValidationException type:
    // case ValidationException:
    //     statusCode = HttpStatusCode.BadRequest;
    //     clientMessage = "The request was invalid.";
    //     break;

    case UnauthorizedAccessException:
        statusCode = HttpStatusCode.Unauthorized;
        clientMessage = "Not authorized.";
        break;

    case KeyNotFoundException:
        statusCode = HttpStatusCode.NotFound;
        clientMessage = "Resource not found.";
        break;

    // Optional domain conflict:
    // case ConflictException:
    //     statusCode = HttpStatusCode.Conflict;
    //     clientMessage = "The request could not be completed due to a conflict.";
    //     break;

    default:
        statusCode = HttpStatusCode.InternalServerError;
        clientMessage = "A server error occurred. Please contact support with this ID.";
        break;
}
```

Everything else (log detail, errorId, etc.) stays the same.

# Tighten service interfaces: stop using nullable / bool for existence

Right now some service methods return:

- PatientDetailDto?
- bool for delete success

To fully embrace middleware-style, change semantics to:

- **Methods that read a single resource**
→ return non-null type and throw KeyNotFoundException if missing.
- **Methods that update / delete**
→ return non-null type or void and throw KeyNotFoundException if the record doesn’t exist.
- **Methods that search / list**
→ never throw KeyNotFoundException – return an empty list/page.

So your interfaces evolve like:


```c#
// Before
Task<PatientDetailDto?> GetPatientAsync(string tenantId, string patientId);
Task<PatientDetailDto?> UpdatePatientAsync(string tenantId, string patientId, PatientUpdateRequestDto request);
Task<bool> DeleteCoverageEnrollmentAsync(string tenantId, string patientId, string coverageEnrollmentId);

// After (clean)
Task<PatientDetailDto> GetPatientAsync(string tenantId, string patientId);
Task<PatientDetailDto> UpdatePatientAsync(string tenantId, string patientId, PatientUpdateRequestDto request);
Task DeleteCoverageEnrollmentAsync(string tenantId, string patientId, string coverageEnrollmentId);
```


Then controllers become even dumber:

- They never check for null / false
- They assume success if no exception is thrown
- The middleware turns thrown KeyNotFoundException into 404 responses.