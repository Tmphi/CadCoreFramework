## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
CAD001  | CadCoreFramework.Architecture | Warning | Services/ViewModels must not access Application.DocumentManager.MdiActiveDocument — use ICadContext.
