# Uno.Effects

Particle and shader effects for Uno Platform, drawn with SkiaSharp through a
single `ParticleView` control, plus the gallery app that shows them off, lets
you tune them, hands you the code, and tests them.

This is **Phase 1** of the recommendation in
[`Uno Effects Recommendation.dc.html`](https://claude.ai/design/p/7b2677f1-35c8-4329-ac51-392436f2222a?file=Uno+Effects+Recommendation.dc.html):
`ParticleView` plus eight effects spanning all six categories, the board and the
detail view, Desktop and WebAssembly heads, and deterministic render tests.

```
src/Uno.Effects.Particles/     the NuGet: ParticleView, ParticleEffect, eight effects, .sksl resources
samples/Gallery/               one Uno app; heads: Skia Desktop and WebAssembly
  Catalog/Catalog.json         which effects the gallery shows, and in what order
tests/Uno.Effects.RuntimeTests/ the deterministic render harness, run by the gallery itself
docs/session-log.md            build notes and the decisions taken along the way
```

## Using an effect

```xml
xmlns:fx="using:Uno.Effects.Particles"

<fx:ParticleView Interactive="True">
    <fx:Snow Count="120" Speed="0.8" />
</fx:ParticleView>
```

`ParticleView` is the only control. Effects are its content, so a view is one
element with its layers as children. Effects are `DependencyObject`s, so every
knob is bindable, animatable from a Storyboard or VisualState, and visible to a
designer property panel.

The eight effects, one per category:

| Effect       | Category    | Cost   | Notes                                     |
|--------------|-------------|--------|-------------------------------------------|
| `Dust`       | Atmosphere  | light  | motes on a slow noise field               |
| `Snow`       | Atmosphere  | light  | adds `Sway`                               |
| `Fireflies`  | Light       | medium | adds `BlinkRate`; draws in the accent      |
| `Starfield`  | Paths       | light  | streaks from last frame to this one        |
| `FlowField`  | Fields      | medium | trails, `TrailFade` 0.08                   |
| `Follow`     | Interactive | light  | adds `Attraction`; chases the pointer      |
| `Metaballs`  | Procedural  | heavy  | SKSL shader; adds `Threshold`              |
| `Plasma`     | Procedural  | medium | SKSL shader; adds `Cells`                  |

Every effect also carries the common knobs it honours — `Count`, `Speed`,
`Size`, `Opacity`, `Seed`, and where they mean something, `Direction` and
`Gravity`. `Count` and `Size` treat 0 as "the value this effect was designed
around", so the defaults are the tuned ones and a written-out value is always a
deliberate one.

`Palette` is a list of brushes rather than colours, so an effect can be handed
theme resources and will follow a light/dark switch without any code. Index 0 is
the primary ink, index 1 the accent.

## The gallery

```
dotnet run --project samples/Gallery/Uno.Effects.Gallery -f net10.0-desktop
```

- **Board** — every catalog effect drawn by the real `ParticleView`. Idle tiles
  hold a settled frame and cost nothing; hovering starts the clock. Chips filter
  by category, by shader-based, and by measured cost.
- **Detail** — full-size preview, a property panel built from each effect's
  declared ranges, a light/dark surface toggle that flips the palette, the
  diagnostics overlay, and the generated XAML, C# and SKSL.
- **Copy** — the generated markup writes only the knobs that differ from the
  effect's own defaults, so it stays as short as the snippet above however long
  the panel gets.

Two switches, both used by the tests and the docs embed:

```
--effect <id>                  open straight on that effect's detail page
--render-tests [--output DIR]  run the deterministic render tests and exit
```

## Testing

`--render-tests` drives every effect at a fixed 1/60 s timestep onto a 256x256
surface and hashes frames 0, 60 and 300, twice, comparing the runs. It also
reports steady-state allocation per frame and median frame time, and writes the
frames as PNGs.

The harness lives in the app rather than beside it. That is deliberate: effects
are `DependencyObject`s, so they need a running Uno app, and running the tests
*inside* the gallery means one harness covers every head instead of a second,
headless implementation of the same thing. Comparing the printed hashes between
heads is the render-consistency test.

Measured on Skia Desktop (Windows, CPU raster), 256x256:

| Effect      | alloc/frame | median ms |
|-------------|-------------|-----------|
| Dust        | 43 B        | 0.12      |
| Snow        | 15 B        | 0.13      |
| Fireflies   | 43 B        | 0.05      |
| Starfield   | 43 B        | 0.11      |
| Flow field  | 104 B       | 0.18      |
| Follow      | 131 B       | 0.12      |
| Metaballs   | 10.7 KB     | 5.4–14.3  |
| Plasma      | 10.4 KB     | 2.1–3.4   |

The particle effects hold the recommendation's budget with room to spare and
allocate essentially nothing per frame. **The shader effects do not**, and the
reason is worth knowing before the CI budgets in Phase 2 are written: on the CPU
raster backend every SKSL instruction is interpreted per pixel, and each frame
allocates one `SKShader`. Metaballs in particular sits at or above the
recommendation's 8 ms desktop budget at 256x256. Adding a `break` once the
active blob count is passed cut it roughly threefold; the rest is the backend.
A GPU-backed surface, or evaluating the field at a coarser resolution, is the
Phase 2 fix. Until then Metaballs is honestly labelled heavy.

## Decisions and deviations from the recommendation

- **Catalog.json lists effects; attributes describe them.** The recommendation
  puts name, category, tags, cost and knobs in the catalog file. Those all live
  on the effect type instead, in `[Effect]` and `[EffectProperty]`, because the
  designer toolbox has to read the same metadata and two copies would drift.
  Catalog.json keeps what it alone knows: which effects the gallery shows and in
  what order. Adding effect nine is still one C# class plus one catalog entry.
- **`ParticleView.Surface`, not `Background`.** `FrameworkElement.Background`
  already exists in Uno, and shadowing a base dependency property is a good way
  to get a binding that silently targets the wrong one.
- **Composition knobs.** `Blur`, `BlendMode` and `Paused` are implemented.
  `Mask` is not; it needs a brush-to-shader conversion that earns its place
  later rather than in Phase 1.
- **Windows head.** Not enabled. `SKCanvasElement` needs the Skia renderer, so
  the WinAppSDK head is exactly the open question the recommendation raises, and
  Phase 1 does not need to answer it.
- **Hot Design metadata** (the `.Design` assembly) is Phase 3. The attributes it
  would read are already in place.
