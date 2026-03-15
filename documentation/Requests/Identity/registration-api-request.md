# Identity API — Request Examples

> Base URL (local dev): `http://localhost:5000/api/v1`

---

## POST `/auth/register`

Registers a new user and assigns them a role.

> **Note:** In production this endpoint must be restricted to `SuperAdmin` or `ClinicOwner`. It is currently open for development convenience.

---

### ✅ Register a ClinicOwner (201 Created)

```http
POST http://localhost:5000/api/v1/auth/register
Content-Type: application/json

{
  "email": "owner@demo-clinic.local",
  "password": "DemoClinic123!@#",
  "firstName": "Jane",
  "lastName": "Smith",
  "role": "ClinicOwner",
  "tenantId": null
}
```

**Response `201 Created`**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "owner@demo-clinic.local",
  "fullName": "Jane Smith",
  "role": "ClinicOwner"
}
```

---

### ✅ Register a Receptionist for a tenant (201 Created)

```http
POST http://localhost:5000/api/v1/auth/register
Content-Type: application/json

{
  "email": "receptionist@demo-clinic.local",
  "password": "Receptionist123!@#",
  "firstName": "Bob",
  "lastName": "Jones",
  "role": "Receptionist",
  "tenantId": "a1b2c3d4-0000-0000-0000-000000000001"
}
```

**Response `201 Created`**

```json
{
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "email": "receptionist@demo-clinic.local",
  "fullName": "Bob Jones",
  "role": "Receptionist"
}
```

---

### ✅ Register without specifying a role (defaults to `Receptionist`)

```http
POST http://localhost:5000/api/v1/auth/register
Content-Type: application/json

{
  "email": "newstaff@demo-clinic.local",
  "password": "NewStaff123!@#",
  "firstName": "Alice",
  "lastName": "Brown",
  "role": null,
  "tenantId": null
}
```

**Response `201 Created`**

```json
{
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "email": "newstaff@demo-clinic.local",
  "fullName": "Alice Brown",
  "role": "Receptionist"
}
```

---

### ❌ Validation error — invalid email and weak password (400 Bad Request)

```http
POST http://localhost:5000/api/v1/auth/register
Content-Type: application/json

{
  "email": "not-an-email",
  "password": "short",
  "firstName": "",
  "lastName": "",
  "role": "Receptionist",
  "tenantId": null
}
```

**Response `400 Bad Request`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["'Email' is not a valid email address."],
    "Password": [
      "The length of 'Password' must be at least 12 characters.",
      "Password must contain at least one uppercase letter.",
      "Password must contain at least one digit.",
      "Password must contain at least one non-alphanumeric character."
    ],
    "FirstName": ["'First Name' must not be empty."],
    "LastName": ["'Last Name' must not be empty."]
  }
}
```

---

### ❌ Conflict — email already registered (409 Conflict)

```http
POST http://localhost:5000/api/v1/auth/register
Content-Type: application/json

{
  "email": "superadmin@pearldesk.local",
  "password": "SuperAdmin123!@#",
  "firstName": "Super",
  "lastName": "Admin",
  "role": "SuperAdmin",
  "tenantId": null
}
```

**Response `409 Conflict`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "An account with this email already exists.",
  "status": 409
}
```

---

### ❌ Validation error — invalid role value (400 Bad Request)

```http
POST http://localhost:5000/api/v1/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "ValidPass123!@#",
  "firstName": "Test",
  "lastName": "User",
  "role": "HackerRole",
  "tenantId": null
}
```

**Response `400 Bad Request`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Role": [
      "Role must be one of: SuperAdmin, ClinicOwner, Dentist, Hygienist, Receptionist, BillingStaff, ReadOnly"
    ]
  }
}
```

---

## Available Roles

| Role | Description |
|---|---|
| `SuperAdmin` | Platform-level administrator |
| `ClinicOwner` | Owner of a tenant clinic |
| `Dentist` | Clinical staff — dentist |
| `Hygienist` | Clinical staff — hygienist |
| `Receptionist` | Front-desk staff (default if `role` is omitted) |
| `BillingStaff` | Billing and invoicing staff |
| `ReadOnly` | Read-only access to all tenant data |

---

## Password Requirements

| Rule | Requirement |
|---|---|
| Minimum length | 12 characters |
| Uppercase letter | At least 1 |
| Lowercase letter | At least 1 |
| Digit | At least 1 |
| Non-alphanumeric character | At least 1 (e.g. `!`, `@`, `#`) |

