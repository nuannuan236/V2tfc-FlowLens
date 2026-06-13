# Commands

## Environment

Status: active
Updated: 2026-06-13
Confidence: high
Scope: setup

Run from `E:\Ai-\project\test_3—v2rayn` unless noted otherwise.

Install SDK, already verified on 2026-06-13:

```powershell
winget install Microsoft.DotNet.SDK.8 --source winget
```

Verify SDK:

```powershell
dotnet --list-sdks
```

Expected current result includes:

```text
8.0.422 [C:\Program Files\dotnet\sdk]
```

Build:

```powershell
dotnet build
```

Test:

```powershell
dotnet test
```

Run the WPF app:

```powershell
dotnet run --project .\V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj
```
