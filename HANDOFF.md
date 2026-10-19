# HANDOFF — Uno.Effects (particles library + gallery)
Updated: 2026-09-04 16:37

## Where we are

Implemented the Claude Design recommendation
(`Uno Effects Recommendation.dc.html`, project `7b2677f1-35c8-4329-ac51-392436f2222a`)
as a working Uno Platform solution. Phase 1 is done and overshot: all **64**
studies from `effects.js` are ported to C#/SkiaSharp behind a single
`ParticleView` control, with the gallery app (8×8 board, detail view, property
panel, generated XAML/C#/SKSL), a deterministic render-test harness, and a
packable `Uno.Effects.Particles` 0.1.0 NuGet carrying both heads. Since then:
a measured performance pass (worst frame 226 ms → 9.7 ms) and a fix for a
device-tier density the docs claimed but the code never implemented.

Full detail is in `README.md`; the reasoning, measurements and findings that
should go back to the recommendation are in `docs/session-log.md`.

## Last verified state

- **Build:** pass. Whole solution, both TFMs (`net10.0-desktop`,
  `net10.0-browserwasm`), **zero warnings**. Uno.Sdk pinned to stable 6.7.22.
- **Runtime:** verified by running the desktop head. Board renders all 64 tiles,
  filter chips work, tile → detail navigation works, property panel, light/dark
  surface toggle, diagnostics overlay, Copy XAML all confirmed by screenshot.
- **Tests:** `--render-tests` exits 0. All 64 effects deterministic across two
  runs at frames 0/60/300, each draws something, palette override changes
  pixels, density scales the count. Hashes are **unchanged** across the
  performance pass and the density fix.
- **Git:** `main`, clean tree, last commit `fbd964e`, pushed to
  `mtmattei/uno.particles` (private).
- **Packaging:** `dotnet pack` produces `Uno.Effects.Particles.0.1.0.nupkg` with
  `lib/net10.0-desktop` and `lib/net10.0-browserwasm`.

## Next actions (in order)

1. **Run `--render-tests` on the WebAssembly head and diff the hashes against
   desktop.** Highest-information item left. Every hash so far came from Skia
   Desktop, so the recommendation's central claim — the same pixels on every
   target — is asserted, not demonstrated. It may well fail (float determinism
   in the browser, `MathF.Sin`), which is exactly why it should run before more
   is built on the assumption.
2. **Benchmark head + `matrix.json` + the platform strip on each detail page**
   (recommendation §1 and §5). The measurement plumbing already exists in
   `RenderTestRunner`.
3. **Amend §5's CI budget in the document.** 8 ms p95 on desktop does not hold
   for Metaballs on the CPU raster backend (measured 5–18 ms at 256×256).
   State budgets per backend, or give shader effects a coarser evaluation path
   first — writing the gate as-is makes CI red on day one.
4. **Add the remaining heads** (WinAppSDK, Android, iOS, Skia Desktop on
   macOS/Linux). Currently only `browserwasm` and `desktop` are built. This also
   settles the document's open decision about the Windows head with evidence.
5. **`.github/workflows`** — there is no CI. Build both heads, run
   `--render-tests`, fail on non-determinism or a blank effect, publish
   `matrix.json`.
6. **LICENSE file.** The document lists licence as an open decision and there is
   none; a NuGet package without one is not consumable.
7. **Copy-as-source (Tier 2)** — the zero-dependency tier in §2, entirely
   unbuilt.

Lower down: compare view, URL state, `Mask`, the `?effect=…&embed=1` docs route,
and the Phase 3 tooling work (Hot Design `.Design` assembly, Playground deep
links, Figma tag mapping). The attributes a Hot Design toolbox would read are
already in place.

## Open questions

- **§5 contradicts itself and the document should resolve it.** "The same pixels
  on every target" and "scale `Count` by a device-tier heuristic" cannot both
  hold for the live view. Current resolution: the render tests pin density to 1,
  so they compare the *effect* across heads while the tier applies to the
  *view*. §5 should say which it means.
- **Shader effects allocate one `SKShader` per frame.** The "no per-frame
  allocation" rule needs an explicit exemption or a reusable builder.
- **Determinism depends on the fixed timestep, not only the seed** — Fireflies
  and Flow field draw random numbers during `Step`. Worth stating plainly in §5,
  since it is what makes the cross-head hash comparison meaningful.
- **Windows head** (recommendation's own open decision): `SKCanvasElement` needs
  the Skia renderer, so WinAppSDK parity is unresolved. Item 4 answers it.
- **Board virtualization**: all 64 tiles are realized, ~28 MB of surfaces. The
  viewport check already stops off-screen clocks; an `ItemsRepeater` is the fix
  if the catalog grows well past 64.

## Relaunch

```
cd <your clone of Uno-particle-effects>

# build everything, both heads
dotnet build

# run the gallery
dotnet run --project samples/Gallery/Uno.Effects.Gallery -f net10.0-desktop

# open straight on one effect
samples\Gallery\Uno.Effects.Gallery\bin\Debug\net10.0-desktop\Uno.Effects.Gallery.exe --effect metaballs

# the test suite (exit code is the result)
samples\Gallery\Uno.Effects.Gallery\bin\Debug\net10.0-desktop\Uno.Effects.Gallery.exe --render-tests --output <dir>
```

Notes:

- **Kill the running app before building** — it locks
  `Uno.Effects.Particles.dll` and the build fails with MSB3021.
- Launch detached (`Start-Process`), not from a shell that exits; a backgrounded
  shell child gets torn down with the shell.
- Window automation on this desktop is flaky: the window intermittently
  minimizes or returns a torn/blank `PrintWindow` capture after a
  `SetWindowPos`. Restore with `ShowWindow(h, 9)`, let it settle, then capture.
  It is the automation, not the app.
- The design source is read through the `claude_design` MCP; the project is
  `7b2677f1-35c8-4329-ac51-392436f2222a`.
