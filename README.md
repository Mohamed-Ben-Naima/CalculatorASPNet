# Calculix — Premium ASP.NET Calculator

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC_%2B_Web_API-blue?style=for-the-badge&logo=asp.net)
![EF Core](https://img.shields.io/badge/Entity_Framework_Core-8.0-68217A?style=for-the-badge&logo=entity-framework)
![SQL Server](https://img.shields.io/badge/Microsoft_SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver)
![xUnit](https://img.shields.io/badge/xUnit-46_tests-512BD4?style=for-the-badge&logo=xunit)

A polished, dark-fintech **calculator application** built with ASP.NET Core (.NET 8). It pairs a beautiful MVC front-end (keyboard + mouse, live history) with a **REST API** whose expressions are evaluated server-side by a custom arithmetic engine and persisted with EF Core.

---

## ✨ Features

### 🧮 The Engine (`CalculatorService`)
A dependency-free recursive-descent parser supporting:

- Operators: `+ − × ÷ % (modulo) ^ (power)` with correct precedence and **right-associative exponentiation**
- Unary operators: `-5`, `-(2+3)`, `2*-3`
- Functions: `sqrt(x)`, `abs(x)`, `floor(x)`, `ceil(x)`, `round(x)`
- Constants: `pi`, `e`
- Friendly input: `×`, `÷`, `−` glyphs, comma or dot decimals, whitespace anywhere
- Rigorous errors with machine-readable codes:
  - `INVALID_EXPRESSION` — malformed or unrecognized input
  - `DIVISION_BY_ZERO` — `1/0`, `0^-1`, `sqrt(-1)` paths
  - `OVERFLOW` — results outside the supported `decimal` range
- `checked` arithmetic throughout — no silent overflow

### 🎛️ The UI
- Dark fintech design with a glowing display, operator-colored keys and an equals button with a gradient
- Full **keyboard support** (digits, operators, `Enter` = , `Backspace`, `Esc` = AC)
- On-screen **status indicator** (Ready / Calculating… / Error / Offline)
- Every `=` posts the expression to the server — evaluation and validation happen **server-side**

### 📜 History
- Every successful calculation is persisted (`Expression`, `Result`, `CreatedAt`) via EF Core + SQL Server
- Side-panel history with one-click **Clear all**, auto-refreshing after each evaluation
- Served to the MVC page and exposed over the API

### 🌐 REST API (`/api/calculations`)
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/calculations` | Evaluate `{ "expression": "2+3*4" }` → `200` with result, or `400` with `error`/`errorCode` |
| `GET` | `/api/calculations?limit=50` | Most recent calculations (latest first, max 200) |
| `GET` | `/api/calculations/{id}` | A single stored calculation |
| `DELETE` | `/api/calculations/{id}` | Delete one entry |
| `DELETE` | `/api/calculations` | Clear the history |

---

## 🛡️ Security & Best Practices

- All mutations are **antiforgery-protected** (`[ValidateAntiForgeryToken]`) — the JS client forwards the token from the page
- Expressions are sanitized and validated **server-side only**; the client never trusts its own input
- Clear controller/service/contract separation, immutable-style DTOs for the API surface
- No secrets in code — connection string lives in configuration

---

## 🧪 Testing

```bash
dotnet test CalculatorASPNet.sln
```

The xUnit suite (46 tests, enforced by CI) covers the engine exhaustively:

- Basic arithmetic, precedence, parentheses and associativity
- Unary minus edge cases (`-2^2` = −4, `2^-2` = 0.25, `2^3^2` = 512)
- Friendly symbols (`6×3`, `10÷4`, comma decimals), whitespace
- Functions and constants (`sqrt`, `abs`, `floor`/`ceil`/`round`, `pi`, `e`)
- Malformed-input rejection, divide-by-zero, overflow, and large-number handling

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (Express / LocalDB) — the app auto-migrates on startup

```bash
cd CalculatorASPNet
dotnet run --launch-profile https
```

Open `https://localhost:7032` (or `http://localhost:5276`) and start typing.

Try the API:
```bash
curl -X POST http://localhost:5276/api/calculations \
     -H "Content-Type: application/json" \
     -d '{"expression":"sqrt((3+1)*(6-2))"}'
```

---

## 📂 Project Structure

```
CalculatorASPNet/
├── Services/CalculatorService.cs     # Expression engine (recursive descent)
├── Controllers/
│   ├── HomeController.cs             # Calculator page + antiforgery mutations
│   └── CalculationsController.cs     # /api/calculations REST surface
├── Contracts/CalculationDtos.cs      # Request/response DTOs
├── Models/CalculationHistory.cs      # Persisted history entity
├── Data/CalculatorDbContext.cs       # EF Core context (SQL Server)
├── Migrations/                       # Code-first schema
├── Views/Home/                       # Dark fintech calculator UI
├── wwwroot/css/site.css · js/site.js # Custom theme + keypad/keyboard logic
├── CalculatorASPNet.Tests/           # xUnit suite (46 tests)
└── .github/workflows/ci.yml          # Build + test on every push/PR
```

---

## 🔄 Roadmap

- [x] Server-side expression engine with functions & constants
- [x] REST API with persisted history
- [x] Keyboard + mouse calculator UI (dark fintech theme)
- [x] Unit tests + CI
- [ ] Scientific functions (`sin`, `cos`, `log`, `ln`)
- [ ] Per-IP history with soft delete
- [ ] Docker image + compose profile

---

*Built as a senior-level .NET showcase: custom parsing, security-first MVC, a real REST API, automated testing and container-ready structure.*