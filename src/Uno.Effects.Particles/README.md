# Uno.Effects.Particles

Particle and shader effects for Uno Platform, drawn with SkiaSharp through a
single `ParticleView` control.

```xml
xmlns:fx="using:Uno.Effects.Particles"

<fx:ParticleView Interactive="True">
    <fx:Snow Count="120" Speed="0.8" />
</fx:ParticleView>
```

`ParticleView` is built on `SKCanvasElement`, so the Uno Skia renderer produces
the same pixels on Windows, WebAssembly, Android, iOS, macOS and Linux from one
code path. Effects are `DependencyObject`s: every knob is bindable, animatable
from a Storyboard or VisualState, and visible to a designer property panel.

Included: all 64 studies from the sheet, across six families — Atmosphere,
Light, Paths, Fields, Interactive and Procedural. Plasma and Metaballs are SKSL
runtime shaders.

Effects are deterministic — the same `Seed` and a fixed timestep produce the
same frames on every platform — and pause when off screen, when the view is
paused, and when the system asks for reduced motion.
