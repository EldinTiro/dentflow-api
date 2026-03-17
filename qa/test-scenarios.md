# DentFlow — QA Test Scenarios

This file is used by the Copilot agent on the QA machine to execute browser-based tests via Playwright MCP.
Run these scenarios after every pull from `main` or `develop`.

---

## Prerequisites
- App running at http://localhost:3000
- API running at http://localhost:5000
- SuperAdmin credentials: `superadmin@DentFlow.local` / `SuperAdmin123!@#`

---

## 1. Authentication

### 1.1 Login — valid credentials
1. Go to http://localhost:3000/login
2. Enter email `superadmin@DentFlow.local` and password `SuperAdmin123!@#`
3. Click Sign in
4. **Expected:** Redirected to dashboard, user name visible in the UI

### 1.2 Login — invalid credentials
1. Go to http://localhost:3000/login
2. Enter email `wrong@example.com` and password `wrongpassword`
3. Click Sign in
4. **Expected:** Error message "Invalid email or password" shown, no redirect

### 1.3 Logout
1. Log in as SuperAdmin
2. Click logout/avatar menu
3. **Expected:** Redirected back to /login, token cleared

---

## 2. Tenants (SuperAdmin only)

### 2.1 Create tenant
1. Log in as SuperAdmin
2. Navigate to Tenants
3. Click "New Tenant", fill in name and slug
4. Submit
5. **Expected:** New tenant appears in the tenant list

### 2.2 View tenant list
1. Log in as SuperAdmin
2. Navigate to Tenants
3. **Expected:** List of tenants loads without error

---

## 3. Patients

### 3.1 Create patient
1. Log in
2. Navigate to Patients
3. Click "New Patient"
4. Fill in first name, last name, date of birth, phone
5. Submit
6. **Expected:** Patient appears in the patients list

### 3.2 Edit patient
1. Navigate to Patients
2. Click on an existing patient
3. Edit the phone number
4. Save
5. **Expected:** Updated phone number shown in the list/detail

### 3.3 Delete patient
1. Navigate to Patients
2. Click on an existing patient
3. Click Delete and confirm
4. **Expected:** Patient removed from the list

---

## 4. Staff

### 4.1 Create staff member
1. Navigate to Staff
2. Click "New Staff Member"
3. Fill in first name, last name, role (e.g. Dentist), email
4. Submit
5. **Expected:** Staff member appears in the staff list

### 4.2 Edit staff member
1. Navigate to Staff
2. Click on an existing staff member
3. Edit their role
4. Save
5. **Expected:** Updated role shown in the list

---

## 5. Appointments

### 5.1 Book appointment
1. Navigate to Appointments
2. Click "Book Appointment"
3. Select a patient, staff member, date/time, and appointment type
4. Submit
5. **Expected:** Appointment appears on the calendar

### 5.2 Reschedule appointment
1. Click an existing appointment on the calendar
2. Click Reschedule
3. Pick a new date/time
4. Confirm
5. **Expected:** Appointment moves to the new slot

### 5.3 Cancel appointment
1. Click an existing appointment
2. Click Cancel and confirm
3. **Expected:** Appointment removed from the calendar

---

## 6. Treatments

### 6.1 Create treatment plan
1. Navigate to a patient's detail page
2. Go to the Treatments tab
3. Click "New Treatment Plan"
4. Add a plan name and at least one item
5. Submit
6. **Expected:** Treatment plan visible in the patient's treatments tab

---

## 7. Reporting / After all scenarios

After running all scenarios, file a GitHub Issue using the QA Report template in:
`dentflow-api/.github/ISSUE_TEMPLATE/qa-report.md`

Include:
- The current commit hash (`git rev-parse --short HEAD`)
- Pass/fail for each scenario above
- Screenshots for any failures
