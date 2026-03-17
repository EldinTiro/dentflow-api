# DentFlow — Feature Progress Tracker

> Living document. Update as features ship.  
> Last updated: 2026-03-18

**Legend:** ✅ Done · 🔄 Partial · 🔲 Not started · 🚫 Blocked

---

## Table of Contents

1. [Infrastructure & Foundation](#1-infrastructure--foundation)
2. [Authentication & Identity](#2-authentication--identity)
3. [Tenants (SuperAdmin)](#3-tenants-superadmin)
4. [Staff](#4-staff)
5. [Patients](#5-patients)
6. [Appointments](#6-appointments)
7. [Treatments (Phase 3)](#7-treatments-phase-3)
8. [Billing (Phase 4)](#8-billing-phase-4)
9. [Notifications (Phase 2/4)](#9-notifications-phase-24)
10. [Documents (Phase 3)](#10-documents-phase-3)
11. [Reporting (Phase 4)](#11-reporting-phase-4)
12. [Frontend Shell & UX](#12-frontend-shell--ux)
13. [Testing](#13-testing)
14. [DevOps & Production](#14-devops--production)
15. [AI Assistant — DentAI](#15-ai-assistant--dentai-backlog)
16. [Agent Tooling & Copilot Instructions](#16-agent-tooling--copilot-instructions)
17. [Localization (i18n)](#17-localization-i18n)
18. [User Preferences & Settings](#18-user-preferences--settings)
19. [Feature Flags / Capability Flags](#19-feature-flags--capability-flags)
10. [Documents (Phase 3)](#10-documents-phase-3)
11. [Reporting (Phase 4)](#11-reporting-phase-4)
12. [Frontend Shell & UX](#12-frontend-shell--ux)
13. [Testing](#13-testing)
14. [DevOps & Production](#14-devops--production)
15. [AI Assistant — DentAI (Backlog)](#15-ai-assistant--peariai-backlog)

---

## 1. Infrastructure & Foundation

| # | Item | Status | Notes |
|---|---|---|---|
| 1.1 | .NET 9 solution structure (API / Application / Domain / Infrastructure / Modules) | ✅ | |
| 1.2 | EF Core 9 + Npgsql + PostgreSQL 17 | ✅ | |
| 1.3 | Finbuckle multi-tenancy (subdomain strategy) | ✅ | Config store — dev tenant `localhost` |
| 1.4 | Global query filter — tenant + soft delete isolation | ✅ | `TenantAuditableEntity` base class |
| 1.5 | MediatR pipeline (Logging / Validation / Performance behaviors) | ✅ | |
| 1.6 | FluentValidation auto-registration | ✅ | |
| 1.7 | ErrorOr result pattern throughout | ✅ | |
| 1.8 | FastEndpoints + JWT Bearer + HMAC-SHA256 dev key | ✅ | Swap to RS256 for production |
| 1.9 | Docker Compose local dev (API + PostgreSQL + Redis) | ✅ | |
| 1.10 | Serilog structured logging | ✅ | |
| 1.11 | `System.Text.Json` global enum string serialization | ✅ | `JsonStringEnumConverter` in `Program.cs` |
| 1.12 | Redis caching layer | 🔲 | Configured, not actively used yet |
| 1.13 | Hangfire background jobs (PostgreSQL storage) | 🔲 | Planned for Notifications |
| 1.14 | GitHub Actions CI/CD pipeline | 🔲 | |
| 1.15 | Rate limiting (300 req/min per tenant) | 🔲 | Middleware registered, not tuned |

---

## 2. Authentication & Identity

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 2.1 | Register user (tenant-scoped) | ✅ | |
| 2.2 | Login — JWT access token + refresh token | ✅ | |
| 2.3 | Refresh token rotation | ✅ | |
| 2.4 | Logout / token revocation | ✅ | |
| 2.5 | Role seeding (`SuperAdmin`, `ClinicOwner`, `ClinicAdmin`, `Dentist`, `Hygienist`, `Receptionist`, `BillingStaff`, `ReadOnly`) | ✅ | |
| 2.6 | SuperAdmin auto-seed on startup | ✅ | |
| 2.7 | Password reset (email link) | 🔲 | Requires Notifications module |
| 2.8 | Two-factor authentication (TOTP) | 🔲 | |
| 2.9 | Account lockout after failed attempts | 🔲 | |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 2.10 | Login page with logo | ✅ | Logo above card, `h-40` |
| 2.11 | JWT token storage + auto-refresh | ✅ | |
| 2.12 | Role-based route guards | ✅ | |
| 2.13 | Auth store (Zustand) | ✅ | |
| 2.14 | Logout | ✅ | |
| 2.15 | Forgot password page | 🔲 | |
| 2.16 | Change password (in profile) | 🔲 | |

---

## 3. Tenants (SuperAdmin)

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 3.1 | Create tenant | ✅ | |
| 3.2 | List / get tenant | ✅ | |
| 3.3 | Update tenant (name, plan) | ✅ | |
| 3.4 | Activate / deactivate tenant | ✅ | |
| 3.5 | Per-tenant user management (list, invite, remove) | ✅ | |
| 3.6 | Tenant-scoped settings (timezone, working hours, logo) | 🔲 | |
| 3.7 | Stripe subscription management | 🔲 | Phase 4 |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 3.8 | SuperAdmin panel — tenant list & CRUD | ✅ | |
| 3.9 | SuperAdmin panel — user management per tenant | ✅ | |
| 3.10 | Tenant settings page (clinic profile) | 🔲 | |
| 3.11 | Onboarding wizard for new tenants | 🔲 | |

---

## 4. Staff

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 4.1 | Create / update / soft-delete staff member | ✅ | |
| 4.2 | Get by ID / list with search & filter | ✅ | |
| 4.3 | Staff availability (weekly schedule) | ✅ | |
| 4.4 | Staff blocked time (leave / holidays) | ✅ | |
| 4.5 | Provider availability engine (query free slots) | 🔲 | Needed for booking + online portal |
| 4.6 | Leave management workflow | 🔲 | |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 4.7 | Staff list page with search & filter | ✅ | |
| 4.8 | Create staff drawer | ✅ | |
| 4.9 | Edit staff drawer | ✅ | |
| 4.10 | Staff detail / profile page | ✅ | `StaffDetailPage` with tabs |
| 4.11 | Availability weekly schedule UI | ✅ | `StaffAvailabilityTab` — per-day open/close times, day toggle |
| 4.12 | Blocked time UI (leave / vacation) | ✅ | `StaffBlockedTimesTab` — create, delete, date-range picker |

---

## 5. Patients

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 5.1 | Create / update / soft-delete patient | ✅ | |
| 5.2 | Get by ID / list with search, filter, pagination | ✅ | |
| 5.3 | Auto-generated patient number | ✅ | |
| 5.4 | Medical history | ✅ | |
| 5.5 | Allergies | ✅ | |
| 5.6 | Emergency contacts | ✅ | |
| 5.7 | Dental insurance details | 🔲 | |
| 5.8 | Patient merge (duplicates) | 🔲 | |
| 5.9 | Patient portal access (read-only) | 🔲 | Requires `Patient` role |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 5.10 | Patients list page with search & filter | ✅ | |
| 5.11 | Create patient drawer | ✅ | |
| 5.12 | Edit patient drawer | ✅ | |
| 5.13 | Patient detail page (demographics + history) | ✅ | |
| 5.14 | Appointment history tab on patient detail | ✅ | Clickable rows open appointment detail panel |
| 5.15 | Documents tab on patient detail | 🔲 | Phase 3 |
| 5.16 | Treatment history tab on patient detail | ✅ | `TreatmentPlanTab` — plan list, line items, CDT codes |
| 5.17 | Insurance details form | 🔲 | |

---

## 6. Appointments

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 6.1 | Book appointment | ✅ | |
| 6.2 | List appointments (with date/status/provider/patient filters) | ✅ | |
| 6.3 | Get appointment by ID | ✅ | |
| 6.4 | Reschedule appointment | ✅ | |
| 6.5 | Cancel appointment | ✅ | |
| 6.6 | Status lifecycle (Scheduled → Confirmed → CheckedIn → InChair → Completed / Cancelled / NoShow) | ✅ | |
| 6.7 | Appointment types CRUD | ✅ | |
| 6.8 | Default appointment type seeding per tenant | ✅ | 10 dental types seeded via `AppointmentTypeSeeder` |
| 6.9 | GET `/appointment-types` endpoint | ✅ | |
| 6.10 | Conflict detection (overlapping provider bookings) | ✅ | `HasProviderConflictAsync` in `BookAppointmentCommandHandler` + `RescheduleAppointmentCommandHandler` |
| 6.11 | Blocked-time enforcement (leave / vacation) before booking | ✅ | `IProviderBlockedTimeChecker` cross-module service; checked in Book + Reschedule handlers |
| 6.12 | Buffer time between appointments | 🔲 | |
| 6.13 | Recurring appointments | 🔲 | |
| 6.14 | Waiting list | 🔲 | |
| 6.15 | Cancellation policy enforcement | 🔲 | |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 6.16 | Appointments list page (with date/status filters) | ✅ | |
| 6.17 | Week calendar view | ✅ | Timezone-correct local date grouping |
| 6.18 | Book appointment drawer | ✅ | Defaults to next 15-min boundary |
| 6.19 | Appointment type dropdown (seeded) | ✅ | |
| 6.20 | Reschedule modal | ✅ | |
| 6.21 | Cancel appointment action | ✅ | |
| 6.22 | Status actions panel | ✅ | Quick-advance button inline in list rows (one click per lifecycle step) |
| 6.23 | Day view in calendar | 🔲 | |
| 6.24 | Month view in calendar | 🔲 | |
| 6.25 | Drag-to-reschedule on calendar | 🔲 | |
| 6.26 | Appointment detail side panel | ✅ | Patient, provider, type, duration, status + reschedule/cancel actions |
| 6.27 | Manage appointment types (admin UI) | 🔲 | CRUD for appointment types per tenant |

---

## 7. Treatments (Phase 3)

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 7.1 | Treatment plan CRUD | ✅ | Create, update, soft-delete, list per patient |
| 7.2 | CDT procedure code library | ✅ | Seeded with common dental procedure codes |
| 7.3 | Per-tooth condition tracking | 🔲 | Deferred — visual dental chart (F2 backlog) |
| 7.4 | SOAP clinical notes | 🔲 | Deferred — F3 backlog |
| 7.5 | Treatment status workflow (Draft → Accepted → Completed) | ✅ | |
| 7.6 | Link treatments to appointments | 🔄 | Plans linked to patient; per-appointment link deferred |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 7.7 | Visual dental chart (clickable, FDI numbering) | 🔲 | Deferred — F2 backlog |
| 7.8 | Treatment plan builder UI | ✅ | Plan list, add/edit plans, line items with CDT codes and costs |
| 7.9 | SOAP notes editor | 🔲 | Deferred — F3 backlog |
| 7.10 | Treatment history timeline | 🔲 | |
| 7.11 | PDF export of treatment plan | 🔲 | Deferred — F6 backlog |

---

## 8. Billing (Phase 4)

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 8.1 | Invoice generation from completed treatments | 🔲 | |
| 8.2 | Itemised line items + tax | 🔲 | |
| 8.3 | Payment recording (cash, card, insurance) | 🔲 | |
| 8.4 | Stripe integration (card payments) | 🔲 | |
| 8.5 | Insurance claim tracking | 🔲 | |
| 8.6 | Invoice PDF generation | 🔲 | |
| 8.7 | Stripe webhook handling | 🔲 | |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 8.8 | Invoices list page | 🔲 | |
| 8.9 | Invoice detail / print view | 🔲 | |
| 8.10 | Payment modal | 🔲 | |
| 8.11 | Stripe checkout flow | 🔲 | |

---

## 9. Notifications (Phase 2/4)

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 9.1 | Email provider adapter (AWS SES / SendGrid) | 🔲 | |
| 9.2 | SMS provider adapter (Twilio) | 🔲 | |
| 9.3 | Appointment confirmation email | 🔲 | |
| 9.4 | Appointment reminder (24h + 2h) via Hangfire | 🔲 | |
| 9.5 | Cancellation / reschedule notification | 🔲 | |
| 9.6 | Notification template engine (per-tenant customisation) | 🔲 | |
| 9.7 | Notification delivery log | 🔲 | |
| 9.8 | Patient opt-out support | 🔲 | |
| 9.9 | Password reset email | 🔲 | Blocks 2.7 |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 9.10 | Notification preferences page (per patient) | 🔲 | |
| 9.11 | Notification log view (admin) | 🔲 | |

---

## 10. Documents (Phase 3)

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 10.1 | File upload to S3 | 🔲 | |
| 10.2 | Signed download URL generation | 🔲 | |
| 10.3 | Per-patient document list | 🔲 | |
| 10.4 | Document soft delete | 🔲 | |
| 10.5 | File type / size validation | 🔲 | |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 10.6 | Document upload UI (drag & drop) | 🔲 | |
| 10.7 | Document list + download on patient detail | 🔲 | |

---

## 11. Reporting (Phase 4)

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 11.1 | Revenue by period query | 🔲 | |
| 11.2 | Appointment volume & cancellation rate | 🔲 | |
| 11.3 | Patient demographics breakdown | 🔲 | |
| 11.4 | Staff utilisation report | 🔲 | |
| 11.5 | PDF export | 🔲 | |
| 11.6 | Excel export | 🔲 | |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 11.7 | Reporting dashboard with charts (Recharts) | 🔲 | |
| 11.8 | Date range + filter controls | 🔲 | |
| 11.9 | Export buttons | 🔲 | |

---

## 12. Frontend Shell & UX

| # | Item | Status | Notes |
|---|---|---|---|
| 12.1 | App shell — sidebar navigation + header | ✅ | Collapsible sidebar |
| 12.2 | Role-based nav items | ✅ | |
| 12.3 | React Router v7 lazy-loaded routes | ✅ | |
| 12.4 | TanStack Query + Axios API client | ✅ | |
| 12.5 | Login page with brand logo | ✅ | Logo `h-40` above card |
| 12.6 | Toast / notification feedback (success, error) | ✅ | `sonner` installed; `<Toaster>` in `App.tsx`; all mutations use `toast.error(getApiErrorMessage(...))` |
| 12.7 | Confirmation dialogs (delete actions) | 🔲 | Patient delete has modal; staff/appointments don't |
| 12.8 | Empty states (no data illustrations) | 🔲 | Showing plain text fallbacks |
| 12.9 | Responsive / mobile layout | 🔲 | Desktop-only currently |
| 12.10 | Dark mode | ✅ | Full coverage — all pages/components, class-based Tailwind v4 |
| 12.11 | Global error boundary | 🔲 | |
| 12.12 | Loading skeletons (instead of spinners) | 🔲 | |
| 12.13 | Clinic logo in sidebar header | 🔲 | |
| 12.14 | User profile dropdown (avatar, name, logout) | 🔲 | Logout is buried in sidebar footer |
| 12.15 | Keyboard shortcuts (e.g. N for new, / for search) | 🔲 | |
| 12.16 | Auto dark mode (time-based: 20:00–06:00, localStorage override) | ✅ | Anti-FOUC script in `index.html`; 60 s live interval in `ThemeContext` |
| 12.17 | Dashboard — Quick Actions bar (Book Appointment + Register Patient) | ✅ | |
| 12.18 | Dashboard — Tomorrow stat card + Today by Provider card | ✅ | Provider bars always render all Dentist/Hygienist staff (0-count default) |
| 12.19 | Dashboard — appointment completion progress bar | ✅ | "X of Y completed" counter in Today's Schedule header |
| 12.20 | Country code phone picker (`DialPicker`) on Create Patient form | ✅ | Custom dropdown, Unicode flag emoji, 37 countries, Bosnia (+387) default |

---

## 13. Testing

| # | Item | Status | Notes |
|---|---|---|---|
| 13.1 | Patient command/query handler unit tests | 🔄 | File exists, coverage incomplete |
| 13.2 | Appointment handler unit tests | 🔄 | Book + Cancel + Reschedule + ProviderBlocked covered; UpdateStatus missing |
| 13.3 | Staff handler unit tests | 🔄 | File exists |
| 13.4 | Identity handler unit tests | 🔲 | |
| 13.5 | Tenant handler unit tests | 🔲 | |
| 13.6 | Validator unit tests | 🔲 | |
| 13.7 | Repository integration tests (real DB) | 🔲 | |
| 13.8 | API integration tests (WebApplicationFactory) | 🔲 | |
| 13.9 | Playwright end-to-end tests | 🔲 | Phase 5 |

---

## 14. DevOps & Production

| # | Item | Status | Notes |
|---|---|---|---|
| 14.1 | Dockerfile (API) | ✅ | |
| 14.2 | Dockerfile (frontend nginx) | ✅ | |
| 14.3 | Docker Compose (local dev) | ✅ | API + PostgreSQL + Redis |
| 14.4 | GitHub Actions — build + test on PR | 🔲 | |
| 14.5 | GitHub Actions — push Docker image to ECR on merge | 🔲 | |
| 14.6 | Terraform — ECS Fargate cluster | 🔲 | |
| 14.7 | Terraform — RDS PostgreSQL | 🔲 | |
| 14.8 | Terraform — ElastiCache Redis | 🔲 | |
| 14.9 | Terraform — S3 + CloudFront (frontend) | 🔲 | |
| 14.10 | Terraform — Route 53 wildcard subdomain | 🔲 | |
| 14.11 | RS256 JWT key pair (production) | 🔲 | Dev uses HMAC-SHA256 |
| 14.12 | Secrets management (AWS Secrets Manager) | 🔲 | |
| 14.13 | Health check endpoint (`GET /health`) | ✅ | |
| 14.14 | Structured log shipping to CloudWatch | 🔲 | |
| 14.15 | OWASP security audit | 🔲 | Phase 5 |
| 14.16 | Performance testing + query optimisation | 🔲 | Phase 5 |

---

## Next Up — Recommended Priority Order

1. **Confirmation dialogs** (12.7) — staff/appointment deletes fire immediately with no confirmation
2. **Manage appointment types admin UI** (6.27) — clinic can't customise types without hitting the DB
3. **Feature flags** (19.1–19.5) — prerequisite for safely enabling/disabling SMS, AI, online booking per tenant
4. **User preferences & settings** (18.1–18.8) — language, theme, time format stored per user
5. **Localization / i18n** (17.1–17.5) — English + Bosnian/Croatian/Serbian
6. **Provider availability engine** (4.5) — query free slots; prerequisite for online booking portal
7. **Notifications module** (9.1–9.9) — appointment reminders are a core clinic need
8. **AI chatbot — DentAI** (15.1–15.13) — patient context Q&A
9. **Billing module** (Phase 4) — invoices from completed treatment items, Stripe integration
10. **Production infrastructure** (Phase 5) — Terraform, GitHub Actions CI/CD, security audit

---

## 16. Agent Tooling & Copilot Instructions

| # | Item | Status | Notes |
|---|---|---|---|
| 16.1 | Backend Copilot instructions (`dentflow-api/.github/copilot-instructions.md`) | ✅ | Architecture, CQRS, FastEndpoints error pattern, error handling, testing, DateTime, FluentValidation, migrations |
| 16.2 | Module-scoped instructions (`src/Modules/.instructions.md`) | ✅ | Applies to all files under `src/Modules/**`; documents mandatory endpoint error propagation pattern |
| 16.3 | Frontend Copilot instructions (`dentflow-web/.github/copilot-instructions.md`) | ✅ | Tech stack, error handling with `getApiErrorMessage` + sonner, Forms/Zod, query keys, naming, routing, TypeScript conventions |

---

## 17. Localization (i18n)

> Support English (default) + Bosnian / Croatian / Serbian (Latin script, mutually intelligible — one translation file covers all three).

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 17.1 | Install and configure `react-i18next` | 🔲 | |
| 17.2 | Extract all UI strings into translation files (`en.json`, `bs.json`) | 🔲 | Bosnian file doubles as Croatian/Serbian Latin |
| 17.3 | Language switcher in header / user settings | 🔲 | Persists to localStorage until user prefs are stored in DB |
| 17.4 | Date/time and number formatting via `Intl` locale | 🔲 | |
| 17.5 | RTL support scaffolding | 🔲 | Not needed now but architect for it |

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 17.6 | Error description localization | 🔲 | Low priority — messages are readable English for now; revisit if required by tenants |

---

## 18. User Preferences & Settings

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 18.1 | `UserPreferences` entity (per-user, per-tenant) | 🔲 | Stores language, theme, time format, default calendar view |
| 18.2 | GET / PUT `/users/me/preferences` endpoint | 🔲 | |
| 18.3 | Change password endpoint | 🔲 | Authenticated user changes own password |
| 18.4 | Avatar upload (S3) | 🔲 | Blocked by Documents module |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 18.5 | User settings page (`/settings/profile`) | 🔲 | Name, avatar, change password |
| 18.6 | Preferences panel — language, theme, time format (12h/24h), default calendar view | 🔲 | |
| 18.7 | User profile dropdown in sidebar — avatar, name, role badge, settings link, logout | 🔲 | Currently logout is buried in sidebar footer |
| 18.8 | Preferences synced to DB (not just localStorage) once backend is ready | 🔲 | |

---

## 19. Feature Flags / Capability Flags

> Per-tenant toggles that let a SuperAdmin or ClinicAdmin enable/disable product features without a code deploy.

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 19.1 | `TenantFeatureFlag` entity (flagKey, isEnabled, tenantId) | 🔲 | `null` tenantId = platform-wide default |
| 19.2 | `IFeatureFlagService` + `IsEnabled(flagKey)` implementation | 🔲 | Checks tenant override, falls back to platform default |
| 19.3 | Seed default flags: `sms-reminders`, `ai-chatbot`, `online-booking`, `billing`, `treatment-plans` | 🔲 | |
| 19.4 | GET / PUT `/admin/feature-flags` (SuperAdmin) and `/settings/feature-flags` (ClinicAdmin) endpoints | 🔲 | |
| 19.5 | Guard handlers / endpoints with flag checks where relevant | 🔲 | E.g. SMS send blocked if `sms-reminders` off |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 19.6 | `useFeatureFlag(key)` hook — fetches tenant flags, returns boolean | 🔲 | Cache with TanStack Query |
| 19.7 | Feature flag admin page (SuperAdmin — platform defaults; ClinicAdmin — tenant overrides) | 🔲 | Toggle switches per flag |
| 19.8 | Conditionally hide nav items and UI sections based on flags | 🔲 | E.g. hide AI chat button if `ai-chatbot` off |

---

## 15. AI Assistant — DentAI (Backlog)

> Planned feature F8 from `implementation-plan.md`. Not started. Revisit after Phase 5 is complete.

### Backend
| # | Item | Status | Notes |
|---|---|---|---|
| 15.1 | `DentFlow.AI` module scaffold | 🔲 | New module alongside existing ones |
| 15.2 | Patient context builder service | 🔲 | Aggregates demographics, treatment plans, SOAP notes, appt history, allergies into system prompt |
| 15.3 | `POST /ai/chat` FastEndpoints endpoint (SSE streaming) | 🔲 | Server-Sent Events for real-time response streaming |
| 15.4 | AI provider abstraction (OpenAI / Azure OpenAI / Ollama) | 🔲 | Configurable per tenant |
| 15.5 | Per-tenant AI opt-in compliance gate | 🔲 | No PII sent to third parties without explicit tenant consent |
| 15.6 | Conversation history in Redis (session TTL, not persisted to DB) | 🔲 | |
| 15.7 | Rate limiting per tenant + per user (token cost control) | 🔲 | |
| 15.8 | AI audit log (prompt hash + response hash, no PII stored) | 🔲 | |

### Frontend
| # | Item | Status | Notes |
|---|---|---|---|
| 15.9 | Floating chat button on patient detail page | 🔲 | |
| 15.10 | Expandable AI chat side panel with message history | 🔲 | |
| 15.11 | Streaming text rendering (SSE → real-time typing effect) | 🔲 | |
| 15.12 | Quick-action prompt chips ("Summarise history", "Draft SOAP note", "Check contraindications") | 🔲 | |
| 15.13 | DentAI settings page — enable/disable, provider selection (admin only) | 🔲 | |
