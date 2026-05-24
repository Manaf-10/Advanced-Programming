# Advanced-Programming

## DATE: 3/20/2026


### By:
[Mohamed Alnooh](https://github.com/alnoohy) | 
[Manaf Hujairi](https://github.com/Manaf-10) | 
[Abdulla Alwaraqaa](https://github.com/AbdullahAlwarqaa) | 
[Mohamed Jaffar](https://github.com/MohamedJaafar0) | [Mahmood Abdulla](https://github.com/Mahm64d)

## ERD

![erd-image](./assets/erd_img.png)

## Routing Table


Notion routing table: [Routing Table](https://www.notion.so/Routing-Table-32c1a2392a5e8075935ac7b550635ad6?source=copy_link)

### Authentication

| Method | Route | Access | Description | Status |
| --- | --- | --- | --- | --- |
| GET | `/Account/Login` | Public | Display the login form | Finished |
| POST | `/Account/Login` | Public | Authenticate user and create cookie session | Finished |
| GET | `/Account/Register` | Public | Display the patient registration form | Finished |
| POST | `/Account/Register` | Public | Create a new patient account | Finished |
| POST | `/Account/Logout` | Authenticated | Sign out the current user | Finished |
| GET | `/Account/Profile` | Authenticated | Display the current user's profile | Finished |
| POST | `/api/auth/token` | Public | Authenticate API/reporting user and issue JWT | Finished |

### Appointment

| Method | Route | Access | Description | Status |
| --- | --- | --- | --- | --- |
| GET | `/Appointments` | Authenticated | Display appointment list for the current role | Finished |
| GET | `/Appointments/Details/{id}` | Doctor | Display appointment visit/details form for in-progress appointments | Finished |
| POST | `/Appointments/UpdateStatus` | Admin, Reception, Doctor | Update appointment status and broadcast live update via SignalR | Finished |
| POST | `/Appointments/UpdateDetails` | Doctor | Save visit details and complete an appointment | Finished |
| GET | `/Appointments/PatientAppointments` | Patient | Display the current patient's appointments | Finished |
| GET | `/Lookup` | Public | Display public appointment lookup form | Finished |
| POST | `/Lookup` | Public | Lookup appointments by CPR and patient reference through the API | Finished |
| GET | `/api/appointments/lookup` | Public API | Lookup active appointments by CPR and patient reference | Finished |
| POST | `/api/public/appointments/lookup` | Public API | Lookup upcoming appointments and recent visits by CPR and patient reference | Finished |
| HUB | `/hubs/appointments` | Staff | SignalR hub used for live appointment status updates | Finished |
| GET | `/appointments/book` | Staff | Display appointment booking form | Not finished |
| POST | `/appointments/book` | Staff | Create a new appointment | Not finished |

### Visit History

| Method | Route | Access | Description | Status |
| --- | --- | --- | --- | --- |
| GET | `/Patients/History` | Patient | Display the current patient's full visit history | Finished |
| GET | `/visit-records/{id}` | Doctor | Display visit diagnosis, doctor notes, and prescriptions | Finished |
| POST | `/Appointments/UpdateDetails` | Doctor | Create or update a visit record when completing an appointment | Finished |
| POST | `/Appointments/UpdateDetails` | Doctor | Save prescription text linked to a completed visit | Finished |
| GET | `/patients/prescriptions` | Patient | Display the current patient's prescriptions | Finished |
| GET | `/patients/{id}/history` | Staff | Display any patient's full visit history | Not finished |
| GET | `/prescriptions/{id}` | Staff and the Patient | Display a single prescription for an authorized user | Not finished |

### Doctor

| Method | Route | Access | Description | Status |
| --- | --- | --- | --- | --- |
| POST | `/Appointments/UpdateDetails` | Doctor | Create a new diagnosis or visit record | Finished |
| GET | `/visit-records` | Doctor | Display visit records for the logged-in doctor's patients | Finished |
| GET | `/Doctor` | Staff | List all doctors with schedules and upcoming appointment counts | Finished |
| GET | `/doctors/me/availability` | Doctor | Display the logged-in doctor's weekly availability | Finished |
| GET | `/doctors/me/appointments` | Doctor | Display appointments assigned to the logged-in doctor | Finished |
| GET | `/doctors/{id}/availability` | Staff | Display available time slots for a selected doctor | Not finished |
| GET | `/doctors/{id}/appointments` | Staff | Display appointments assigned to a selected doctor | Not finished |

### Notifications

| Method | Route | Access | Description | Status |
| --- | --- | --- | --- | --- |
| GET | Layout notification dropdown | Authenticated | Display appointment alerts and system notifications in the shared layout | Finished |
| POST | `/Notifications/Delete/{id}` | Authenticated | Delete a dismissed notification from the database | Finished |
| POST | `/Notifications/Clear` | Authenticated | Delete all notifications for the current user | Finished |
| GET | `/notifications` | Authenticated | Display a standalone notifications page | Not finished |

### Manager

| Method | Route | Access | Description | Status |
| --- | --- | --- | --- | --- |
| GET | `/Doctor` | Staff | List all doctors | Finished |
| GET | `/reports` | Clinic Manager | Display reporting dashboard | Finished |
| GET | `/reports/statistics` | Clinic Manager | Display overall appointment statistics | Finished |
| GET | `/reports/doctor-workload` | Clinic Manager | Display doctor workload and utilization report | Finished |
| GET | `/reports/cancellations` | Clinic Manager | Display cancellation and missed appointment report | Finished |
| GET | `/api/reports/summary` | Clinic Manager API | Return overall appointment statistics | Finished |
| GET | `/api/reports/doctor-workload` | Clinic Manager API | Return doctor workload and utilization report data | Finished |
| GET | `/api/reports/cancellations` | Clinic Manager API | Return cancellation and missed appointment report data | Finished |
| GET | `/api/reports/appointment-status` | Clinic Manager API | Return appointment status breakdown data | Finished |
| GET | `/doctors/{id}` | Clinic Manager | Display doctor details and edit form | Not finished |
| PUT | `/doctors/{id}` | Clinic Manager | Update doctor profile details | Not finished |
| PUT | `/appointments/{id}` | Clinic Manager | Update appointment details | Not finished |
| POST | `/doctors/{id}/availability` | Clinic Manager | Create or update doctor availability | Not finished |

### Specializations

| Method | Route | Access | Description | Status |
| --- | --- | --- | --- | --- |
| GET | `/specializations` | Staff | List specializations | Not finished |
| POST | `/specializations` | Clinic Manager | Create specialization | Not finished |
| PUT | `/specializations/{id}` | Clinic Manager | Edit specialization | Not finished |
| POST | `/doctors/{id}/specializations` | Clinic Manager | Assign specialization to doctor | Not finished |
