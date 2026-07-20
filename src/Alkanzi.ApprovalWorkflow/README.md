# Alkanzi.ApprovalWorkflow

A generic, multi-level approval workflow engine for .NET — attach an ordered chain of approval steps to **any** entity type (budgets, lease contracts, legal cases, purchase orders) without coupling the engine to your domain model.

## Requirements
Requires **.NET 8.0 or later**. Depends on [`Alkanzi.Auditable`](https://github.com/ObaiAlkanzi/alkanzi.auditable), which uses C# default interface methods — so .NET Standard 2.0 and .NET Framework are not supported.

## Install
`dotnet add package Alkanzi.ApprovalWorkflow`

## Concepts

| Type | Purpose |
| --- | --- |
| `ApprovalRequest` | The workflow instance. Points at a target entity via `EntityType` + `EntityId`, and owns an ordered `List<ApprovalStep>`. Implements `IAuditable`. |
| `ApprovalStep` | One level of the chain — `Order`, `ApproverRole`, and the outcome once actioned. |
| `ApprovalStatus` | `Pending`, `Approved`, `Rejected`, `Cancelled`. |
| `IApprovalWorkflowEngine` | The engine contract: `Approve` and `Reject`. |

The engine is deliberately stateless — it takes an `ApprovalRequest`, mutates it, and returns it. Persistence is yours.

## Usage

Build a request with an ordered chain of steps:

```csharp
using Alkanzi.ApprovalWorkflow;

var request = new ApprovalRequest
{
    EntityType = "Budget",
    EntityId   = budget.Id.ToString(),
    Steps =
    {
        new ApprovalStep { Order = 1, ApproverRole = "LineManager" },
        new ApprovalStep { Order = 2, ApproverRole = "FinanceHead" },
        new ApprovalStep { Order = 3, ApproverRole = "CEO" },
    }
};
```

`CurrentStep` always resolves to the lowest-`Order` step still `Pending`:

```csharp
IApprovalWorkflowEngine engine = new ApprovalWorkflowEngine();

engine.Approve(request, approverId: "u-101", comment: "Within Q3 allocation");
// Step 1 -> Approved. OverallStatus stays Pending (steps 2 and 3 remain).

engine.Approve(request, approverId: "u-204");
// Step 2 -> Approved. OverallStatus still Pending.

engine.Approve(request, approverId: "u-001");
// Step 3 -> Approved. All steps approved -> OverallStatus becomes Approved.
```

A rejection at **any** level halts the whole workflow immediately:

```csharp
engine.Reject(request, approverId: "u-204", comment: "Exceeds department cap");
// That step -> Rejected, and OverallStatus -> Rejected regardless of remaining steps.
```

Calling `Approve` or `Reject` when no `Pending` step remains throws `InvalidOperationException`.

## Audit stamping

`ApprovalRequest` implements `IAuditable`, so it carries `CREATED_BY` / `UPDATED_AT` / `IS_DELETED` and friends. Because those are default interface methods, stamp through the interface type:

```csharp
using Alkanzi.Auditable;

((IAuditable)request).MarkCreated(userId: 42);
```

See the [Alkanzi.Auditable README](https://github.com/ObaiAlkanzi/alkanzi.auditable) for details.

## Why entity-agnostic?
`EntityType` and `EntityId` are plain strings rather than a generic parameter or a foreign key. One approval table, one engine, and one set of screens serve every approvable entity in the system — adding a new one requires no schema or engine change.

## License
MIT — see [LICENSE](LICENSE).
