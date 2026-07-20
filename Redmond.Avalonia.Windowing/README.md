# Redmond Avalonia Windowing

Shared window materials, corners, caption controls, resize handles, and native
platform integration for Redmond Project Avalonia applications.

The initial implementation is copied and generalized from Redmond Calculator.
Calculator remains unchanged; applications consume this project through a
workspace-relative `ProjectReference`.

Windows uses Avalonia's supported Mica/Acrylic transparency fallback chain.
macOS uses the copied AppKit `NSVisualEffectView` bridge because Avalonia only
exposes transparent composition there. Unsupported combinations fall back to
safe native or transparent window behavior.
