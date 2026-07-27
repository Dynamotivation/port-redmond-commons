# Redmond Avalonia Controls

Shared controls for the `port-redmond-calculator` and
`port-redmond-notepad` Avalonia frontends. Applications pin this repository as
their `redmond-commons` Git submodule, add relative project references, and
import the bundled styles:

```xml
<ProjectReference Include="../../../redmond-commons/Redmond.Avalonia.Controls/Redmond.Avalonia.Controls.csproj" />
```

```xml
<StyleInclude Source="avares://Redmond.Avalonia.Controls/Themes/SettingsControls.axaml" />
```

`AnimatedSettingsExpander` was extracted and generalized from Redmond
Calculator. Calculator and Notepad now consume this shared implementation
instead of carrying application-local copies. The shared control owns the header,
animated chevron, 200ms content reveal, reversal handling, card bezel, hover
states, 72px sizing, and standard settings-content margins.

`SettingsCard` provides the matching non-expanding icon/title/description/action
row so application settings pages do not recreate that geometry.

The project also owns the reusable, platform-neutral window UI copied and
generalized from Calculator:

- `WindowCaptionButtons` renders the Windows 10/11 minimize, maximize/restore,
  and close buttons and forwards clicks to its owning Avalonia `Window`.
- `WindowResizeHandles` supplies the invisible edge and corner hit targets used
  by custom-drawn windows.
- `WindowDragRegion` is a title-bar `Border` that invokes Avalonia's native
  window move operation from its non-interactive area. Its transparent
  background supplies a full rectangular hit-test layer. Applications place it
  beneath, rather than around, their tabs and caption buttons so empty space
  falls through to dragging while interactive chrome remains independent.
- `WindowAppearanceSettingsPanel` is the shared settings UI for material,
  decoration, and corner choices.
- `WindowAppearanceOptions` contains the shared appearance values used by both
  controls and the native windowing implementation.

These types contain no AppKit, Win32, or compositor integration. Keeping them
here lets applications reuse the same visuals without coupling their XAML to a
native-platform library.

`Assets/RedmondFluentIcons.ttf` is the copied Calculator Fluent icon font. It
provides consistent chrome glyphs such as Settings on every target platform,
including systems without Segoe Fluent Icons installed.

This is intentionally a source project rather than a published package.
See [`../SHARED-DEVELOPMENT.md`](../SHARED-DEVELOPMENT.md) for the submodule
layout and update workflow.
