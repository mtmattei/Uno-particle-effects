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
