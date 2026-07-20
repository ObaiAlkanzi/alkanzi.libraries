# Alkanzi.Auditable

A lightweight, interface-based contract for audit-stamping entities (Created/Updated/Deleted metadata) — designed to work with **any** existing base class, no inheritance required.

## Requirements
Requires **.NET 8.0 or later** (uses C# default interface methods — not compatible with .NET Standard 2.0 or .NET Framework).

## Install
`dotnet add package Alkanzi.Auditable`

## Usage

Implement `IAuditable` on your entity — no base class required, works alongside any existing inheritance:

```csharp
public class Budget : IAuditable
{
    public bool IS_UPDATED { get; set; }
    public bool IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}
```

Because `MarkCreated`, `MarkUpdated`, and `MarkDeleted` are **default interface methods**, call them through the `IAuditable` type — not directly on the concrete class:

```csharp
var budget = new Budget();

((IAuditable)budget).MarkCreated(userId: 42);
```

Or, if you already have a variable typed as the interface (common in service/repository code):

```csharp
IAuditable auditable = budget;
auditable.MarkCreated(42);
auditable.MarkUpdated(7);
auditable.MarkDeleted(3);
```

## Why an interface, not a base class?
Your entities may already inherit from your own `Base.cs` or ORM base type. `IAuditable` works alongside that — just implement it, no multiple-inheritance conflicts.

## Why call through the interface?
Default interface methods are only accessible via the interface type, not the implementing class directly. This is a C# language rule (not specific to this package) — casting or declaring the variable as `IAuditable` resolves it.
