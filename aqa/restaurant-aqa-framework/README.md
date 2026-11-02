# Restaurant Group Pre-Order Test Automation Framework (Direction 1)

[![Playwright & SpecFlow End-to-End Tests](https://github.com/owner/repo/actions/workflows/playwright.yml/badge.svg)](.github/workflows/playwright.yml)

Industrial-grade end-to-end and API test automation solution covering **Direction 1 — Group Pre-Order** up to **Level 3** and complete E2E flow for the restaurant web platform.

Built with **C# (.NET 10)**, **Playwright**, **SpecFlow (BDD)**, and the **Screenplay Pattern**, with full CI/CD pipeline integration via **GitHub Actions**.

---

## 📋 Table of Contents
1. [Direction & Scope](#-direction--scope)
2. [Architecture & Patterns](#-architecture--patterns)
3. [Project Structure](#-project-structure)
4. [Prerequisites & Environment Setup](#-prerequisites--environment-setup)
5. [Running Tests Locally](#-running-tests-locally)
6. [Filtering Tests by Tags & Levels](#-filtering-tests-by-tags--levels)
7. [Reports & Failure Artifacts](#-reports--failure-artifacts)
8. [Assumed Behavior for Edge Cases](#-assumed-behavior-for-edge-cases)
9. [CI/CD Pipeline (GitHub Actions)](#-cicd-pipeline-github-actions)

---

## 🎯 Direction & Scope

This automation suite fulfills **Direction 1 — Group Pre-Order** across all three functional levels:

### 🟢 Level 1: View Participants (UI & API)
* **Happy Path**: Owner creates a reservation; guests join via invite link and add dishes. The Owner verifies participant list, dishes, quantities, individual subtotals, and dynamic Grand Total updates upon cart changes.
* **Negative & Edge Cases**:
  * Participants who haven't submitted their cart are visually and contractually marked ("Still choosing").
  * Empty groups display `participants: []` and `total: 0.00`.
  * Non-existent group queries return `404 Not Found`.
* **Authorization**: Strictly verifies that **only** the group Owner can view participants (Guests, Waiters, Other Clients receive `403 Forbidden`, Unauthenticated users receive `401 Unauthorized`).

### 🔵 Level 2: Confirm Consolidated Pre-Order (UI & API)
* **Happy Path**: Owner submits the consolidated pre-order via UI modal. The order status updates to `SubmittedByCustomer`, group status displays `Sent to Restaurant`, identical dishes across different guests are merged and summed, and the Waiter sees the verified order in the restaurant system.
* **Negative & Edge Cases**:
  * Cannot submit if no participants have confirmed their cart (`400 Bad Request`).
  * Cannot submit if confirmed carts contain 0 items (`400 Bad Request`).
  * Cannot submit if group reservation or order does not exist (`404 Not Found`).
  * Cannot submit a group reservation that is already `Submitted` (`400 Bad Request`).
* **Authorization**: Only the Owner can submit (`403` / `401` for others).

### 🟣 Level 3: Remove Guest (UI & API)
* **Happy Path**: Owner removes a guest participant from the group. The remaining participants stay intact, the removed guest's subtotal is deducted, and the Grand Total recalculates dynamically.
* **Negative & Edge Cases**:
  * Removing a guest who has already confirmed their cart.
  * Removing the last participant leaves an empty group with total 0.00.
  * Attempting to delete a guest belonging to another group returns `400 Bad Request`.
  * Attempting to delete a non-existent session returns `404 Not Found`.
* **Authorization**: Only the Owner can remove guests (`403` / `401` for others).
* **Complete E2E Scenario**: Full lifecycle from guest cart addition $\rightarrow$ Owner review $\rightarrow$ Owner removes a guest $\rightarrow$ Owner submits $\rightarrow$ Waiter verifies order excluding removed guest items.

---

## 🏛️ Architecture & Patterns

The framework adheres to **BDD (Behavior-Driven Development)** and the **Screenplay Pattern** to maximize maintainability, readability, and code reuse.

```
                           +-------------------+
                           |  SpecFlow Feature |
                           |  (Gherkin syntax) |
                           +---------+---------+
                                     |
                                     v
                           +-------------------+
                           |  Step Definitions |
                           +---------+---------+
                                     |
                  +------------------+------------------+
                  |                                     |
                  v                                     v
        +-------------------+                 +-------------------+
        |       Actor       |                 |       Actor       |
        |  (Owner / Guest)  |                 | (Waiter / Client) |
        +---------+---------+                 +---------+---------+
                  |                                     |
         +--------+--------+                   +--------+--------+
         |                 |                   |                 |
         v                 v                   v                 v
   +-----------+     +-----------+       +-----------+     +-----------+
   |  Ability: |     |  Ability: |       |  Ability: |     |  Ability: |
   | BrowseWeb |     | CallAnApi |       | BrowseWeb |     | CallAnApi |
   +-----+-----+     +-----+-----+       +-----+-----+     +-----+-----+
         |                 |                   |                 |
         v                 v                   v                 v
   +-----------+     +-----------+       +-----------+     +-----------+
   | UI Tasks  |     | API Tasks |       | Questions |     | Page Obj  |
   | (Actions) |     | (Requests)|       |  (State)  |     | (Locators)|
   +-----------+     +-----------+       +-----------+     +-----------+
```

### Core Components
* **Actors**: Represent distinct users with individual credentials and context (`Owner`, `Guest`, `Waiter`, `Unauthorized`).
* **Abilities**:
  * `BrowseTheWeb`: Wraps Playwright's `IPage` and `IBrowserContext`.
  * `CallAnApi`: Wraps Playwright's `IAPIRequestContext` for direct REST API calls.
* **Tasks**: Reusable user interactions (e.g., `OpenGroupOrderPage`, `SubmitConsolidatedPreOrder`, `RemoveGuestParticipant`, `FetchGroupSessions`, `SetupTestGroupData`).
* **Questions**: Decoupled queries that inspect application state (e.g., `ParticipantsCount`, `ParticipantNames`, `ParticipantCardState`, `GrandTotalAmount`, `GroupOrderStatusBadge`, `ConsolidatedDishesData`, `WaiterOrderData`).
* **Semantic Locators**: Strictly prioritized by **Role + Accessible Name**, **Label**, and **Test ID** (avoiding brittle CSS/XPath hierarchies):
  ```csharp
  // Accessible role & name
  _page.GetByRole(AriaRole.Heading, new() { Name = "Group Pre-Order", Level = 1 });
  _page.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order" });
  _page.GetByRole(AriaRole.Alert);
  ```
* **Dynamic Test Data & Teardown**:
  * Every test run generates unique GUID-based identifiers (`groupId`, `reservationId`, `sessionId`) and dynamic future dates via `TestDataGenerator`.
  * `TestDataManager` registers all created groups and automatically performs clean API teardown in `[AfterScenario]` hooks to leave zero state in the environment.

---

## 📁 Project Structure

```
aqa/restaurant-aqa-framework/
├── .env.example                       # Environment template
├── .env                               # Local secrets (git-ignored)
├── .gitignore                         # Framework ignore rules
├── RestaurantAqaFramework.csproj      # .NET 10 project definition
├── Configuration/
│   └── TestConfiguration.cs           # DotNetEnv + environment config loader
├── Models/
│   └── GroupModels.cs                 # DTOs and API contract models
├── TestData/
│   ├── TestDataGenerator.cs           # Dynamic GUID, email, and date generation
│   └── TestDataManager.cs             # Automatic teardown and cleanup tracking
├── Drivers/
│   └── PlaywrightDriver.cs            # Playwright lifecycle, video, trace, screenshot manager
├── Hooks/
│   └── ScenarioHooks.cs               # SpecFlow Before/After hooks & artifact attachment
├── Pages/
│   └── GroupOrderPage.cs              # Screenplay page objects and accessible locators
├── Screenplay/
│   ├── Actors/
│   │   ├── IActor.cs
│   │   ├── Actor.cs
│   │   └── Cast.cs                    # Multi-actor management
│   ├── Abilities/
│   │   ├── BrowseTheWeb.cs
│   │   └── CallAnApi.cs
│   ├── Tasks/
│   │   ├── UiTasks.cs                 # Screenplay UI actions
│   │   └── ApiTasks.cs                # Screenplay API actions
│   └── Questions/
│       ├── UiQuestions.cs             # UI state queries
│       └── ApiQuestions.cs            # API response queries
├── Features/
│   ├── Level1_ViewParticipants.feature
│   ├── Level2_ConfirmConsolidatedPreOrder.feature
│   ├── Level3_RemoveGuest.feature
│   └── Level3_E2E_GroupPreOrderFlow.feature
└── StepDefinitions/
    ├── CommonStepDefinitions.cs
    ├── Level1StepDefinitions.cs
    ├── Level2StepDefinitions.cs
    └── Level3StepDefinitions.cs
```

---

## ⚙️ Prerequisites & Environment Setup

### 1. Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js 20+](https://nodejs.org/) (for serving the frontend SPA)
* [PowerShell](https://github.com/powershell/powershell) (`pwsh`) for Playwright browser driver provisioning

### 2. Configure `.env`
Copy `.env.example` to `.env` in `aqa/restaurant-aqa-framework/`:
```bash
cp aqa/restaurant-aqa-framework/.env.example aqa/restaurant-aqa-framework/.env
```

Ensure `.env` matches your environment:
```ini
# Application URLs
BASE_URL=http://localhost:4200
API_BASE_URL=http://localhost:5023

# Execution Settings
HEADLESS=true
BROWSER=chromium
SLOWMO=0
DEFAULT_TIMEOUT=15000

# Failure & Reporting Artifacts
RECORD_VIDEO=true
TAKE_SCREENSHOTS_ON_FAILURE=true
ENABLE_TRACING=true
```

> **Security Note**: `.env` is explicitly added to `.gitignore` and must never be committed.

---

## 🚀 Running Tests Locally

### 1. Start the Local Backend API
```bash
dotnet run --project EpamPreOrderApi/EpamPreOrderApi.csproj --urls "http://localhost:5023"
```

### 2. Start the Frontend SPA Server
```bash
node green-and-tasty/frontend/restaurant-frontend-app/scripts/serve-spa.mjs
```

### 3. Install Playwright Browsers (One-Time Setup)
```bash
pwsh aqa/restaurant-aqa-framework/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

### 4. Build and Run All Tests
```bash
# Build the test framework
dotnet build aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj

# Run all 32 tests
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj
```

---

## 🏷️ Filtering Tests by Tags & Levels

You can execute specific subsets of tests using SpecFlow tags:

### Run by Level:
```bash
# Run Level 1 tests (UI + API)
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=level1"

# Run Level 2 tests (UI + API)
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=level2"

# Run Level 3 tests (UI + API)
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=level3"

# Run complete End-to-End flow
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=e2e"
```

### Run by Target:
```bash
# Run all pure API tests
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=api"

# Run all UI browser tests
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=ui"

# Run authorization matrix tests
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=authorization"

# Run negative & boundary tests
dotnet test aqa/restaurant-aqa-framework/RestaurantAqaFramework.csproj --filter "TestCategory=negative"
```

---

## 📊 Reports & Failure Artifacts

### 1. Test Results Reports
When running with the `--logger` switch, reports are saved to `aqa/restaurant-aqa-framework/TestResults/`:
* `test-results.html`: Self-contained HTML test execution summary.
* `test-results.trx`: Standard Visual Studio / VSTest XML logger.

### 2. SpecFlow LivingDoc Report
To generate the interactive LivingDoc report:
```bash
dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
livingdoc test-execution aqa/restaurant-aqa-framework/bin/Debug/net10.0/RestaurantAqaFramework.dll \
  -t aqa/restaurant-aqa-framework/bin/Debug/net10.0/TestExecution.json \
  --output aqa/restaurant-aqa-framework/TestResults/LivingDoc.html
```

### 3. Diagnostic Artifacts on Failure
For any failed test scenario, the framework automatically records and attaches:
* **Playwright Trace**: Saved in `TestResults/traces/<ScenarioTitle>.zip`. View using [Playwright Trace Viewer](https://trace.playwright.dev):
  ```bash
  npx playwright show-trace path/to/trace.zip
  ```
* **Failure Screenshots**: Full-page snapshot saved in `TestResults/screenshots/<ScenarioTitle>_<Timestamp>.png`.
* **Execution Video**: Video recording saved in `TestResults/videos/<ScenarioTitle>/`.

---

## 🧐 Assumed Behavior for Edge Cases

To ensure unambiguous test assertions, the following behaviors have been formally specified and validated:

| Edge Case | Assumed & Verified Behavior | HTTP Status / UI Outcome |
| :--- | :--- | :--- |
| **Removing the Last Participant** | The last guest can be removed by the Owner. The group remains active with only the Owner, the participants list displays only the Owner (or empty state if Owner has no items), and Grand Total becomes `$0.00`. | `200 OK` (API) / UI list updated, Submit disabled |
| **Removing Guest with Confirmed Cart** | Allowed prior to submission. The guest's confirmed cart is discarded, and their subtotal is immediately subtracted from the consolidated Grand Total. | `200 OK` / Subtotal deducted |
| **Submitting Already Submitted Group** | Once submitted, a group cannot be submitted a second time. Attempting re-submission returns a conflict/bad request. | `400 Bad Request` with error message |
| **Submitting Without Any Confirmed Carts** | If participants have items but none have submitted their cart (`IsSubmitted = false`), the consolidated order is empty, so submission is rejected. | `400 Bad Request` |
| **Submitting Confirmed Carts with 0 Items** | If participants confirmed their cart but added 0 dishes, the order cannot be placed. | `400 Bad Request` |
| **Cross-Group Deletion Attempt** | Attempting to delete `guestSessionId` using another group's URL `DELETE /guest/groups/{otherGroupId}/sessions/{guestId}` is blocked. | `400 Bad Request` (`Session does not belong to this group reservation`) |
| **Deleting Non-Existent Session** | Deleting a non-existent `guestSessionId` returns `404`. | `404 Not Found` |
| **Non-Existent Group / Reservation** | Querying or submitting a non-existent group ID returns `404`. | `404 Not Found` / UI displays "Group reservation not found" |
| **Role Authorization** | Only the **Owner** has permission to view participants, submit the consolidated pre-order, or remove guests. | `401 Unauthorized` (no auth), `403 Forbidden` (Guest / Waiter / OtherClient) |

---

## 🔄 CI/CD Pipeline (GitHub Actions)

The workflow is configured in [`.github/workflows/playwright.yml`](.github/workflows/playwright.yml).

### Workflow Pipeline Highlights:
* **Triggers**: Runs automatically on every `push` and `pull_request` against `main` and `master`.
* **Environment Configuration**: Injects URLs and credentials via GitHub Secrets (`BASE_URL`, `API_BASE_URL`).
* **Resilient Service Lifecycle**: Starts both the backend API and frontend SPA server in the background, with automated health-check polling before running tests.
* **Non-Blocking Test Execution**: Uses `continue-on-error: true` during test execution so report generation and artifact uploads execute even if a test fails (`if: always()`).
* **Artifact Upload**: Retains all traces, failure screenshots, execution videos, and HTML reports as downloadable build artifacts for 14 days.
