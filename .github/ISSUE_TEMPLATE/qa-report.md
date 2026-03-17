---
name: QA Test Report
about: Manual / AI-assisted test results for a release candidate
title: "QA Report — [commit or feature name]"
labels: qa
assignees: ''
---

## Build Info
- **Repo:** dentflow-api / dentflow-web
- **Branch:** 
- **Commit:** 
- **Date tested:** 
- **Tested by:** 

---

## Environment
- **Machine:** QA machine
- **URL:** http://localhost:3000
- **API:** http://localhost:5000

---

## Test Results

| # | Feature | Scenario | Result | Notes |
|---|---------|----------|--------|-------|
| 1 | Auth | Login with valid credentials | ⬜ Pass / ⬜ Fail | |
| 2 | Auth | Login with invalid credentials shows error | ⬜ Pass / ⬜ Fail | |
| 3 | Patients | Create new patient | ⬜ Pass / ⬜ Fail | |
| 4 | Patients | Edit patient details | ⬜ Pass / ⬜ Fail | |
| 5 | Patients | Delete patient | ⬜ Pass / ⬜ Fail | |
| 6 | Staff | Create new staff member | ⬜ Pass / ⬜ Fail | |
| 7 | Staff | Edit staff member | ⬜ Pass / ⬜ Fail | |
| 8 | Appointments | Book appointment | ⬜ Pass / ⬜ Fail | |
| 9 | Appointments | Reschedule appointment | ⬜ Pass / ⬜ Fail | |
| 10 | Appointments | Cancel appointment | ⬜ Pass / ⬜ Fail | |
| 11 | Tenants | Create tenant (SuperAdmin) | ⬜ Pass / ⬜ Fail | |
| 12 | Treatments | Create treatment plan | ⬜ Pass / ⬜ Fail | |

---

## Failed Tests — Details

### Issue 1: [Short description]
- **Scenario:** 
- **Steps to reproduce:**
  1. 
  2. 
  3. 
- **Expected:** 
- **Actual:** 
- **Screenshot:** *(attach below)*

---

## Summary
- Total tested: 
- ✅ Passed: 
- ❌ Failed: 
- ⏭️ Skipped: 
