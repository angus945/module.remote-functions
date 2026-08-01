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

The `core` project defines the remote-function domain model and application port.
The `google-apps-script` project implements that port for Google Apps Script Web
App endpoints.

## Build

```powershell
dotnet test module.remote-functions.sln
```
