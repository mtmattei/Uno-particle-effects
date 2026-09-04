namespace Uno.Effects.Particles;

/// <summary>
/// Everything an effect needs to advance and draw one frame. Passed by
/// <see langword="in"/> so stepping a frame allocates nothing.
/// </summary>
public readonly struct EffectFrame
{
    public EffectFrame(
        float width,
        float height,
        float deltaTime,
        float time,
        float pointerX,
        float pointerY,
        bool pointerInside,
        float density)
    {
        Width = width;
        Height = height;
        DeltaTime = deltaTime;
        Time = time;
        PointerX = pointerX;
        PointerY = pointerY;
        PointerInside = pointerInside;
        Density = density;
    }

    /// <summary>Surface width in logical pixels.</summary>
    public float Width { get; }

    /// <summary>Surface height in logical pixels.</summary>
    public float Height { get; }

    /// <summary>
    /// Seconds since the previous frame, already multiplied by the effect's and
    /// the view's <c>Speed</c>. Zero in a frozen frame.
    /// </summary>
    public float DeltaTime { get; }

    /// <summary>Seconds of simulated time since the effect was emitted.</summary>
    public float Time { get; }

    /// <summary>Pointer position in logical pixels, relative to the surface.</summary>
    public float PointerX { get; }

    /// <summary>Pointer position in logical pixels, relative to the surface.</summary>
    public float PointerY { get; }

    /// <summary>
    /// True when the pointer is genuinely over the surface. When false the view
    /// still supplies a drifting synthetic position so interactive effects have
    /// something to react to.
    /// </summary>
    public bool PointerInside { get; }

    /// <summary>
    /// Device-tier particle budget multiplier: 1 on a normal desktop, lower on
    /// constrained devices. Effects size their pools by this.
    /// </summary>
    public float Density { get; }
}
