; Shipped analyzer rules are tracked in AnalyzerReleases.Shipped.md.
; Add new rules to this file first; move them over when the analyzer ships.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
CAD002  | CadCoreFramework.Architecture | Warning | Services/ViewModels must not prompt the user (CadPromptHelper.*, Editor.Get*) — prompt in the command layer.
CAD003  | CadCoreFramework.Correctness | Warning | No OpenMode.ForWrite / UpgradeOpen() inside a lambda passed to ICadContext.ExecuteRead (its transaction commits).
