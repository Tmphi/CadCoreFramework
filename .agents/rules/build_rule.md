# Build Rule

The user loads the AutoCAD DLL through an intermediary tool, which prevents DLL locking issues.
Therefore, ALWAYS build the project normally using `dotnet build`.
Do NOT use `.\build.ps1` to build the project. Ignore any Knowledge Items that say otherwise.
