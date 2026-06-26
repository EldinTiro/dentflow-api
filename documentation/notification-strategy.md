# Notification Strategy — Appointment Reminders

## Overview

This document covers the channel comparison (SMS, WhatsApp, Viber), the two sender models (shared vs. per-tenant), integration architecture, compliance requirements, and a phased rollout plan for DentFlow's appointment reminder system.

---

## Channel Comparison

| Criteria | SMS | WhatsApp | Viber |
|---|---|---|---|
| Global reach | Everywhere | 2B+ users, strong globally | ~1B users, strong in Eastern Europe, SEA, Middle East |
| Delivery guarantee | High (carrier-level) | High (internet-dependent) | Medium (internet-dependent) |
| Rich content | No (plain text only) | Yes (images, buttons, templates) | Yes (images, buttons, stickers) |
| Pricing model | Per message | Per conversation (24h window) | Per message |
| Approx. cost (per unit) | $0.007–0.05 | $0.02–0.08/conv | $0.005–0.05 |
| Setup complexity | Low | Medium (Meta approval required) | Medium (Viber Business account) |
| .NET SDK quality | Excellent (Twilio) | Excellent (Twilio) | Community/REST only |
| Opt-out compliance | Built-in (STOP keyword) | Requires explicit opt-out flow | Requires explicit opt-out flow |
| Best for DentFlow | Default channel | Upgrade / richer experience | Opt-in if target market warrants |

### Recommendation

- **Phase 1:** SMS only — widest reach, least friction, same Twilio SDK used in Phase 2.
- **Phase 2:** WhatsApp via Twilio — richer messages, lower cost at scale, one SDK.
- **Phase 3:** Viber — only if tenant market analysis shows demand (Eastern Europe clinics).

---

## Sender Models

### Model A — Shared Sender (Platform-owned numbers)

DentFlow owns and operates a Twilio account. All tenants send through DentFlow's phone numbers (or WhatsApp sender).

```
Patient ← SMS ← DentFlow Twilio Account
                 └── Shared Number Pool
                     ├── Tenant A messages
                     ├── Tenant B messages
                     └── Tenant C messages
```

**Pros:**
- Zero setup for clinic owners — works out of the box
- Single billing account, easy cost tracking
- Centralized opt-out and compliance management
- Fastest to ship

**Cons:**
- Patient sees "DentFlow" or a generic number, not the clinic's name/number
- A spam complaint on the shared number affects all tenants
- You absorb all messaging costs (must be baked into subscription pricing)
- Twilio A2P 10DLC registration required in the US (one-time, shared)

**Cost model:** Include a message quota per subscription tier (e.g., 500 SMS/month on Standard, 2000 on Pro). Overage charged at cost + margin.

---

### Model B — Per-Tenant Sender (Clinic-owned credentials)

Each clinic brings their own Twilio (or provider) credentials. DentFlow stores them encrypted per-tenant and uses them at send time.

```
Patient ← SMS ← Tenant A's Twilio Account (Tenant A's number)
Patient ← SMS ← Tenant B's Twilio Account (Tenant B's number)
```

**Pros:**
- Patient sees the clinic's own number — higher trust, better open rates
- A complaint on one tenant does not affect others
- DentFlow has zero messaging cost — pass-through billing
- Clinics can port their existing number to Twilio

**Cons:**
- Clinic must create and manage a Twilio account (onboarding friction)
- DentFlow must securely store and rotate credentials per tenant
- Harder to support clinics who are not technical
- No revenue from messaging (unless you charge a connection fee)

**Cost model:** Clinics pay Twilio directly. DentFlow charges a flat feature fee for the notification module (part of the subscription tier).

---

### Model Comparison Summary

| Factor | Shared (A) | Per-Tenant (B) |
|---|---|---|
| Time to ship | Faster | Slower |
| Clinic onboarding effort | None | Medium |
| DentFlow revenue opportunity | Yes (markup) | No (feature fee only) |
| Isolation between tenants | No | Yes |
| Patient trust (recognizable number) | Lower | Higher |
| Compliance burden | DentFlow | Split (DentFlow + Clinic) |
| Recommended phase | Phase 1 | Phase 2 (optional upgrade) |

### Hybrid Approach (Recommended Long-Term)

Default all tenants to shared sender. Allow clinics on Pro/Enterprise tiers to connect their own Twilio credentials. The abstraction layer in code is the same — only the credentials source differs.

---

## Architecture

### Abstraction Layer

```csharp
// DentFlow.Notifications / Application
public interface INotificationChannel
{
    NotificationChannelType ChannelType { get; }
    Task<ErrorOr<NotificationResult>> SendAsync(NotificationMessage message, CancellationToken ct);
}

// Implementations registered per channel
public class SmsChannel : INotificationChannel { }       // Twilio SMS
public class WhatsAppChannel : INotificationChannel { }  // Twilio WhatsApp
public class ViberChannel : INotificationChannel { }     // Viber REST API
```

### Tenant Notification Config Entity

```csharp
public class TenantNotificationConfig : TenantAuditableEntity
{
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public bool ViberEnabled { get; set; }

    // Null = use shared sender; populated = per-tenant credentials
    public string? TwilioAccountSid { get; set; }    // Encrypted at rest
    public string? TwilioAuthToken { get; set; }     // Encrypted at rest
    public string? TwilioFromNumber { get; set; }

    public int ReminderHoursBefore { get; set; } = 24;  // Configurable per tenant
}
```

