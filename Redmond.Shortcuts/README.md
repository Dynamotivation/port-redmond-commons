# Redmond Shortcuts

Platform-neutral keyboard shortcuts for the Redmond Calculator and Notepad
ports: definition, matching, conflict and convention checks, and localized
display text.

## One definition per shortcut

Applications define every shortcut once, with the same gesture on every
platform. There are no per-platform gesture tables and no platform-only keys.

Write the application-command modifier as `Primary`:

```csharp
new ShortcutDefinition("file.save-as", ShortcutGesture.Parse("Primary+Shift+S"));
new ShortcutDefinition("edit.find-next", ShortcutGesture.Parse("F3"));
```

`ShortcutPrimaryModifier.Resolve` turns `Primary` into the physical key:

| Platform | `Primary` means |
| --- | --- |
| Windows, Linux | Control |
| macOS, setting *Command* (default) | Command |
| macOS, setting *Control* | Control |

A gesture that names `Control` (or `Ctrl`) instead of `Primary` means Control
on every platform. Applications should not need it: use `Primary` for every
application command so the macOS setting and the labels apply uniformly.

## Reserved macOS shortcuts

`MacOSReservedShortcuts.All` lists the gestures macOS itself or the standard
app menu uses: ⌘H, ⌥⌘H, ⌘M, ⌘Q, ⌘Space, ⌃Space, ⌘Tab, ⌘\`, ⌥⌘Esc, ⌃⌘F, ⌃⌘Q,
⇧⌘Q, the screenshot keys, ⌃ plus an arrow (Mission Control and Spaces), and
others. `ShortcutConventionValidator` checks every resolved macOS gesture against
it, so run it for both `MacPrimaryModifier` values:

- A collision is an error (`ReservedByOperatingSystem`). Change the shortcut's
  gesture for **every** platform, keeping a mnemonic where possible (for example
  Primary+H → Primary+Shift+H). Never add a macOS-only variant.
- ⌘N, ⌘W and ⌘, are conventional commands (New, Close, Settings). They are
  reported as `CommonApplicationCollision` warnings and are fine for exactly that
  command.
- Option(+Shift) plus an arrow is macOS text navigation and is an error unless
  Command or Control is also held.

`ShortcutConventionIssue.IsCollision` is true for both collision kinds.
Applications assert in their tests that no shortcut collides.

A gesture whose key is missing from a platform's standard keyboard is left out
there (`ShortcutPrimaryModifier.IsKeyAvailable`; today: Insert on a Mac). That
is not a platform-specific binding: the remaining gestures are the same.

`ShortcutGesture.Parse` reads `Primary`, `Control`/`Ctrl`, `Alt`/`Option`,
`Shift`, `Command` and `Super`, followed by a named key (`S`, `F3`, `Delete`,
`OemPlus`, `D0`, …). `ToString()` writes the same form.

## The macOS modifier setting

`MacPrimaryModifier` (`Command` = 0, `Control` = 1) is the user's choice for
macOS. Applications persist it, default to `Command`, and show the setting only
on macOS. Pass it to the service and change it live:

```csharp
var service = ShortcutServices.CreateForCurrentPlatform(
    new ShortcutServiceOptions { MacPrimaryModifier = savedChoice });
service.GesturesChanged += (_, _) => RefreshMenuAndTooltipText();
service.MacPrimaryModifier = MacPrimaryModifier.Control; // re-resolves every shortcut
```

Registrations stay in place; matching, `GetGesture`, display text, conflicts
and convention checks all use the current choice. On Windows and Linux the
value has no effect.

## Display labels

`ShortcutService.GetGestureDisplayText`, `GetGestureDisplayTexts`,
`FormatGesture`, `FormatText` and `RewriteText` format gestures with labels for
the platform and language:

| | English | German |
| --- | --- | --- |
| Windows | `Ctrl+Shift+S`, `Delete`, `Win+E` | `Strg+Umschalt+S`, `Entf`, `Win+E` |
| Linux | `Ctrl+Shift+S`, `Super+E` | `Strg+Umschalt+S`, `Super+E` |
| macOS | `⇧⌘S`, `⌦` | `⇧⌘S`, `⌦` |

Labels come from `ShortcutLabels.json` (embedded). Each language has a `text`
table for Windows and Linux, optional `windows` and `linux` additions, and a
`macOS` table. A label falls back from the full culture (`de-CH`) to the
language (`de`) to English, so a new language only needs the entries that
differ. Add one by adding its two-letter code to the file.

The language defaults to `ShortcutLabels.DetectCulture()`: the active keyboard
layout's language on Windows, otherwise the UI culture. Set
`ShortcutServiceOptions.LabelCulture` to override it.

Applications never write gesture text such as `"Ctrl+N"` themselves. Avalonia
menus show these labels through `MenuItemShortcut.Text` in
`Redmond.Avalonia.Controls`; native macOS menu items get the resolved
`KeyGesture` and AppKit draws the key equivalent.

## Verify

```sh
dotnet run --project Redmond.Commons.Verification
```
