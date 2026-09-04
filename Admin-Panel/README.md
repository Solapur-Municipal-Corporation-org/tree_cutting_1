# Admin Panel

This is the isolated administration application boundary. It starts after common SMC officer authentication and contains no login page, citizen UI, mock records, or fabricated application data.

Configure `Backend/appsettings.json` with the same SQL Server connection string and common SMC JWT authority/audience used by User-End. The Admin backend reads the existing `Application`, `ApplicationDocument`, `ApplicationPhoto`, and master tables. On startup it creates only `TreeCuttingWorkflowHistory` and `TreeCuttingDepartmentReview` if they do not already exist.

The role queue is available at `GET /api/admin/tree-cutting/applications`. Workflow actions are `POST /api/admin/tree-cutting/applications/{id}/inspection`, `construction-review`, `hod-review`, `committee-review`, and `commissioner-review`; every action validates the authenticated claim role, current status, required fields, and writes review plus workflow history in one transaction. Existing stored documents and photos are served through the corresponding Admin file routes.

The current source schema contains application type, applicant type, and document type masters, so their authenticated CRUD routes are under `/api/admin/tree-cutting/masters`. Zones, prabhags, wards, departments, designations, and workflow-stage masters are not present in the inspected schema and are not invented here.

For local Development testing only, `GET /api/admin/test-login/options` reads the active roles from the database and `POST /api/admin/test-login` creates an HttpOnly test session for the selected role. Startup creates `test-ags`, `test-nagar_abhi...`, and the corresponding test employee-role mappings only in Development; this path is not registered in production. No passwords or production users are created.
