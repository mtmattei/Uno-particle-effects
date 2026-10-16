# Uno.Effects media

Captured from `mtmattei/uno.particles` @ `681211d`, gallery Release build, Skia Desktop on Linux (Xvfb).

| Folder | What |
|---|---|
| `art/` | 16 finished art pieces + `00-library-poster.jpg`. Captioned with the catalog number and description. |
| `art/clean/` | Same 16 pieces without captions, for backgrounds and crops. |
| `screenshots/` | Real gallery UI at 1600×1000: board, filtered board, detail pages, light surface, SKSL tab, diagnostics. |
| `recording/` | `gallery-walkthrough.mp4` — 45 s, 1600×1000, 30 fps, real time. |
| `tweet.md` | Launch tweet drafts and which images to attach. |
| `_source/` | Everything needed to regenerate the set. |

## How the art was made

The effects were driven by their own `ParticleEffect.Render` at a fixed 1/60 s timestep,
the same loop the render tests use, but at 1280–2560 px instead of 256. Files named
`*-longexp` lighten-blend the last N frames into one image (a long exposure); `*-paper`
uses a light surface with dark ink and darken-blends. No pixels are painted by hand. The
finishing pass (`finish.py`) adds bloom on bright pixels only, a vignette, film grain and
the caption.

## Regenerate

1. Copy `_source/ArtRenderer.cs` into `samples/Gallery/Uno.Effects.Gallery/` and apply
   `_source/App.xaml.cs.capture.patch` (adds `--art <spec.json> <outDir>` and `--window WxH`).
   This patch is for capture only and is not meant for the library.
2. `dotnet build samples/Gallery/Uno.Effects.Gallery -f net10.0-desktop -c Release`
3. `Uno.Effects.Gallery --art art1.json raw/` then `--art art2.json raw/`
4. `python3 finish.py raw/ out/`
5. Recording: run the gallery with `--window 1600x1000`, start `ffmpeg -f x11grab`, run `drive.py` (xdotool).

## Caveat on numbers

The Metaballs detail screenshot's footer shows ~280 ms per frame. That is SKSL on the CPU
raster backend in a 4-core cloud VM, not a representative figure. The Constellation
diagnostics shot (57 fps, 3.6 ms) is typical for the particle effects.
