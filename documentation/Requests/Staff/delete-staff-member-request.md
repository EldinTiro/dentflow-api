# Staff API — Delete Staff Member

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`  
> ⚠️ This is a **soft delete** — the record is flagged as deleted and hidden from all queries, but never permanently removed.

---

## DELETE `/staff/{id}`

Soft-deletes a staff member record.

---

### ✅ Happy path — staff member soft-deleted (204 No Content)

```http
DELETE http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <access_token>
```

**Response `204 No Content`**

_(empty body)_

---

### ❌ Not found (404)

```http
DELETE http://localhost:5000/api/v1/staff/00000000-0000-0000-0000-000000000000
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
DELETE http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
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
DELETE http://localhost:5000/api/v1/staff/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <receptionist_access_token>
```

**Response `403 Forbidden`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```

