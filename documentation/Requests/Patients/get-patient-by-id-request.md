# Patients API — Get Patient by ID

> Base URL (local dev): `http://localhost:5000/api/v1`  
> 🔐 Requires Bearer token — roles: `SuperAdmin`, `ClinicOwner`, `Receptionist`, `Dentist`, `Hygienist`

---

## GET `/patients/{id}`

Returns a single patient record by its UUID.

---

### ✅ Happy path — patient found (200 OK)

```http
GET http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <access_token>
```

**Response `200 OK`**

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
  "firstVisitDate": "2025-01-10",
  "lastVisitDate": "2026-02-20",
  "recallDueDate": "2026-08-20",
  "preferredProviderId": "a1b2c3d4-0000-0000-0000-000000000010",
  "smsOptIn": true,
  "emailOptIn": true,
  "notes": "Patient prefers morning appointments.",
  "createdAt": "2026-03-15T10:00:00Z",
  "updatedAt": "2026-03-15T11:30:00Z"
}
```

---

### ❌ Not found — patient does not exist or belongs to another tenant (404)

```http
GET http://localhost:5000/api/v1/patients/00000000-0000-0000-0000-000000000000
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
GET http://localhost:5000/api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response `401 Unauthorized`**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

