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
| **Storage** | Azure Blob Storage (or Azurite emulator) |
| **Monitoring** | Application Insights, SonarCloud |
| **API Docs** | Scalar (OpenAPI) |

---

## 🏗 Project Structure

- `SSSKLv2/`: The main ASP.NET Core API and backend logic.
- `Frontend/`: The Angular single-page application.
- `SSSKLv2.AppHost/`: .NET Aspire orchestration project for local development.
- `SSSKLv2.ServiceDefaults/`: Shared service configurations (resilience, service discovery).
- `SSSKLv2.Test/`: Integrated and unit tests.

---

## 🏁 Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (Version 9+)
- [Node.js](https://nodejs.org/) (Latest LTS)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (For database and storage emulators or running SQL Server)

### Local Development (Aspire)
The easiest way to run the entire stack is using .NET Aspire:

1. Clone the repository.
2. Open the solution in **Visual Studio 2022** or **JetBrains Rider**.
3. Set `SSSKLv2.AppHost` as the startup project.
4. Press `F5` to start. This will launch the Aspire Dashboard, SQL Server, Azurite, the API, and the Frontend.

Alternatively, you can use the **CLI**:
```bash
dotnet run --project SSSKLv2.AppHost
```

### Manual Setup
If you prefer to run services manually:

1. Start the database:
   ```bash
   docker-compose up -d
   ```
2. Run the Backend:
   ```bash
   cd SSSKLv2
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
`https://localhost:5251/scalar`

---

## 🚢 Deployment
- **Frontend**: Deployed to **Azure Static Web Apps**.
- **Backend**: Deployed to **Azure App Service** or **Azure Container Apps**.
- **CI/CD**: Automated via GitHub Actions (see `.github/workflows`).

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE.txt](file:///Users/ricardotill/Development/Repositories/SSSKLv2/LICENSE.txt) file for details.

---

Developed with ❤️ for **Scouting Wilo**.