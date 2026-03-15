﻿# Patients API — Update Patient

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`, `Receptionist`, `Dentist`, `Hygienist`

---

## PUT `/patients/{id}`

Replaces all updatable fields on an existing patient record. All fields must be supplied — omitted optional fields will be set to `null`.

### Enum values

| Field | Accepted values |
|---|---|
| `gender` | `Male`, `Female`, `NonBinary`, `Other`, `PreferNotToSay` |
| `preferredContactMethod` | `Mobile`, `Home`, `Work`, `Email`, `Sms` |

---

### ✅ Happy path — full update (200 OK)

```http
PUT http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "firstName": "Emily",
  "lastName": "Carter-Walsh",
  "preferredName": "Em",
  "dateOfBirth": "1990-04-15",
  "gender": "Female",
  "pronouns": "she/her",
  "email": "emily.walsh@example.com",
  "phoneMobile": "+1-555-0199",
  "phoneHome": null,
  "phoneWork": "+1-555-0200",
  "preferredContactMethod": "Email",
  "addressLine1": "456 Oak Avenue",
  "addressLine2": null,
  "city": "Chicago",
  "stateProvince": "IL",
  "postalCode": "60601",
  "countryCode": "US",
  "occupation": "Principal",
  "preferredProviderId": "a1b2c3d4-0000-0000-0000-000000000010",
  "smsOptIn": false,
  "emailOptIn": true,
  "notes": "Updated address — moved to Chicago."
}
```

**Response `200 OK`**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "patientNumber": "P-00042",
  "firstName": "Emily",
  "lastName": "Carter-Walsh",
  "preferredName": "Em",
  "fullName": "Emily Carter-Walsh",
  "displayName": "Em Carter-Walsh",
  "dateOfBirth": "1990-04-15",
  "gender": "Female",
  "email": "emily.walsh@example.com",
  "phoneMobile": "+1-555-0199",
  "phoneHome": null,
  "phoneWork": "+1-555-0200",
  "preferredContactMethod": "Email",
  "addressLine1": "456 Oak Avenue",
  "addressLine2": null,
  "city": "Chicago",
  "stateProvince": "IL",
  "postalCode": "60601",
  "countryCode": "US",
  "status": "Active",
  "firstVisitDate": "2025-01-10",
  "lastVisitDate": "2026-02-20",
  "recallDueDate": "2026-08-20",
  "preferredProviderId": "a1b2c3d4-0000-0000-0000-000000000010",
  "smsOptIn": false,
  "emailOptIn": true,
  "notes": "Updated address — moved to Chicago.",
  "createdAt": "2026-03-15T10:00:00Z",
  "updatedAt": "2026-03-15T14:22:00Z"
}
```

---

### ❌ Validation error — missing required fields (400 Bad Request)

```http
PUT http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
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

### ❌ Not found — patient does not exist (404)

```http
PUT http://localhost:5000/api/v1/patients/00000000-0000-0000-0000-000000000000
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "firstName": "Ghost",
  "lastName": "User",
  "smsOptIn": false,
  "emailOptIn": false
}
```

**Response `404 Not Found`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Patient not found.",
  "status": 404
}
```

---

### ❌ Unauthorized — missing or expired token (401)

```http
PUT http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "firstName": "Emily",
  "lastName": "Carter",
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

