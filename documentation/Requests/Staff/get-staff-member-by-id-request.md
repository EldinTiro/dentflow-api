# Staff API — Get Staff Member by ID

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`, `Receptionist`, `Dentist`, `Hygienist`

---

## GET `/staff/{id}`

Returns a single staff member record by its UUID.

---

### ✅ Happy path — staff member found (200 OK)

```http
GET http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <access_token>
```

**Response `200 OK`**

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
  "biography": "Board-certified orthodontist with 10 years experience.",
  "licenseNumber": "DDS-123456",
  "licenseExpiry": "2027-12-31",
  "npiNumber": "1234567890",
  "isActive": true,
  "hireDate": "2024-03-01",
  "terminationDate": null,
  "createdAt": "2026-03-15T10:00:00Z",
  "updatedAt": null
}
```

---

### ❌ Not found (404)

```http
GET http://localhost:5000/api/v1/staff/00000000-0000-0000-0000-000000000000
Authorization: Bearer <access_token>
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
GET http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response `401 Unauthorized`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

