# Prompt — artsy effect captures

Paste this into a fresh session that has `mtmattei/uno.particles` checked out.

---

Make a set of gallery-quality art images from the Uno.Effects library (`mtmattei/uno.particles`). They're for social posts, a landing page and launch material, so they should read as prints and look nothing like app screenshots. Every pixel must come from the effects themselves: no painting, no AI generation, no compositing of fake content.

**How to render**

- Leave the app window alone for art. Drive the effects offscreen through the public `ParticleEffect.Render(canvas, in EffectFrame, reemit)`, at a fixed 1/60 s timestep. That's the same loop `tests/Uno.Effects.RuntimeTests/RenderTestRunner.cs` uses, and the same seed always gives the same image.
- Render at a small logical size and scale the canvas up, so particle sizes stay as designed: 640×360, 480×480 or 480×600 logical, at ×4. That gives 2560×1440, 1920×1920 or 1920×2400.
- Effects are DependencyObjects, so they need a running Uno app. Add a capture-only switch to the gallery: `--art <shots.json> <outDir>`. Keep it out of the library and don't commit it to `uno.particles`. Work in a scratch copy.
- Each shot in the JSON lists:
  - `name`, `w`, `h`, `scale`, `frames`, `surface` (hex)
  - `layers[]`, each with an effect `id` from `Catalog.json`, a `palette` (hex list) and `props` (knob values set by reflection)
  - optional `exposure` (N frames) and `blend` (`lighten` or `darken`)
- Long exposure: lighten-blend the last N frames into an accumulator on dark surfaces, and darken-blend on light ones. This is the single biggest win for path and field effects.

**Curate before rendering big**

- Run `--render-tests --output <dir>` first and build an 8×8 contact sheet of the 256 px frames. Pick from that.
- What worked best:
  - Long exposure: Attractor (600 frames, exposure 400), Flow field (420/200), Curl noise (420/240), Vortex (360/90), Dipole (360/160), Galaxy (400/120), Orbits (600/300), Fireworks (400/240), Sparkler (300/45, at 200×250 ×8 so it fills the frame).
  - Single frame: Phyllotaxis (`Count` 400, `Size` 1.6, 320×320 ×6), Constellation, Arcs, Plasma (`Cells` 96, or it looks blocky).
  - Ink on paper: Lissajous (900/700, darken), Halftone, Metaballs.
- Skip the effects that only read in motion: Rain, Dust, Grain, Static, Lightning and Fog.

**Palette** (the library's own)

| Role | Hex |
|---|---|
| Bone | `#E9E5DD` |
| Amber | `#D6A068` |
| Sage | `#96A38C` |
| Clay | `#BE7860` |
| Night surfaces | `#0F0F0E` to `#1A1918` |
| Paper | `#EEEAE2` |
| Ink | `#1F1E1C` |

Mostly bone on night, with amber as the accent. Do one or two in sage and amber, and three on paper.

**Finishing pass** (Python with Pillow and numpy)

- **Bloom, bright pixels only:** take `max(0, v − 48) × 1.4`, blur it at `max(w,h)/220` at 0.55 strength and at `max(w,h)/60` at 0.30, and screen both over the image. A plain blur lifts the blacks to grey haze, and you don't want that.
- **Vignette:** 0.45 on dark images and 0.10 on paper, falling off from 35% of the radius.
- **Grain:** monochrome Gaussian noise, sigma 4 on dark images and 5 on paper.
- **Caption, bottom-left, in Inter:**
  - Two-digit catalog number (muted), then the effect name (bone, or ink on paper), each at 3.2% of the short side.
  - The effect's `[Effect(Description = …)]` on the line below at 1.7%.
  - Bottom-right: `U N O . E F F E C T S` in Inter Medium at 1.45%.
  - Margin: 6% of the short side.
- **Scrim:** on dark full-bleed images, fade the bottom 28% towards 0.7× the median background, so the caption never sits on busy strokes.
- **Export:** JPEG at quality 95 with 4:4:4 chroma (grain makes PNGs huge). Export each piece twice, with and without the caption.
- **Poster:** a 4×4 grid poster of square centre crops (2400 wide). Put "Particles" in Inter Light at the top with a one-line subtitle, a label under each tile, and a footer with `<fx:ParticleView><fx:Snow /></fx:ParticleView>` in mono and the eyebrow on the right.

**Review loop**

After each batch, look at downscaled thumbnails and fix:
- lifted blacks
- captions colliding with the subject
- subjects too small or too sparse
- blocky shaders

Re-render only the shots that need it.

**Watch out for**

- Shader effects (Metaballs, Plasma) are slow on the CPU raster. Metaballs at 1920×1080 for 200 frames takes about 5 minutes. Run batches in the background.
- Build Release so the Debug Studio overlay doesn't show up.
- Don't publish FPS or ms numbers measured in a VM.

**Deliver**

- `media/art/`: captioned pieces and the poster.
- `media/art/clean/`
- `media/_source/`: the shot JSON, the renderer, the finishing script and a README saying how each image was made.

---
