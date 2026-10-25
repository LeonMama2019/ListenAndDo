# Responsive display

All 11 build scenes use Canvas Scaler / Scale With Screen Size, reference resolution
1000 x 600, and Screen Match Mode / Expand. The reference width includes the
existing stage menu back button and nameplate (which extend to x = -493) and the
gameplay controls. The old 800 x 600 reference did not include these controls.
StageTop previously used Constant Pixel Size, so it did not shrink on phones.

The Responsive Web template fits a 5:3 game canvas into the available page or
itch.io iframe, leaving space around it when necessary. This preserves the
existing anchored layouts without stretching them on tall or wide devices.
It responds to viewport/iframe resizing, rotation and fullscreen, uses dynamic
viewport height for mobile browser controls, and respects CSS safe-area insets.
Fullscreen includes the document so the existing Japanese name input dialog
remains visible. Unsupported fullscreen controls are hidden.

## Publish and verify

1. Pull the commit and open the project in Unity 6000.5.3f1.
2. Verify the Web build uses the `Responsive` template (selected in Player Settings).
3. Build the Web player again and replace the itch.io upload, including index.html.
4. Check Top, registration/settings, StageTop swipe to Stage6, LevelMenu, all six
   stages and their tutorials, result history and level results.
5. Check portrait/landscape, browser bars showing/hidden, rotation, fullscreen and
   the Japanese name input dialog. Portrait fits the entire landscape game into
   the available width; landscape gives larger controls.

Static scene/layout checks and template JavaScript checks (using a mock DOM)
can run without Unity. These are not browser rendering tests.
Actual Unity rendering, touch gameplay and a Web build still require Unity and
device checks; a template preview alone does not validate those.
