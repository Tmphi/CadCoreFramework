# Code Snippets

Snippets save ~80% of boilerplate for the most common patterns in this
framework. Four snippets ship: `cadcmd`, `cadsvc`, `cadscope`, `cadvm`.

The VS Code equivalents live in `.vscode/cadcoreframework.code-snippets`
and work automatically when the workspace is open — no setup needed.

## Visual Studio installation

VS reads snippets from a per-user folder, not from the project. Install once:

```powershell
# PowerShell at repo root
$dest = "$env:USERPROFILE\Documents\Visual Studio 2022\Code Snippets\Visual C#\My Code Snippets"
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Copy-Item -Path .\snippets\*.snippet -Destination $dest -Force
```

Snippets become available immediately — no VS restart required. Trigger each
by typing its shortcut followed by `Tab Tab`:

| Shortcut   | What it inserts |
| :--------- | :-------------- |
| `cadcmd`   | `[CommandMethod]` method with `ExecuteWithScope` |
| `cadsvc`   | Service interface + implementation with DI |
| `cadscope` | Inline `ExecuteWithScope` block |
| `cadvm`    | ViewModel on `ViewModelBase` with `[RelayCommand]` |

## VS Code

Nothing to install. `.vscode/cadcoreframework.code-snippets` is picked up
automatically when the workspace loads. Trigger by typing the shortcut
and selecting from IntelliSense (or `Ctrl+Space`).

VS Code snippets ship with four extras not in the VS pack:
`cadasync` (two-phase async), `cadfilter` (SelectionFilterBuilder),
`caddraw` (common draw one-liners), `cadguard` (preconditions).
