# Credit API Gateway (YARP, paso 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up a minimal ASP.NET Core (.NET 10) gateway that uses YARP to reverse-proxy every request to an existing backend at `http://localhost:5156`, configured entirely from `appsettings.json`, with no auth, plus a Scalar UI that renders the backend's proxied OpenAPI spec.

**Architecture:** Single ASP.NET Core "empty" web project (`CreditApiGateway`). `Program.cs` wires `Yarp.ReverseProxy` from `IConfiguration` and maps the proxy middleware; a single catch-all route/cluster in `appsettings.json` forwards everything to `http://localhost:5156`. `Scalar.AspNetCore` is mapped to render `/swagger/v1/swagger.json` — a path that resolves through the same catch-all proxy, so Scalar never generates its own OpenAPI document.

**Tech Stack:** .NET 10 SDK (confirmed installed: `10.0.111`), ASP.NET Core empty template (`dotnet new web`), `Yarp.ReverseProxy` 2.3.0, `Scalar.AspNetCore` 2.17.1.

**Spec:** [docs/superpowers/specs/2026-08-21-credit-api-gateway-design.md](../specs/2026-08-21-credit-api-gateway-design.md)

## Global Constraints

- Target framework: `net10.0`.
- No `UseAuthentication`/`UseAuthorization` or any security middleware in this step.
- Reverse proxy routes/clusters live only in `appsettings.json` under a `ReverseProxy` section — never hardcoded in `Program.cs`.
- Exact package versions: `Yarp.ReverseProxy` `2.3.0`, `Scalar.AspNetCore` `2.17.1`.
- Scalar must not generate its own OpenAPI document (no `AddOpenApi()`); it only points at `/swagger/v1/swagger.json`, which the catch-all proxy resolves against the backend.
- Project is added to the existing `CreditApiGateway.slnx` at the repo root.

---

## File Structure

```
CreditApiGateway.slnx                              (modified — add project)
src/CreditApiGateway/
  CreditApiGateway.csproj                          (created)
  Program.cs                                        (created)
  appsettings.json                                  (created)
  appsettings.Development.json                      (created)
  Properties/launchSettings.json                    (created)
```

No test project: the spec calls for manual/curl-based verification since there's no business logic to unit test (pure plumbing). Each task's verification step below spins up the app (and a throwaway fake backend where needed) and curls it directly.

---

### Task 1: Scaffold the empty ASP.NET Core project and wire it into the solution

**Files:**
- Create: `src/CreditApiGateway/CreditApiGateway.csproj`
- Create: `src/CreditApiGateway/Program.cs`
- Create: `src/CreditApiGateway/appsettings.json`
- Create: `src/CreditApiGateway/appsettings.Development.json`
- Create: `src/CreditApiGateway/Properties/launchSettings.json`
- Modify: `CreditApiGateway.slnx`

**Interfaces:**
- Produces: a buildable, runnable ASP.NET Core project targeting `net10.0`, registered in `CreditApiGateway.slnx`, that Task 2 will add YARP to.

- [ ] **Step 1: Scaffold the project from the empty ASP.NET Core template**

Run:
```bash
dotnet new web -n CreditApiGateway -o src/CreditApiGateway
```

This creates `CreditApiGateway.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`, and `Properties/launchSettings.json` with a default `net10.0` target and a `GET /` returning `"Hello World!"`.

- [ ] **Step 2: Add the project to the existing solution file**

Run:
```bash
dotnet sln CreditApiGateway.slnx add src/CreditApiGateway/CreditApiGateway.csproj
```

Expected: `CreditApiGateway.slnx` now contains a `<Folder Name="/src/">` entry with `<Project Path="src/CreditApiGateway/CreditApiGateway.csproj" />`.

- [ ] **Step 3: Build the solution**

Run:
```bash
dotnet build CreditApiGateway.slnx
```

Expected: `Compilación correcta. 0 Advertencia(s) 0 Errores`.

- [ ] **Step 4: Verify the app runs and serves the default route**

Run:
```bash
dotnet run --project src/CreditApiGateway --urls http://localhost:5299 > /tmp/gw_task1.log 2>&1 &
sleep 4
curl -s http://localhost:5299/
kill %1 2>/dev/null
```

Expected output from curl: `Hello World!`

- [ ] **Step 5: Commit**

```bash
git add CreditApiGateway.slnx src/CreditApiGateway
git commit -m "Scaffold empty ASP.NET Core project for CreditApiGateway"
```

---

### Task 2: Add YARP reverse proxy with a catch-all route to the backend

**Files:**
- Modify: `src/CreditApiGateway/CreditApiGateway.csproj`
- Modify: `src/CreditApiGateway/Program.cs`
- Modify: `src/CreditApiGateway/appsettings.json`

**Interfaces:**
- Consumes: the project scaffolded in Task 1.
- Produces: a running catch-all reverse proxy from the gateway to `http://localhost:5156`, which Task 3 relies on for `/swagger/v1/swagger.json` to resolve through the same app.

- [ ] **Step 1: Add the YARP package**

