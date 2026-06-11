# Deterministic, namespace-grouped output

Date: 2026-06-11
Branch: feat/nullable-reference-types

## Problem

Generated TypeScript output is non-deterministic across runtimes/OSes because two
ordering sources are unspecified by the CLR:

- `Type.GetMembers()` returns members in no guaranteed order (per-member ordering
  inside an interface).
- The `_typeStack` pop order determines the order interfaces and enums are emitted.

We want output to be a pure function of the input types, with namespaced types grouped
together.

## Goals

1. Member order within each interface is deterministic.
2. Interfaces are grouped by namespace, then ordered; enums likewise.
3. Within a namespace group, base types precede derived types; otherwise alphabetical.

## Non-goals

- No change to how individual properties/types are *formatted*.
- No change to the discovery traversal itself (still uses `_typeStack`).
- No unrelated refactoring.

## Design

### 1. Member ordering

In `RenderType`, sort members before the emit loop:

```csharp
.OrderBy(m => m.Name, StringComparer.Ordinal)
```

Ordinal (not culture-sensitive) so the sort cannot become locale-dependent.

### 2. Type & enum ordering — buffer then concatenate

Keep the `_typeStack` walk for *discovery* (it is how referenced and base types are
found). Change *when* we concatenate:

- Render each type into its own buffer and collect a record:
  `{ Namespace, Name, Type, BaseType, RenderedText }`.
  - `BaseType` is normalized to the generic type definition (matching existing
    `RenderType` logic) and is `null` when there is no exported base.
- Render each enum into its own buffer; collect `{ Namespace, Name, RenderedText }`.
- After the discovery loop, order the records and concatenate into `typeBuilder` /
  `enumBuilder`.
- Module wrapping and the trailing newline happen after concatenation, unchanged.

### 3. Ordering rule

Types:

- Primary key: namespace, `StringComparer.Ordinal` (null namespace treated as `""`).
- Within a namespace group: **base-before-derived, alphabetical otherwise.**
  - Kahn topological sort over base→derived edges where *both* ends are in the same
    group. Ties broken by choosing the alphabetically smallest (ordinal) ready node.
  - A base in a different namespace imposes no constraint; it sorts under its own
    namespace.

Enums:

- Order by `(Namespace, Name)` ordinal. No inheritance among enums.

## Output change to flag

Today base classes render *after* derived ones (base pushed last). After this change a
base renders *before* its derived type within the namespace. This is a deliberate,
visible change that will appear in re-approved snapshots.

## Testing

- These are Assent approval tests (`.approved.txt`). Member and type order will change,
  so approved files must be re-reviewed and re-approved.
- Add a focused regression test asserting ordering directly: types across two
  namespaces + a base/derived pair + multiple members, verifying namespace grouping,
  base-before-derived, and alphabetical member order independently of the snapshots.
