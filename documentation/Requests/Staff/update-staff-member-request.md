# Staff API — Update Staff Member

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`

---

## PUT `/staff/{id}`

Updates an existing staff member's details. `staffType` cannot be changed via this endpoint.

---

### ✅ Happy path — full update (200 OK)

```http
PUT http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "firstName": "Sarah",
  "lastName": "Nguyen-Chen",
  "email": "sarah.chen@demo-clinic.local",
  "phone": "+1-555-0299",
  "specialty": "Orthodontics & Pediatric",
  "colorHex": "#10B981",
  "biography": "Board-certified orthodontist with 11 years experience.",
  "licenseNumber": "DDS-123456",
  "licenseExpiry": "2028-12-31",
  "npiNumber": "1234567890"
}
```

**Response `200 OK`**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "staffType": "Dentist",
  "firstName": "Sarah",
  "lastName": "Nguyen-Chen",
  "fullName": "Sarah Nguyen-Chen",
  "email": "sarah.chen@demo-clinic.local",
  "phone": "+1-555-0299",
  "specialty": "Orthodontics & Pediatric",
  "colorHex": "#10B981",
  "biography": "Board-certified orthodontist with 11 years experience.",
  "licenseNumber": "DDS-123456",
  "licenseExpiry": "2028-12-31",
  "npiNumber": "1234567890",
  "isActive": true,
  "hireDate": "2024-03-01",
  "terminationDate": null,
  "createdAt": "2026-03-15T10:00:00Z",
  "updatedAt": "2026-03-15T14:30:00Z"
}
```

---

### ❌ Validation error — missing required fields (400 Bad Request)

```http
PUT http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
Authorization: Bearer <access_token>

{
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

### ❌ Not found (404)

```http
PUT http://localhost:5000/api/v1/staff/00000000-0000-0000-0000-000000000000
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "firstName": "Ghost",
  "lastName": "Staff"
}
```

**Response `404 Not Found`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Staff member not found.",
  "status": 404
}
```

---

### ❌ Unauthorized — missing or expired token (401)

```http
PUT http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "firstName": "Sarah",
  "lastName": "Nguyen"
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

