# port-redmond-commons

Shared source projects for the Redmond Calculator and Notepad ports.

- `Redmond.Avalonia.Controls` provides reusable Avalonia controls and themes.
- `Redmond.Avalonia.Windowing` owns window materials, decorations, and native
  platform integration.
- `Redmond.Shortcuts` provides platform-neutral shortcut registration,
  matching, formatting, and validation.

The application repositories consume Commons as a Git submodule and reference
these projects directly. No NuGet publication is required.

```sh
dotnet build Redmond.Avalonia.Controls/Redmond.Avalonia.Controls.csproj
dotnet build Redmond.Avalonia.Windowing/Redmond.Avalonia.Windowing.csproj
dotnet build Redmond.Shortcuts/Redmond.Shortcuts.csproj
```

See [SHARED-DEVELOPMENT.md](SHARED-DEVELOPMENT.md) for the application layout
and update workflow.
