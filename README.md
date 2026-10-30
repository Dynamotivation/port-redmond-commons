# port-redmond-commons

Shared source projects for the Redmond Calculator and Notepad ports.

- `Redmond.Avalonia.Controls` provides reusable Avalonia controls and themes.
- `Redmond.Avalonia.Windowing` owns window materials, decorations, and native
  platform integration.
- `Redmond.Shortcuts` provides platform-neutral shortcut registration,
  matching, localized formatting, and validation. Applications define each
  shortcut once with the `Primary` modifier; see
  [Redmond.Shortcuts/README.md](Redmond.Shortcuts/README.md).
- `Redmond.Commons.Verification` checks the shared libraries.

The application repositories consume Commons as a Git submodule and reference
these projects directly. No NuGet publication is required.

```sh
dotnet build Redmond.Avalonia.Controls/Redmond.Avalonia.Controls.csproj
dotnet build Redmond.Avalonia.Windowing/Redmond.Avalonia.Windowing.csproj
dotnet build Redmond.Shortcuts/Redmond.Shortcuts.csproj
dotnet run --project Redmond.Commons.Verification
```

See [SHARED-DEVELOPMENT.md](SHARED-DEVELOPMENT.md) for the application layout
and update workflow.

## License

Redmond Commons is available under the [MIT License](LICENSE).
The bundled Calculator Fluent icon font remains Microsoft-owned and is
documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
