-- DT-16 — one-off cleanup of rows created before DT-01/02 were fixed.
--
-- Before the fix, child rows could point at a patient that doesn't exist or that belongs to
-- another clinic (stored under the caller's tenant), and appointments at a provider that
-- doesn't exist. They show as "(no patient)", endless "Loading…" and totals that disagree.
--
-- What it does: SOFT-deletes those rows (IsDeleted = true, DeletedAt = now) — nothing is
-- removed. Run the PREVIEW block first, check the counts, then run the CLEANUP block.
-- Payments of an orphan invoice are left as they are and listed in the preview: money
-- received needs a human decision, not a script.
--
-- Run with: psql "$CONNECTION_STRING" -f scripts/dt16-orphan-cleanup.sql   (preview only by default)

-- ── PREVIEW ─────────────────────────────────────────────────────────────────
-- A row is an orphan when no live patient with that id exists in the row's own tenant.

SELECT 'appointments (patient)' AS what, count(*) FROM appointments a
WHERE NOT a."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM patients p WHERE p."Id" = a."PatientId" AND p."TenantId" = a."TenantId" AND NOT p."IsDeleted")
UNION ALL
SELECT 'appointments (provider)', count(*) FROM appointments a
WHERE NOT a."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM staff_members s WHERE s."Id" = a."ProviderId" AND s."TenantId" = a."TenantId" AND NOT s."IsDeleted")
UNION ALL
SELECT 'invoices', count(*) FROM invoices i
WHERE NOT i."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM patients p WHERE p."Id" = i."PatientId" AND p."TenantId" = i."TenantId" AND NOT p."IsDeleted")
UNION ALL
SELECT 'treatment_plans', count(*) FROM treatment_plans t
WHERE NOT t."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM patients p WHERE p."Id" = t."PatientId" AND p."TenantId" = t."TenantId" AND NOT p."IsDeleted")
UNION ALL
SELECT 'allergies', count(*) FROM allergies x
WHERE NOT x."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM patients p WHERE p."Id" = x."PatientId" AND p."TenantId" = x."TenantId" AND NOT p."IsDeleted")
UNION ALL
SELECT 'patient_emergency_contacts', count(*) FROM patient_emergency_contacts x
WHERE NOT x."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM patients p WHERE p."Id" = x."PatientId" AND p."TenantId" = x."TenantId" AND NOT p."IsDeleted")
UNION ALL
SELECT 'medical_histories', count(*) FROM medical_histories x
WHERE NOT x."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM patients p WHERE p."Id" = x."PatientId" AND p."TenantId" = x."TenantId" AND NOT p."IsDeleted");

-- Payments recorded on orphan invoices: decide on these by hand.
SELECT i."InvoiceNumber", i."TenantId", pay."Amount", pay."PaidAt"
FROM invoice_payments pay
JOIN invoices i ON i."Id" = pay."InvoiceId"
WHERE NOT i."IsDeleted" AND NOT EXISTS (
    SELECT 1 FROM patients p WHERE p."Id" = i."PatientId" AND p."TenantId" = i."TenantId" AND NOT p."IsDeleted");

-- ── CLEANUP ─────────────────────────────────────────────────────────────────
-- Uncomment after checking the preview. One transaction: all or nothing.

-- BEGIN;
--
-- UPDATE appointments a SET "IsDeleted" = true, "DeletedAt" = now() AT TIME ZONE 'utc'
-- WHERE NOT a."IsDeleted" AND (
--     NOT EXISTS (SELECT 1 FROM patients p WHERE p."Id" = a."PatientId" AND p."TenantId" = a."TenantId" AND NOT p."IsDeleted")
--  OR NOT EXISTS (SELECT 1 FROM staff_members s WHERE s."Id" = a."ProviderId" AND s."TenantId" = a."TenantId" AND NOT s."IsDeleted"));
--
-- UPDATE invoices i SET "IsDeleted" = true, "DeletedAt" = now() AT TIME ZONE 'utc'
-- WHERE NOT i."IsDeleted"
--   AND NOT EXISTS (SELECT 1 FROM invoice_payments pay WHERE pay."InvoiceId" = i."Id")
--   AND NOT EXISTS (SELECT 1 FROM patients p WHERE p."Id" = i."PatientId" AND p."TenantId" = i."TenantId" AND NOT p."IsDeleted");
--
-- UPDATE treatment_plans t SET "IsDeleted" = true, "DeletedAt" = now() AT TIME ZONE 'utc'
-- WHERE NOT t."IsDeleted" AND NOT EXISTS (
--     SELECT 1 FROM patients p WHERE p."Id" = t."PatientId" AND p."TenantId" = t."TenantId" AND NOT p."IsDeleted");
--
-- UPDATE allergies x SET "IsDeleted" = true, "DeletedAt" = now() AT TIME ZONE 'utc'
-- WHERE NOT x."IsDeleted" AND NOT EXISTS (
--     SELECT 1 FROM patients p WHERE p."Id" = x."PatientId" AND p."TenantId" = x."TenantId" AND NOT p."IsDeleted");
--
-- UPDATE patient_emergency_contacts x SET "IsDeleted" = true, "DeletedAt" = now() AT TIME ZONE 'utc'
-- WHERE NOT x."IsDeleted" AND NOT EXISTS (
--     SELECT 1 FROM patients p WHERE p."Id" = x."PatientId" AND p."TenantId" = x."TenantId" AND NOT p."IsDeleted");
--
-- UPDATE medical_histories x SET "IsDeleted" = true, "DeletedAt" = now() AT TIME ZONE 'utc'
-- WHERE NOT x."IsDeleted" AND NOT EXISTS (
--     SELECT 1 FROM patients p WHERE p."Id" = x."PatientId" AND p."TenantId" = x."TenantId" AND NOT p."IsDeleted");
--
-- COMMIT;
