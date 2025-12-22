# API Scope Parameter Strategy

## Rule: Scope Identifiers Come From Context, Not Request Body

### ✅ DO:
- Extract `tenantId` from JWT claims
- Extract `practiceId` from route parameters
- Validate practice access against claims
- Include resource IDs (`patientId`, `encounterId`) in route when resource is the subject

### ❌ DON'T:
- Include `tenantId` in request DTOs
- Include `practiceId` in request DTOs (when route has it)
- Allow clients to specify scope in request body

### Exception:
- `patientId` in `EncounterCreateRequestDto` is appropriate because encounters 
  are NOT nested under `/patients/{patientId}/encounters`