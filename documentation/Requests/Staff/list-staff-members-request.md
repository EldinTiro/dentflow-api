# Staff API — List Staff Members

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`, `Receptionist`, `Dentist`, `Hygienist`

---

## GET `/staff`

Returns a paginated list of staff members for the current tenant.

### Query Parameters

| Parameter   | Type      | Required | Default | Description |
|-------------|-----------|----------|---------|---|
| `staffType` | `string`  | No       | —       | Filter by staff type enum (see below) |
| `isActive`  | `boolean` | No       | —       | Filter by active status: `true` or `false` |
| `page`      | `int`     | No       | `1`     | Page number (1-based) |
| `pageSize`  | `int`     | No       | `20`    | Results per page (max `100`) |

### `staffType` enum values

`Dentist`, `DentalAssistant`, `Hygienist`, `Receptionist`, `ClinicAdmin`, `OfficeManager`

---

### ✅ Happy path — default list (200 OK)

```http
GET http://localhost:5000/api/v1/staff
Authorization: Bearer <access_token>
```

**Response `200 OK`**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "staffType": "Dentist",
      "firstName": "Sarah",
      "lastName": "Nguyen",
      "fullName": "Sarah Nguyen",
      "email": "sarah.nguyen@demo-clinic.local",
      "isActive": true,
      "hireDate": "2024-03-01",
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

### ✅ Happy path — filter by staffType and isActive (200 OK)

```http
GET http://localhost:5000/api/v1/staff?staffType=Dentist&isActive=true&page=1&pageSize=10
Authorization: Bearer <access_token>
```

**Response `200 OK`**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "staffType": "Dentist",
      "firstName": "Sarah",
      "lastName": "Nguyen",
      "fullName": "Sarah Nguyen",
      "isActive": true,
      "createdAt": "2026-03-15T10:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

### ❌ Unauthorized — missing or expired token (401)

```http
GET http://localhost:5000/api/v1/staff
```

**Response `401 Unauthorized`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

