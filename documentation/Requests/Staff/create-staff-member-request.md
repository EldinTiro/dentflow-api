# Staff API — Create Staff Member

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`

---

## POST `/staff`

Creates a new staff member record for the current tenant.

### Enum values

| Field | Accepted values |
|---|---|
| `staffType` | `Dentist`, `DentalAssistant`, `Hygienist`, `Receptionist`, `ClinicAdmin`, `OfficeManager` |

---

### ✅ Happy path — full record (201 Created)

```http
POST http://localhost:5000/api/v1/staff
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "staffType": "Dentist",
  "firstName": "Sarah",
  "lastName": "Nguyen",
  "email": "sarah.nguyen@demo-clinic.local",
  "phone": "+1-555-0201",
  "hireDate": "2024-03-01",
  "specialty": "Orthodontics",
  "colorHex": "#3B82F6"
}
```

**Response `201 Created`**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "staffType": "Dentist",
  "firstName": "Sarah",
  "lastName": "Nguyen",
  "fullName": "Sarah Nguyen",
  "email": "sarah.nguyen@demo-clinic.local",
  "phone": "+1-555-0201",
  "specialty": "Orthodontics",
  "colorHex": "#3B82F6",
  "biography": null,
  "licenseNumber": null,
  "licenseExpiry": null,
  "npiNumber": null,
  "isActive": true,
  "hireDate": "2024-03-01",
  "terminationDate": null,
  "createdAt": "2026-03-15T10:00:00Z",
  "updatedAt": null
}
```

---

### ✅ Happy path — minimal fields (201 Created)

```http
POST http://localhost:5000/api/v1/staff
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "staffType": "Receptionist",
  "firstName": "Tom",
  "lastName": "Baker",
  "email": null,
  "phone": null,
  "hireDate": null,
  "specialty": null,
  "colorHex": null
}
```

**Response `201 Created`**

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "staffType": "Receptionist",
  "firstName": "Tom",
  "lastName": "Baker",
  "fullName": "Tom Baker",
  "email": null,
  "phone": null,
  "specialty": null,
  "colorHex": "#3B82F6",
  "isActive": true,
  "hireDate": null,
  "createdAt": "2026-03-15T10:01:00Z",
  "updatedAt": null
}
```

---

### ❌ Validation error — missing required fields (400 Bad Request)

```http
POST http://localhost:5000/api/v1/staff
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "staffType": "Dentist",
  "firstName": "",
  "lastName": ""
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
POST http://localhost:5000/api/v1/staff
Content-Type: application/json

{
  "staffType": "Dentist",
  "firstName": "Jane",
  "lastName": "Doe"
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

---

### ❌ Forbidden — insufficient role (403)

```http
POST http://localhost:5000/api/v1/staff
Content-Type: application/json
Authorization: Bearer <receptionist_access_token>

{
  "staffType": "Dentist",
  "firstName": "Jane",
  "lastName": "Doe"
}
```

**Response `403 Forbidden`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```

