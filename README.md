# Platinum Credential Manager

A full-stack application for securely storing and managing user credentials. It consists of a .NET 10 ASP.NET Core REST API backend and a Vue 3 single-page application frontend.

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ |
| [Node.js](https://nodejs.org/) | 20.19+ or 22.12+ |

## Project Structure

```
platinum-credential-manager/
├── PlatinumCredentialManager.Api/     # ASP.NET Core backend
└── PlatinumCredentialManager.Client/  # Vue 3 + Vite frontend
```

## Getting Started

Both services must be running for the application to work. Start them in separate terminals.

### Backend (API)

```bash
cd PlatinumCredentialManager.Api
dotnet build
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5142`
- HTTPS: `https://localhost:7032`

### Frontend

```bash
cd PlatinumCredentialManager.Client
npm install
npm run dev
```

The dev server will start and print the local URL (typically `http://localhost:5173`).

## Tech Stack

- **Backend:** ASP.NET Core (.NET 10), C#
- **Frontend:** Vue 3, Vite, Axios
