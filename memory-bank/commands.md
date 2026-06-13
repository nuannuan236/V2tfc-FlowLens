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
dotnet build .\V2rayN.FlowLens.sln
```

Test:

```powershell
dotnet test .\V2rayN.FlowLens.sln
```

Run the WPF app:

```powershell
dotnet run --project .\V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj
```

For V1.1 ETW traffic statistics, run the WPF app from an elevated terminal. Without administrator privileges, ETW byte accounting is unavailable but log parsing and connection attribution should still run.

Initialize and verify Git repository:

```powershell
git init
git status --short --branch
```

Current initial commit:

```text
2f6542f Initial FlowLens MVP
```

Current V1.1 baseline commit:

```text
846a371 Add V1.1 attribution stability and log discovery fixes
```

Current V1.2 commit message:

```text
Add V1.2 diagnostics and settings persistence
```

V1.2 settings file path:

```text
%LocalAppData%\V2rayN.FlowLens\settings.json
```
