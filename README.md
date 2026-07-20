# Alkanzi.Libraries

Development monorepo for the Alkanzi .NET packages. Both libraries build, version, and test together here; each still ships as its own NuGet package.

## Packages

| Package | Description | NuGet |
| --- | --- | --- |
| [`Alkanzi.Auditable`](src/Alkanzi.Auditable) | Interface-based audit-stamping contract (`IAuditable`) — Created/Updated/Deleted metadata for any entity, no base class required. | [![NuGet](https://img.shields.io/nuget/v/Alkanzi.Auditable.svg)](https://www.nuget.org/packages/Alkanzi.Auditable) |
| [`Alkanzi.ApprovalWorkflow`](src/Alkanzi.ApprovalWorkflow) | Generic multi-level approval workflow engine — attach an ordered approval chain to any entity type. | [![NuGet](https://img.shields.io/nuget/v/Alkanzi.ApprovalWorkflow.svg)](https://www.nuget.org/packages/Alkanzi.ApprovalWorkflow) |

`Alkanzi.ApprovalWorkflow` depends on `Alkanzi.Auditable`. Inside this repo that link is a `ProjectReference`, so changes flow across both projects immediately — no NuGet round-trip while developing. `dotnet pack` still emits it as a proper package dependency.

## Layout

```
Alkanzi.Libraries.slnx
├─ src/
│  ├─ Alkanzi.Auditable/            # IAuditable contract
│  └─ Alkanzi.ApprovalWorkflow/     # workflow engine (-> Auditable)
└─ tests/
   └─ Alkanzi.ApprovalWorkflow.Tests/   # xUnit, covers both libraries
```

## Requirements
**.NET 8.0 or later.** The libraries multi-target `net8.0` and `net10.0`; the test project runs on `net10.0`. `IAuditable` uses C# default interface methods, so .NET Standard 2.0 and .NET Framework are not supported.

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
```

## Packing

```bash
dotnet pack -c Release -o ./artifacts
```

Produces a `.nupkg` for each library. Versions are set per-project via `<Version>` in the respective `.csproj`.

## Publishing

```bash
dotnet nuget push ./artifacts/Alkanzi.Auditable.<version>.nupkg \
  --source https://api.nuget.org/v3/index.json --api-key <key>
```

Publish `Alkanzi.Auditable` first when both have changed — `Alkanzi.ApprovalWorkflow` declares a dependency on it, and NuGet.org will reject a package whose dependency version doesn't yet exist.

## Related repositories
The original single-package repos remain as published history:
[alkanzi.auditable](https://github.com/ObaiAlkanzi/alkanzi.auditable) ·
[alkanzi.approvalworkflow](https://github.com/ObaiAlkanzi/alkanzi.approvalworkflow)

## License
MIT — see [LICENSE](LICENSE).
