# module.remote-functions

Reusable .NET remote-function client modules.

## Structure

```text
module.remote-functions/
├── core/
├── core.test/
├── google-apps-script/
├── google-apps-script.test/
└── module.remote-functions.sln
```

## Dependency Direction

```text
google-apps-script
        ↓
       core
```

The `core` project defines typed remote-function contracts and execution
results. The `google-apps-script` project implements the `core` gateway port for
Google Apps Script Web App endpoints.

## Layers

Each production module uses:

- `domain`: stable value objects and error concepts.
- `application`: input/output ports, use-case orchestration, typed invocation
  and result contracts.
- `infrastructure`: transport details such as HTTP, JSON, configuration and GAS
  envelope mapping.
- `presentation`: composition roots and public factories.

Dependency direction is one-way: `google-apps-script` depends on `core`; `core`
does not reference GAS-specific types.

## Target Framework

All projects currently target `net10.0`. This is intentional while all known
hosts use .NET 10. Multi-targeting should be added only when a concrete older
host requires it.

## Usage

```csharp
using RemoteFunctions.Core.Application;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Configuration;
using RemoteFunctions.GoogleAppsScript.Presentation;

var options = new GoogleAppsScriptOptions(
    "https://script.google.com/macros/s/deployment/exec",
    "shared-access-token",
    "DesktopApp");

IRemoteFunctionClient client = GoogleAppsScriptClientFactory.Create(options);

var result = await client.InvokeAsync<LoadPlayerRequest, LoadPlayerResponse>(
    "loadPlayer",
    new LoadPlayerRequest("player-001"));
```

Zero-argument functions are supported:

```csharp
var health = await client.InvokeAsync<HealthResponse>("health");
```

When injecting a custom `HttpClient`, its handler must set
`AllowAutoRedirect = false`. Redirects are validated by this module so shared
access tokens are not forwarded to untrusted hosts.

## Google Apps Script Contract

Request envelope:

```json
{
  "function": "loadPlayer",
  "payload": {
    "playerId": "player-001"
  },
  "source": "DesktopApp",
  "token": "shared-access-token"
}
```

Success response:

```json
{
  "success": true,
  "data": {
    "playerId": "player-001",
    "level": 12
  },
  "error": null
}
```

Failure response:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "PLAYER_NOT_FOUND",
    "message": "Player does not exist.",
    "retryable": false
  }
}
```

Business fields such as `recordId` or `duplicate` belong in caller-owned DTOs,
not in the shared GAS adapter.

## Token Model

The token sent by browser, desktop or mobile clients is a shared access token.
It is not a secure client secret and can be extracted from client builds.

Do not put service account private keys, administrator tokens or high-privilege
long-lived credentials in a client application. Sensitive operations should use
user login, short-lived tokens and server-side authorization.

## Build

```powershell
dotnet test module.remote-functions.sln
```

## Not Yet Supported

- Google Apps Script Execution API.
- OAuth or dynamic token providers.
- Automatic retry.
- NuGet packaging.
- Logging, metrics and circuit breakers.