Run:
```bash
dotnet add src/CreditApiGateway package Yarp.ReverseProxy --version 2.3.0
```

Expected: `src/CreditApiGateway/CreditApiGateway.csproj` gains:
```xml
<ItemGroup>
  <PackageReference Include="Yarp.ReverseProxy" Version="2.3.0" />
</ItemGroup>
```

- [ ] **Step 2: Replace `Program.cs` to wire YARP from configuration**

Replace the full contents of `src/CreditApiGateway/Program.cs` with:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();
```

- [ ] **Step 3: Add the catch-all route and cluster to `appsettings.json`**

Replace the full contents of `src/CreditApiGateway/appsettings.json` with:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "catch-all": {
        "ClusterId": "backend",
        "Match": {
          "Path": "/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "backend": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5156"
          }
        }
      }
    }
  }
}
```

- [ ] **Step 4: Build**

Run:
```bash
dotnet build CreditApiGateway.slnx
```

Expected: `Compilación correcta. 0 Advertencia(s) 0 Errores`.

- [ ] **Step 5: Verify the catch-all proxy forwards to the backend**

Stand up a throwaway fake backend on port 5156 so this check doesn't depend on any real service being up, then run the gateway and curl through it:

```bash
mkdir -p /tmp/fake_backend/swagger/v1
echo '{"openapi":"3.0.1","info":{"title":"fake"}}' > /tmp/fake_backend/swagger/v1/swagger.json
python3 -m http.server 5156 --directory /tmp/fake_backend > /tmp/fake_backend.log 2>&1 &
sleep 1

dotnet run --project src/CreditApiGateway --urls http://localhost:5299 > /tmp/gw_task2.log 2>&1 &
sleep 4

curl -s http://localhost:5299/swagger/v1/swagger.json

kill %1 %2 2>/dev/null
```

Expected output from curl: `{"openapi":"3.0.1","info":{"title":"fake"}}` (i.e. the request made it through the gateway's catch-all route to the fake backend on 5156 and the response came back unchanged).

- [ ] **Step 6: Commit**

```bash
git add src/CreditApiGateway
git commit -m "Add YARP catch-all reverse proxy to backend on localhost:5156"
```

---

### Task 3: Add Scalar API reference UI over the proxied OpenAPI spec

**Files:**
- Modify: `src/CreditApiGateway/CreditApiGateway.csproj`
- Modify: `src/CreditApiGateway/Program.cs`

**Interfaces:**
- Consumes: the catch-all proxy from Task 2 (specifically that `/swagger/v1/swagger.json` resolves through it to the backend).
- Produces: a Scalar UI at the framework default route (`/scalar/v1`) that renders that proxied spec — the final deliverable of this plan.

- [ ] **Step 1: Add the Scalar package**

Run:
```bash
dotnet add src/CreditApiGateway package Scalar.AspNetCore --version 2.17.1
```

Expected: `src/CreditApiGateway/CreditApiGateway.csproj` gains a second `PackageReference`:
```xml
<PackageReference Include="Scalar.AspNetCore" Version="2.17.1" />
```

- [ ] **Step 2: Map the Scalar UI in `Program.cs`**

Replace the full contents of `src/CreditApiGateway/Program.cs` with:
```csharp
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.MapScalarApiReference(options =>
{
    options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
    options.Title = "Credit API Gateway";
});

app.Run();
```

- [ ] **Step 3: Build**

Run:
```bash
dotnet build CreditApiGateway.slnx
```

Expected: `Compilación correcta. 0 Advertencia(s) 0 Errores`.

- [ ] **Step 4: Verify the Scalar UI is served**

Reuse the same fake backend from Task 2 (recreate it if it was torn down):

```bash
mkdir -p /tmp/fake_backend/swagger/v1
echo '{"openapi":"3.0.1","info":{"title":"fake"}}' > /tmp/fake_backend/swagger/v1/swagger.json
python3 -m http.server 5156 --directory /tmp/fake_backend > /tmp/fake_backend.log 2>&1 &
sleep 1

dotnet run --project src/CreditApiGateway --urls http://localhost:5299 > /tmp/gw_task3.log 2>&1 &
sleep 4

curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5299/scalar/v1
curl -s http://localhost:5299/swagger/v1/swagger.json

kill %1 %2 2>/dev/null
```

Expected: first curl prints `200`; second curl still prints `{"openapi":"3.0.1","info":{"title":"fake"}}`, confirming the proxy route Scalar depends on still resolves correctly.

- [ ] **Step 5: Commit**

```bash
git add src/CreditApiGateway
git commit -m "Add Scalar UI over the proxied OpenAPI spec"
```

---

## Post-plan manual check (against the real backend)

Once all three tasks are committed, do one manual pass against the real backend instead of the fake one, per the spec's testing section:

1. Confirm the real backend is listening on `5156` (`curl http://localhost:5156/swagger/v1/swagger.json`).
2. `dotnet run --project src/CreditApiGateway`.
3. `curl http://localhost:<gateway-port>/swagger/v1/swagger.json` → should return the real backend's spec.
4. Open `http://localhost:<gateway-port>/scalar/v1` in a browser → should render that spec.