### Notification Log Entity

```csharp
public class NotificationLog : TenantAuditableEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public NotificationChannelType Channel { get; set; }
    public string ToNumber { get; set; } = default!;
    public string MessageBody { get; set; } = default!;
    public NotificationStatus Status { get; set; }  // Queued, Sent, Delivered, Failed
    public string? ProviderMessageId { get; set; }  // Twilio SID for status callbacks
    public string? FailureReason { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
```

### Hangfire Job Flow

```
Appointment Confirmed
        │
        ▼
EnqueueReminderJob (fires at: AppointmentTime - ReminderHoursBefore)
        │
        ▼
AppointmentReminderJob.SendAsync(appointmentId)
        │
        ├── Resolve tenant notification config
        ├── Check patient opt-out flag
        ├── Select channel (SMS → WhatsApp → Viber, per tenant config)
        ├── Resolve credentials (shared or per-tenant)
        ├── Send via INotificationChannel
        └── Write NotificationLog entry
                │
                ▼
        Twilio Status Callback Webhook
                │
                └── Update NotificationLog.Status
```

### Credentials Resolution

```csharp
public class NotificationCredentialsResolver
{
    public TwilioCredentials Resolve(TenantNotificationConfig config)
    {
        // Per-tenant credentials take priority
        if (config.TwilioAccountSid is not null)
            return new TwilioCredentials(
                Decrypt(config.TwilioAccountSid),
                Decrypt(config.TwilioAuthToken!),
                config.TwilioFromNumber!);

        // Fall back to shared platform credentials from IOptions<TwilioSettings>
        return _sharedCredentials;
    }
}
```

---

## Patient Opt-Out

Legally required in all target markets (GDPR, TCPA, local equivalents).

```csharp
// On Patient entity
public bool SmsOptIn { get; set; } = false;      // Default OFF — explicit opt-in required
public bool WhatsAppOptIn { get; set; } = false;
public bool ViberOptIn { get; set; } = false;
public DateTime? OptedOutAt { get; set; }
```

- Opt-in captured during patient registration or via a separate consent flow in the frontend
- Replying STOP to SMS automatically triggers Twilio's opt-out — webhook updates the flag
- WhatsApp opt-out handled via button in message template or STOP keyword
- Never send to a patient without explicit opt-in recorded

---

## Subscription Tier Gating

Integrate with the existing feature flag system:

| Feature | Free | Standard | Pro | Enterprise |
|---|---|---|---|---|
| SMS reminders (shared sender) | No | Yes (500/mo) | Yes (2000/mo) | Yes (unlimited) |
| WhatsApp reminders | No | No | Yes | Yes |
| Viber reminders | No | No | No | Yes |
| Per-tenant sender (BYO Twilio) | No | No | No | Yes |
| Custom reminder timing | No | No | Yes | Yes |
| Delivery status tracking | No | Yes | Yes | Yes |

---

## Compliance Checklist

- [ ] Explicit opt-in stored per patient per channel
- [ ] STOP / opt-out keyword handling via Twilio webhook
- [ ] No PHI in message body beyond appointment date/time (avoid diagnosis references)
- [ ] Twilio A2P 10DLC registration (US SMS — required by carriers)
- [ ] WhatsApp template approval via Meta before going live
- [ ] Credentials encrypted at rest (use ASP.NET Core Data Protection or equivalent)
- [ ] NotificationLog retained for audit, never exposes patient data in API responses
- [ ] GDPR: patient can request deletion of notification history

---

## Phased Rollout

### Phase 1 — SMS, Shared Sender (4–6 weeks)
- Implement `INotificationChannel` + `SmsChannel` (Twilio)
- `TenantNotificationConfig` entity + migration
- `NotificationLog` entity + migration
- `AppointmentReminderJob` via Hangfire (triggered on appointment confirmation)
- Patient opt-in fields + consent UI in frontend
- Twilio status callback webhook endpoint
- Gate behind Standard tier feature flag

### Phase 2 — WhatsApp + Per-Tenant Sender (3–4 weeks)
- `WhatsAppChannel` implementation (same Twilio SDK)
- Credentials resolver with per-tenant override
- Encrypted credential storage for BYO Twilio accounts
- Message template approval flow (Meta requires pre-approved templates)
- Gate WhatsApp behind Pro tier; BYO sender behind Enterprise

### Phase 3 — Viber (2–3 weeks, demand-driven)
- `ViberChannel` via Viber REST API
- Evaluate only if tenant market analysis shows Viber-dominant regions
- Gate behind Enterprise tier

---

## Open Questions

1. **Reminder timing** — single reminder 24h before, or a sequence (24h + 2h)?
2. **Multi-language templates** — does each tenant configure message text, or fixed DentFlow templates?
3. **Failed delivery retry** — retry once after 15 minutes, or fall back to a different channel?
4. **Staff notifications** — same infrastructure, or separate concern? (e.g., notify dentist of cancellation)
5. **US vs. international launch** — determines A2P 10DLC registration urgency and WhatsApp template locale requirements.
