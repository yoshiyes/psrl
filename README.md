# Passerelle

A self-hosted link aggregator for curating and sharing links, organized by categories.

![License: AGPL v3](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10-512BD4.svg)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-336791.svg)

## Features

- **Public link board** with search, filtering, sorting, and pagination.
- **RSS feed** at `/feed.xml` (last 50 links).
- **Admin area** (`/admin`) — full CRUD management for links and categories.
- **Dark mode** with `localStorage` and `prefers-color-scheme` support.
- **NO SPA** — fast server rendering powered by HTMX 2.

## Tech stack

| Layer    | Technology                                      |
| -------- | ----------------------------------------------- |
| Backend  | .NET 10, ASP.NET Core MVC                       |
| Views    | Razor Views, Areas, HTMX 2                      |
| Database | PostgreSQL 18, Entity Framework Core 10, Npgsql |
| CSS      | Tailwind CSS 4                                  |

## Project structure

```
.
├── .github/
│   └── workflows/
│       └── docker-publish.yml # CI/CD GitHub Actions
├── compose.yaml
├── Passerelle.slnx
├── Directory.Packages.props
└── src/
    ├── Domain/                # Entities and domain logic
    └── Host/                  # Web application
        ├── Areas/Admin/       # Administration area
        ├── Controllers/       # Public controllers, authentication, RSS
        ├── Data/              # DbContext and seeding data
        ├── Repositories/      # Data access layer
        ├── ViewModels/        # View models
        ├── Migrations/        # EF Core migrations
        ├── Views/             # Razor views
        └── wwwroot/           # Static assets and compiled CSS
```

## Quick start with Docker

The fastest way to run Passerelle is using Docker Compose.

### 1. Create a `compose.yaml` file

```yaml
services:
  app:
    image: ghcr.io/yoshiyes/psrl:latest
    container_name: passerelle-app
    restart: unless-stopped
    ports:
      - "8080:80"
    environment:
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=passerelle;Username=postgres;Password=change_me_db_password
      AllowedHosts: "*" # In production, set your domain e.g. "links.example.com"
      UserAdmin__Email: admin@example.com
      UserAdmin__Password: ChangeMe12345@
      Authentication__AuthRoute: "/auth"
      Authentication__LoginPath: "/auth/login"
    depends_on:
      - db

  db:
    image: postgres:18-alpine
    container_name: passerelle-db
    restart: unless-stopped
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: change_me_db_password
      POSTGRES_DB: passerelle
    volumes:
      - db_data:/var/lib/postgresql

volumes:
  db_data:
```

### 2. Start the services

```bash
docker compose up -d
```

### 3. Access the application

- **Public board:** `http://localhost:8080`
- **Admin area:** `http://localhost:8080/admin`
- **Admin login:** `http://localhost:8080/auth/login`

---

## Local development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (to run PostgreSQL)
- [Node.js](https://nodejs.org/) *(optional, only needed when modifying Tailwind CSS)*

### Setup & Run

1. **Clone the repository:**
   ```bash
   git clone git@github.com:yoshiyes/psrl.git
   cd psrl
   ```

2. **Start the PostgreSQL database:**
   ```bash
   docker compose up -d db
   ```

3. **Run the application:**
   ```bash
   dotnet run --project src/Host
   ```

> [!TIP]
> Pending database migrations and sample data (`sample_links.json`) are automatically seeded on startup in the `Development` environment.

### Rebuilding CSS (optional)

Tailwind CSS is pre-built into `src/Host/wwwroot/css/app.css`. If you modify `src/Host/wwwroot/css/site.css` or Razor templates:

```bash
cd src/Host
npm install
npm run tw:watch   # Rebuild on change (development)
npm run tw:build   # Optimized production build
```

---

## Default administrator

On first startup, the application creates a default admin account based on the configuration (`UserAdmin` settings):

| Setting | Default (Development) |
|---|---|
| **Email** | `admin@linkshare.local` |
| **Password** | `Admin12345@` |

> [!IMPORTANT]
> Password policy requires **at least 10 characters**, containing at least one uppercase letter, one lowercase letter, and one digit.

> [!WARNING]
> Always change `UserAdmin__Password` and `UserAdmin__Email` before deploying to a public or production environment.

---

## Configuration

Passerelle can be configured via `src/Host/appsettings.json` or environment variables (using the `__` separator):

| Key / Environment Variable | Description | Default                                                                           |
|---|---|-----------------------------------------------------------------------------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=passerelle;Username=postgres;Password=password` |
| `UserAdmin__Email` | Email for the initial administrator | `admin@linkshare.local`                                                           |
| `UserAdmin__Password` | Password for the initial administrator | `Admin12345@`                                                                     |
| `Authentication__AuthRoute` | Base route for auth controller | `/auth`                                                                           |
| `Authentication__LoginPath` | URL path for the login page | `/auth/login`                                                                     |
| `AllowedHosts` | Allowed host headers | `*` (ex: yourdomain.eu)                                                           |

---

## CI/CD & Docker releases

### Automated Release (GitHub Actions)

A GitHub Actions workflow is configured in [.github/workflows/docker-publish.yml](.github/workflows/docker-publish.yml).

To trigger a release:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## Database migrations

To generate a new EF Core migration after modifying entities:

```bash
dotnet tool install --global dotnet-ef 
dotnet ef migrations add <MigrationName> --project src/Host
```

## Testing

There is currently no automated test suite. Contributions adding test coverage (unit or integration) are very welcome.

## Contributing

Contributions are welcome! Feel free to open an issue or a pull request. For larger changes, please open an issue first to discuss what you'd like to change.

## License

Passerelle is licensed under the [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.html).
