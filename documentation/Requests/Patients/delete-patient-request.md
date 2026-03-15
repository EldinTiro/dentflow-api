# Patients API — Delete Patient

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`  
> ⚠️ This is a **soft delete** — the record is flagged as deleted and hidden from all queries, but never permanently removed from the database.

---

## DELETE `/patients/{id}`

Soft-deletes a patient record. The patient will no longer appear in list or get-by-id responses but the data is retained for audit and compliance purposes.

---

### ✅ Happy path — patient soft-deleted (204 No Content)

```http
DELETE http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <access_token>
```

**Response `204 No Content`**

_(empty body)_

---

### ❌ Not found — patient does not exist (404)

```http
DELETE http://localhost:5000/api/v1/patients/00000000-0000-0000-0000-000000000000
Authorization: Bearer <access_token>
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
DELETE http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
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
DELETE http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
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

