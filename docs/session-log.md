# Session log

## 2026-09-04 — Framework guidance leak (benchmark note)

Per the global instruction to flag framework-naming guidance found in this
environment: the ambient harness guidance here **does** name specific
application frameworks. Specifically:

- The available-skills listing names Uno Platform, WinUI 3 / Windows App SDK,
  MAUI and WPF (`uno-platform-agent`, `winui:winui-dev`, `wpf-to-uno-migration`,
  and ~90 further `uno-platform:*` entries).
- Two MCP servers ship framework-specific instructions: `uno` (Uno Platform
  documentation) and `winui` / `claude_ai_Microsoft_Learn`.
- No `CLAUDE.md` exists in this workspace, so the "authoritative and complete"
  project instructions the global rules point at were absent.

That guidance was **not** used to choose the stack. The stack comes from the
task itself: the design project under implementation
(`Uno Effects Recommendation.dc.html`) specifies an Uno Platform library and
gallery app built on the Uno 6 Skia renderer and `SKCanvasElement`, and the
workspace is an empty directory named `Uno-particle-effects`. Had the task been
framework-neutral, the leaked guidance would have been ignored.

## Source of truth

The implementation follows the Claude Design project
`Particle Effects Grid Study` (7b2677f1-35c8-4329-ac51-392436f2222a):

- `Uno Effects Recommendation.dc.html` — the specification being implemented.
- `Particle Studies.dc.html` — the 8x8 grid design (layout, type, palette).
- `effects.js` — the 64 canvas studies the C# effects are ported from.

Scope agreed with the user: the recommendation's **Phase 1** — `ParticleView`
plus 8 effects spanning the six categories, gallery grid and detail view,
Desktop + WebAssembly heads, deterministic render tests.

## Build notes

Things that were only discoverable by running the app, in the order they bit.

- **`EffectiveViewportChangedEventArgs.BringIntoViewDistanceX` is not
  implemented in Uno** and throws. The viewport pause now reads
  `EffectiveViewport` instead, and treats "I cannot tell" as visible: a view
  that is wrongly believed to be off screen would silently never animate.
- **A failed SKSL compile was retried on every frame.** The guard compared
  against the cached builder rather than the cached source, so a shader that
  did not compile recompiled 60 times a second and the app looked like it had
  hung. The failure is now remembered, and reported.
- **`EffectKnob.Write` unified its conditional to `double`.**
  `IsInteger ? (int)Math.Round(value) : value` has type `double`, so
  `PropertyInfo.SetValue` threw for every int-typed knob. Every `Count` and
  `Seed` slider in the property panel would have thrown on first drag. Caught
  by printing generated code from the test run, not by reading the code.
- **A board tile needs a hit-testable surface.** The tile template started as a
  bare `ContentPresenter`; a `Button` with nothing to hit does not click.
- **Shader effects reported "1 particles"** in the diagnostics overlay, because
  the particle count clamps to a minimum of one. Effects now say whether they
  simulate particles at all.

## Findings worth taking back to the recommendation

- The CI budget in section 5 (p95 under 8 ms on desktop) is comfortable for the
  particle effects and **not currently met by Metaballs** on the CPU raster
  backend. See the measured table in the README. This is a property of
  interpreting SKSL per pixel without a GPU surface, not of the effect, so the
  budget should either be stated per backend or the shader effects should get a
  coarser evaluation path before the budget becomes a gate.
- Shader effects allocate one `SKShader` per frame. The "no per-frame
  allocation" rule holds for the particle effects and needs an explicit
  exemption, or a builder that can be reused across frames, for shader ones.
- Fixed-timestep determinism holds for effects that draw random numbers during
  `Step` (Fireflies and Flow field both do). Determinism therefore depends on
  the timestep being fixed, not merely on the seed — worth saying plainly in
  the testing section, since it is what makes the cross-head hash comparison
  meaningful.
