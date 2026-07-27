# Redmond Avalonia Windowing

Shared window materials, corner application, decoration policy, and native
platform integration for Redmond Project Avalonia applications.

The initial implementation was extracted and generalized from Redmond
Calculator. Calculator and Notepad consume it through workspace-relative
`ProjectReference` entries; native window interop no longer lives in either
application.

Windows uses Avalonia's supported Mica/Acrylic transparency fallback chain.
macOS uses the copied AppKit `NSVisualEffectView` bridge because Avalonia only
exposes transparent composition there. Unsupported combinations fall back to
safe native or transparent window behavior.

Reusable caption buttons, resize handles, appearance settings UI, and the
appearance option contracts live in the sibling `Redmond.Avalonia.Controls`
project. This project references those contracts and applies them to a real
window. In particular, corner rounding belongs here because it spans Avalonia
surface geometry, client-area decoration policy, macOS native layer corner
radii, and native shadow refreshes; it is behavior rather than a standalone
visual control.

`WindowAppearanceController.Attach` accepts `enableNativeEffects: false` for
headless and snapshot hosts. Decoration and surface state still apply, while
native platform calls are suppressed.
