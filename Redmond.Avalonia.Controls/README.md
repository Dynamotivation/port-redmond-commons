# Redmond Avalonia Controls

Workspace-level shared controls for Redmond Project Avalonia frontends. This
directory deliberately sits beside, rather than inside, Calculator and
Notepad. Consumers add a relative project reference and import the bundled
styles:

```xml
<ProjectReference Include="../../../Redmond.Avalonia.Controls/Redmond.Avalonia.Controls.csproj" />
```

```xml
<StyleInclude Source="avares://Redmond.Avalonia.Controls/Themes/SettingsControls.axaml" />
```

`AnimatedSettingsExpander` was copied and generalized from the Redmond
Calculator implementation in
`../calculator/src/Frontends/Calculator.Avalonia/AnimatedSettingsExpander.cs` and
its settings styles in `MainWindow.axaml`. The shared control owns the header,
animated chevron, 200ms content reveal, reversal handling, card bezel, hover
states, 72px sizing, and standard settings-content margins.

`SettingsCard` provides the matching non-expanding icon/title/description/action
row so application settings pages do not recreate that geometry.

This is intentionally a source project rather than a published package.
