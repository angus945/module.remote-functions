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

The projects use the layers that apply to their responsibility:

- `core/domain`: function names and transport-neutral error concepts.
- `core/application`: client/gateway ports, execution, typed invocation, and
  result contracts.
- `core/presentation`: the transport-neutral `RemoteFunctionClient` facade.
- `google-apps-script/infrastructure`: GAS configuration, HTTP/JSON transport,
  envelope contracts, redirect handling, and error mapping.
- `google-apps-script/presentation`: the public
  `GoogleAppsScriptClientFactory` composition root.

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

The factory overload without a custom client uses a shared `HttpClient` with a
10-second timeout. The gateway follows at most three redirects and accepts only
HTTPS targets on `script.google.com` or `script.googleusercontent.com`.
HTTP 301/302/303 redirects continue as GET, while 307/308 preserve POST and its
body.

Google can leave a stale `/macros/echo` URL in the redirect path. When a GET to
that URL returns 404, returns 5xx, or redirects back to the configured endpoint,
the gateway restarts the original POST, up to three times. This is a bounded GAS
deployment-echo recovery path, not general automatic retry: ordinary HTTP,
timeout, rate-limit, and remote-function failures are returned to the caller.

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
dotnet build module.remote-functions.sln
dotnet test module.remote-functions.sln
```

## Not Yet Supported

- Google Apps Script Execution API.
- OAuth or dynamic token providers.
- General automatic retry or retry policies.
- NuGet packaging.
- Logging, metrics and circuit breakers.
