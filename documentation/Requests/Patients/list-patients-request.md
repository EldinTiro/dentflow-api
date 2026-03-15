﻿# Patients API — List Patients

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`, `Receptionist`, `Dentist`, `Hygienist`

---

## GET `/patients`

Returns a paginated list of patients for the current tenant. Supports optional search by name/email and filter by status.

### Query Parameters

| Parameter  | Type     | Required | Default | Description                                          |
|------------|----------|----------|---------|------------------------------------------------------|
| `search`   | `string` | No       | —       | Searches across first name, last name, and email     |
| `status`   | `string` | No       | —       | Filter by status enum: `Active`, `Inactive`, `Deceased`, `Transferred` |
| `page`     | `int`    | No       | `1`     | Page number (1-based)                                |
| `pageSize` | `int`    | No       | `20`    | Results per page (max `100`)                         |

---

### ✅ Happy path — default list, first page (200 OK)

```http
GET http://localhost:5000/api/v1/patients
Authorization: Bearer <access_token>
```

**Response `200 OK`**

```json
{
  "items": [
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
      "status": "Active",
      "createdAt": "2026-03-15T10:00:00Z",
      "updatedAt": null
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

### ✅ Happy path — search by name (200 OK)

```http
GET http://localhost:5000/api/v1/patients?search=carter&page=1&pageSize=10
Authorization: Bearer <access_token>
```

**Response `200 OK`**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "patientNumber": "P-00042",
      "firstName": "Emily",
      "lastName": "Carter",
      "fullName": "Emily Carter",
      "displayName": "Em Carter",
      "status": "Active",
      "createdAt": "2026-03-15T10:00:00Z",
      "updatedAt": null
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

### ✅ Happy path — filter by status (200 OK)

```http
GET http://localhost:5000/api/v1/patients?status=Inactive&page=1&pageSize=20
Authorization: Bearer <access_token>
```

**Response `200 OK`**

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20,
  "totalPages": 0
}
```

---

### ❌ Unauthorized — missing or expired token (401)

```http
GET http://localhost:5000/api/v1/patients
```

**Response `401 Unauthorized`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

