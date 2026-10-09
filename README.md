# 🎫 TicketDesk: AI-powered support ticket desk

A mini Freshdesk/Zendesk: customers raise tickets, **AI triages them instantly** (category, priority, sentiment, summary), agents get a **drafted reply** and **matching knowledge-base articles**, and managers see a **live dashboard**.

**Stack:** ASP.NET Core 8 Web API · Entity Framework Core · SQL Server (SSMS) · JWT auth with roles · Angular 19 (standalone components, signals) · Google Gemini free tier (with an offline rule-based fallback).

## Quick start

**You need:** [.NET 8 SDK or newer](https://dotnet.microsoft.com/download), [Node.js 20+](https://nodejs.org), and SQL Server (LocalDB ships with Visual Studio; SQL Express / Developer also work).

Windows: double-click **`run.bat`**.
Mac/Linux: `./run.sh`

Or manually, in two terminals:

```bash
# 1) API  -> http://localhost:5080  (Swagger at /swagger)
cd backend/TicketDesk.Api
dotnet run

# 2) UI   -> http://localhost:4200
cd frontend
npm install
npm start
```

The API **creates the database (`TicketDeskDb`) and seeds demo data automatically** on first start. Open SSMS, connect to the same server, and you will see it.

### Demo accounts

| Role | Email | Password |
|---|---|---|
| Admin | admin@ticketdesk.com | Admin@123 |
| Agent | agent@ticketdesk.com | Agent@123 |
| Customer | customer@ticketdesk.com | Customer@123 |

The login page has one-click buttons for these.

## Choosing your SQL Server

Default connection string (`backend/TicketDesk.Api/appsettings.json`) uses **LocalDB**: `Server=(localdb)\MSSQLLocalDB`. Change `ConnectionStrings:Default` if you use something else:

| Server | Connection string |
|---|---|
| SQL Express | `Server=.\SQLEXPRESS;Database=TicketDeskDb;Trusted_Connection=True;TrustServerCertificate=True` |
| Local SQL Server (Windows auth) | `Server=localhost;Database=TicketDeskDb;Trusted_Connection=True;TrustServerCertificate=True` |
| Docker (`docker compose up -d`) | `Server=localhost,1433;Database=TicketDeskDb;User Id=sa;Password=Your_strong_Passw0rd;TrustServerCertificate=True` |

You can also override it without editing files: `set ConnectionStrings__Default=...` (Windows) / `export ConnectionStrings__Default=...` (Mac/Linux).
`database/useful-queries.sql` has handy queries and a sample stored procedure to run in SSMS.

## Turning on the real AI (free)

Out of the box the app uses a **local rule-based engine**, so everything works with no key. For real LLM triage and replies:

1. Get a free key at <https://aistudio.google.com/apikey>.
2. Set it (do **not** commit it):
   - Windows: `set Ai__ApiKey=YOUR_KEY` before `dotnet run`
   - Mac/Linux: `export Ai__ApiKey=YOUR_KEY`
   - or put it in `appsettings.Development.json` (git-ignored if you add it to `.gitignore`)
3. The model is configurable via `Ai:Model` (default `gemini-3.1-flash-lite`). Google renames and retires models over time; if you get a model error, pick a current free-tier model from the Gemini docs.

The sidebar chip shows whether AI is **Gemini (live)** or **Local rules**. If Gemini errors or hits a rate limit, the app silently falls back to local rules, so ticket creation never breaks.
Note: Google may use free-tier prompts to improve its products, so don't send real customer data.

## How it works

```
Angular UI ──JWT──► ASP.NET Core Web API ──EF Core──► SQL Server
                        │
                        ├─ AiService ──► Gemini (free)  ⟲ fallback ► RuleBasedAi
                        └─ KbSearchService (TF-IDF keyword ranking)
```

- **AI triage:** on ticket creation, one structured-JSON prompt returns category / priority / sentiment / summary. Output is validated against allowed values.
- **Prompt-injection hygiene:** ticket text is passed as data with explicit instructions to ignore commands inside it.
- **Suggested reply:** prompt includes ticket, recent public comments and the best KB articles; the agent edits before sending.
- **KB matching:** TF-IDF style ranking in C#. Swap `KbSearchService` for embeddings + cosine similarity to upgrade it.
- **Security:** BCrypt password hashing, JWT with Admin/Agent/Customer roles, customers can only see their own tickets, internal notes are hidden from customers.

### Main endpoints

| Method | Route | Who |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | public |
| GET/POST | `/api/tickets` | any user (customers see only their own) |
| GET | `/api/tickets/{id}` | owner or staff |
| PUT | `/api/tickets/{id}/status` | staff (customers: close/reopen) |
| PUT | `/api/tickets/{id}/assign`, `/meta` | staff |
| POST | `/api/tickets/{id}/comments` | any user with access |
| POST | `/api/tickets/{id}/suggest-reply`, `/retriage` | staff |
| GET | `/api/dashboard` | staff |
| GET/POST/PUT/DELETE | `/api/kb` | read: all · write: staff · delete: admin |

## Tests & CI

```bash
dotnet test backend/TicketDesk.Tests
```
GitHub Actions workflow in `.github/workflows/ci.yml` builds the API tests and the Angular app.

## Troubleshooting

- **API crashes at startup with a SQL error:** fix `ConnectionStrings:Default` (see table above).
- **UI says "Cannot reach the API":** make sure the API is running on port 5080 (or change `frontend/src/app/config.ts`).
- **Port 4200/5080 busy:** change `Urls` in `appsettings.json` and `API_URL` in `config.ts`.

## Ideas to extend (good interview talking points)

Real embeddings for KB search · SignalR live updates · EF Core migrations instead of `EnsureCreated` · refresh tokens · Docker image for the API · e2e tests with Playwright.
