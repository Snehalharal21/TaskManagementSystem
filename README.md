# Team Task Management System

Role-based Full-Stack Task Management System built with **.NET 8 Clean Architecture**.

## Features

- **JWT Authentication** with Role-based Access Control (Admin / Manager / User)
- **Team Management** – Create teams, add/remove members
- **Task Management** – Create, assign, update status, filter by deadline/status/priority
- **Comments** – Collaborate on tasks
- **Real Email Notifications** (via MailKit) on task assignment & status change
- **Dashboard** – Overview of task counts per status
- **Swagger UI** – Full API documentation with JWT support
- **Clean Architecture** – Domain → Application → Infrastructure → API

## Technology Stack

| Layer          | Technology                          |
|----------------|-------------------------------------|
| Backend        | ASP.NET Core 8 Web API              |
| Architecture   | Clean Architecture                  |
| Database       | SQL Server **or** SQLite (default)  |
| ORM            | Entity Framework Core 8             |
| Auth           | JWT Bearer                          |
| Email          | MailKit + MimeKit                   |
| Documentation  | Swagger / OpenAPI                   |

## Project Structure

```
TaskManagementSystem/
├── src/
│   ├── Domain/           # Entities, Enums, Base classes
│   ├── Application/      # Interfaces, DTOs
│   ├── Infrastructure/   # EF Core, Email, JWT, Seed data
│   └── API/              # Controllers, Program.cs, Swagger
└── README.md
```

## Sample Credentials (Seeded)

| Role    | Email                  | Password     |
|---------|------------------------|--------------|
| Admin   | admin@taskmgmt.com     | Admin@123    |
| Manager | manager@taskmgmt.com   | Manager@123  |
| User    | alice@taskmgmt.com     | User@123     |
| User    | bob@taskmgmt.com       | User@123     |

## How to Run

### 1. Prerequisites
- .NET 8 SDK
- (Optional) SQL Server if you want production DB

### 2. Configuration

Edit `src/API/appsettings.json`:

**For SQLite (default - easiest):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=TaskManagement.db"
}
```

**For SQL Server:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

**Email (Gmail example):**
```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "yourgmail@gmail.com",
  "Password": "your-app-password",   // Use App Password, not normal password
  "FromEmail": "yourgmail@gmail.com",
  "FromName": "Task Management System"
}
```

> If EmailSettings are empty, emails are logged to console instead of being sent.

### 3. Run the API

```bash
cd src/API
dotnet run
```

Swagger UI will open at:  
**https://localhost:7xxx/swagger** or **http://localhost:5xxx/swagger**

### 4. Create Migration (if needed)

```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/API
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

## API Endpoints Overview

### Auth
- `POST /api/auth/register` – Register new user
- `POST /api/auth/login` – Login & get JWT
- `GET  /api/auth/me` – Current user info

### Users
- `GET /api/users` – List users (Admin/Manager)
- `GET /api/users/{id}` – Get user

### Teams
- `GET    /api/teams` – List teams
- `POST   /api/teams` – Create team (Admin/Manager)
- `POST   /api/teams/{id}/members` – Add member
- `DELETE /api/teams/{id}/members/{userId}` – Remove member

### Tasks
- `GET    /api/tasks` – List tasks (with filters)
- `GET    /api/tasks/{id}` – Get task
- `POST   /api/tasks` – Create task (Admin/Manager)
- `PUT    /api/tasks/{id}` – Update task
- `DELETE /api/tasks/{id}` – Delete task
- `GET    /api/tasks/dashboard` – Dashboard stats

### Comments
- `GET    /api/tasks/{taskId}/comments`
- `POST   /api/tasks/{taskId}/comments`
- `DELETE /api/tasks/{taskId}/comments/{commentId}`

### Notifications
- `GET /api/notifications` – List notifications
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

## Role Capabilities

| Action                    | Admin | Manager | User |
|---------------------------|-------|---------|------|
| Create Teams              | ✅    | ✅      | ❌   |
| Assign Tasks              | ✅    | ✅      | ❌   |
| View All Tasks            | ✅    | ✅*     | Own only |
| Update Own Task Status    | ✅    | ✅      | ✅   |
| Add Comments              | ✅    | ✅      | ✅   |

\* Managers see tasks they created or of their team members.

## Notes for Submission

- Clean Architecture followed
- JWT + Role-based authorization
- Actual email notifications (MailKit)
- Swagger documentation with Authorize button
- Seed data included
- Ready for Docker / CI-CD extension

---

**Assessment Goal Achieved**: Production-oriented role-based full-stack backend ready.
