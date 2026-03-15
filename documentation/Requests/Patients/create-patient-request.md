﻿# Patients API — Create Patient

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`, `Receptionist`, `Dentist`, `Hygienist`

---

## POST `/patients`

Creates a new patient record for the current tenant.

### Enum values

| Field | Accepted values |
|---|---|
| `gender` | `Male`, `Female`, `NonBinary`, `Other`, `PreferNotToSay` |
| `preferredContactMethod` | `Mobile`, `Home`, `Work`, `Email`, `Sms` |

---

### ✅ Happy path — full patient record (201 Created)

```http
POST http://localhost:5000/api/v1/patients
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "firstName": "Emily",
  "lastName": "Carter",
  "preferredName": "Em",
  "dateOfBirth": "1990-04-15",
  "gender": "Female",
  "email": "emily.carter@example.com",
  "phoneMobile": "+1-555-0101",
  "phoneHome": null,
  "phoneWork": null,
  "preferredContactMethod": "Mobile",
  "addressLine1": "123 Maple Street",
  "addressLine2": "Apt 4B",
  "city": "Springfield",
  "stateProvince": "IL",
  "postalCode": "62701",
  "countryCode": "US",
  "occupation": "Teacher",
  "preferredProviderId": "a1b2c3d4-0000-0000-0000-000000000010",
  "smsOptIn": true,
  "emailOptIn": true,
  "referredBySource": "Google",
  "notes": "Patient prefers morning appointments."
}
```

**Response `201 Created`**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "patientNumber": "P-00042",
  "firstName": "Emily",
  "lastName": "Carter",
  "preferredName": "Em",
  "fullName": "Emily Carter",
  "displayName": "Em Carter",
  "dateOfBirth": "1990-04-15",
  "gender": "Female",
  "email": "emily.carter@example.com",
  "phoneMobile": "+1-555-0101",
  "phoneHome": null,
  "phoneWork": null,
  "preferredContactMethod": "Mobile",
  "addressLine1": "123 Maple Street",
  "addressLine2": "Apt 4B",
  "city": "Springfield",
  "stateProvince": "IL",
  "postalCode": "62701",
  "countryCode": "US",
  "status": "Active",
  "firstVisitDate": null,
  "lastVisitDate": null,
  "recallDueDate": null,
  "preferredProviderId": "a1b2c3d4-0000-0000-0000-000000000010",
  "smsOptIn": true,
  "emailOptIn": true,
  "notes": "Patient prefers morning appointments.",
  "createdAt": "2026-03-15T10:00:00Z",
  "updatedAt": null
}
```

---

### ✅ Happy path — minimal required fields only (201 Created)

```http
POST http://localhost:5000/api/v1/patients
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "firstName": "John",
  "lastName": "Doe",
  "preferredName": null,
  "dateOfBirth": null,
  "gender": null,
  "email": null,
  "phoneMobile": null,
  "phoneHome": null,
  "phoneWork": null,
  "preferredContactMethod": null,
  "addressLine1": null,
  "addressLine2": null,
  "city": null,
  "stateProvince": null,
  "postalCode": null,
  "countryCode": null,
  "occupation": null,
  "preferredProviderId": null,
  "smsOptIn": false,
  "emailOptIn": false,
  "referredBySource": null,
  "notes": null
}
```

**Response `201 Created`**

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "patientNumber": "P-00043",
  "firstName": "John",
  "lastName": "Doe",
  "preferredName": null,
  "fullName": "John Doe",
  "displayName": "John Doe",
  "dateOfBirth": null,
  "gender": null,
  "email": null,
  "phoneMobile": null,
  "phoneHome": null,
  "phoneWork": null,
  "preferredContactMethod": null,
  "addressLine1": null,
  "addressLine2": null,
  "city": null,
  "stateProvince": null,
  "postalCode": null,
  "countryCode": null,
  "status": "Active",
  "firstVisitDate": null,
  "lastVisitDate": null,
  "recallDueDate": null,
  "preferredProviderId": null,
  "smsOptIn": false,
  "emailOptIn": false,
  "notes": null,
  "createdAt": "2026-03-15T10:01:00Z",
  "updatedAt": null
}
```

---

### ❌ Validation error — missing required fields (400 Bad Request)

```http
POST http://localhost:5000/api/v1/patients
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "firstName": "",
  "lastName": "",
  "smsOptIn": false,
  "emailOptIn": false
}
```

**Response `400 Bad Request`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "FirstName": ["'First Name' must not be empty."],
    "LastName": ["'Last Name' must not be empty."]
  }
}
```

---

### ❌ Unauthorized — missing or expired token (401)

```http
POST http://localhost:5000/api/v1/patients
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Smith",
  "smsOptIn": false,
  "emailOptIn": false
}
```

**Response `401 Unauthorized`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

