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

## Application integration

Each application pins this repository as a Git submodule named `redmond-commons`
at its repository root:

```text
port-redmond-calculator/
  redmond-commons/
  src/

port-redmond-notepad/
  redmond-commons/
  redmond/
```

Clone an application and its Commons revision with:

```sh
git clone --recurse-submodules <application-url>
git submodule update --init --recursive
```

The application project files reference projects below `redmond-commons`
directly. A commit in each application therefore records the exact Commons
revision it was built and tested against without requiring a package feed.

## Change workflow

1. Make and commit a change in `port-redmond-commons`.
2. Push the Commons commit.
3. Update the `redmond-commons` submodule in each consuming application.
4. Run `dotnet run --project Redmond.Commons.Verification`, then build and
   test both applications.
5. Commit and push the new submodule revisions.

Avoid Git subtree copies here: they duplicate shared source into both
application histories and make every common fix a reconciliation exercise.

## License

Redmond Commons is available under the [MIT License](LICENSE).
The bundled Calculator Fluent icon font remains Microsoft-owned and is
documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
