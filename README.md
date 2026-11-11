# SSSKLv2

| Component | Quality Gate | Coverage |
| :--- | :--- | :--- |
| **Backend** | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SSSKLv2_Backend&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=SSSKLv2_Backend) | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=SSSKLv2_Backend&metric=coverage)](https://sonarcloud.io/summary/new_code?id=SSSKLv2_Backend) |
| **Frontend** | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SSSKLv2_Frontend&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=SSSKLv2_Frontend) | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=SSSKLv2_Frontend&metric=coverage)](https://sonarcloud.io/summary/new_code?id=SSSKLv2_Frontend) |


SSSKLv2 is a modern, full-stack management and Point of Sale (POS) system designed for clubs and organizations. It provides a seamless interface for managing products, users, events, and financial transactions, with a focus on ease of use and visual excellence.

---

## 🚀 Key Features

### 🛒 Point of Sale (POS)
- **Fast Transactions**: Optimized interface for selling drinks, snacks, and shots.
- **Personal Saldo**: Users can maintain a balance and pay using their personal credit.
- **Top-ups**: Easy management of user balances.
- **Live Metrics**: Real-time sales and activity tracking via SignalR.

### 🔐 Security & Identity
- **Modern Authentication**: Support for standard Identity and **Passkey (WebAuthn)** for passwordless login.
- **Role-Based Access**: Granular permissions for Admins, Users, Kiosks, and Guests.
- **Secure by Default**: HttpOnly, secure cookies, and CSRF protection.

### 📅 Event Management
- **Organization**: Create and manage upcoming events.
- **Visuals**: Upload and manage event images using Azure Blob Storage.
- **Public Calendar**: Keep members informed about upcoming activities.

### 🏆 Gamification
- **Achievements**: Unlockable achievements based on sales or participation.
- **Leaderboard**: Competitive view to see the most active members.

### 🛠 Administration
- **Dashboard**: Comprehensive management of products, users, and achievements.
- **Reporting**: Export order history and financial data to CSV.
- **API Reference**: Built-in interactive API documentation using Scalar.

---

## 🛠 Tech Stack

| Component | Technology |
| :--- | :--- |
| **Frontend** | Angular 21, PrimeNG, SignalR |
| **Backend** | .NET (C#), ASP.NET Core Identity, Entity Framework Core |
| **Orchestration** | .NET Aspire |
| **Database** | Microsoft SQL Server |
| **Storage** | Azure Blob Storage (Azurite emulator in development, local files in Docker) |
| **Monitoring** | Application Insights, SonarCloud |
| **API Docs** | Scalar (OpenAPI) |

---

## 🏗 Project Structure

- `Backend/`: the .NET API, in an onion architecture (dependencies only point inwards):
  - `SSSKL.Domain/`: Entities, domain events, constants and domain exceptions.
  - `SSSKL.Contracts/`: DTOs and the interfaces of repositories, services and agents.
  - `SSSKL.Application/`: Business logic (services, domain event handlers, validators).
  - `SSSKL.Infrastructure/`: EF Core persistence and migrations, repositories, Blob Storage, email, web push and Turnstile.
  - `SSSKL.Web/`: The ASP.NET Core API host (controllers, Identity endpoints, SignalR).
  - `SSSKL.AppHost/`: .NET Aspire orchestration project for local development.
  - `SSSKL.ServiceDefaults/`: Shared service configurations (resilience, service discovery).
  - `SSSKL.Application.Test/`, `SSSKL.Infrastructure.Test/`, `SSSKL.Web.Test/`: Unit and integration tests per layer.
  - `Dockerfile`: the API image.
- `Frontend/`: The Angular single-page application, with the `Dockerfile` and `Caddyfile` of the web app image.
- `Mobile/`: The .NET MAUI app for iOS and Android.
- `docs/`: Setup guides (Docker, licenses, Application Insights queries) and the changelog.
- `docker-compose.yml`, `docker-compose.external.yml`: run SSSKL with Docker (see [docs/docker.md](docs/docker.md)).

---

## 🏁 Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (Version 11)
- [Node.js](https://nodejs.org/) (Latest LTS)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (For database and storage emulators or running SQL Server)

### Local Development (Aspire)
The easiest way to run the entire stack is using .NET Aspire:

1. Clone the repository.
2. Open the solution in **Visual Studio 2022** or **JetBrains Rider**.
3. Set `SSSKL.AppHost` (in `Backend/`) as the startup project.
4. Press `F5` to start. This will launch the Aspire Dashboard, SQL Server, Azurite, the API, and the Frontend.

Alternatively, you can use the **CLI**:
```bash
dotnet run --project Backend/SSSKL.AppHost
```

### Manual Setup
If you prefer to run services manually:

1. Start SQL Server (matching `ConnectionStrings:DefaultConnection` in `appsettings.json`):
   ```bash
   docker run -d -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=Password123 -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
   ```
2. Run the Backend:
   ```bash
   cd Backend/SSSKL.Web
   dotnet run
   ```
3. Run the Frontend:
   ```bash
   cd Frontend
   npm install
   npm start
   ```

---

## 🌐 API Documentation
When running in Development mode, you can access the interactive Scalar API documentation at:
`http://localhost:5251/scalar`

---

## 🚢 Deployment
- **Releases**: push a tag `vX.Y.Z` on master; the version comes from the tag (see [docs/docker.md](docs/docker.md#releases)).
- **Frontend**: Deployed to **Azure Static Web Apps**, and published as the Docker image `ghcr.io/ricardotill/ssskl-frontend`.
- **Backend**: Deployed to **Azure App Service**, and published as the Docker image `ghcr.io/ricardotill/ssskl-api`.
- **Docker**: run the whole stack with `docker compose up -d` (see [docs/docker.md](docs/docker.md)).
- **CI/CD**: Automated via GitHub Actions (see `.github/workflows`).

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

---

Developed with ❤️ for **Scouting Wilo**.