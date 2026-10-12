# dotnet new item templates

Scaffolding templates for the most common additions to a CadCoreFramework
project. Installed as a group with:

```powershell
dotnet new install .\Templates\cad-command
```

Each template is self-contained in its own folder with a `.template.config/`
manifest. Add more by copying `cad-command` as a starting point.

## cad-command

Generates a `Commands/XxxCommand.cs` file with a `CadCommandBase` subclass
and an `ExecuteWithScope` lambda — ~90% of a new command's boilerplate.

```powershell
# From the Commands/ folder in your project:
cd Commands
dotnet new cad-command -n PanelDrawCommand --AutoCadCommandName DRAW_PANEL --UndoMark "Vẽ Panel"
```

Parameters:

| Parameter             | Default        | Purpose                                     |
| :-------------------- | :------------- | :------------------------------------------ |
| `-n` / `--name`       | `XxxCommand`   | C# class name (also the file name).         |
| `--AutoCadCommandName`| `MY_COMMAND`   | AutoCAD command string (UPPER_SNAKE_CASE).  |
| `--UndoMark`          | `MyCommand`    | Undo mark label shown in AutoCAD UI.        |

After generation, the file includes an inline reminder to register the class
in `Properties/AssemblyInfo.cs` with `[assembly: CommandClass(...)]` so
AutoCAD actually finds the command on NETLOAD.

## Uninstall

```powershell
dotnet new uninstall .\Templates\cad-command
# or list and remove by identity:
dotnet new uninstall
```
