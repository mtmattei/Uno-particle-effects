namespace Uno.Effects.Particles;

/// <summary>The six families the study sheet is organised into.</summary>
public enum EffectCategory
{
    Atmosphere,
    Light,
    Paths,
    Fields,
    Interactive,
    Procedural,
}

/// <summary>Measured frame cost band, used by the gallery's filter chips.</summary>
public enum EffectCost
{
    Light,
    Medium,
    Heavy,
}

/// <summary>
/// Catalog metadata for an effect type. The gallery's grid, filters and code
/// generation read this, and so does the Hot Design toolbox, so an effect
/// describes itself in exactly one place.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EffectAttribute : Attribute
{
    public EffectAttribute(string name, EffectCategory category)
    {
        Name = name;
        Category = category;
    }

    /// <summary>Display name, e.g. "Flow field".</summary>
    public string Name { get; }

    public EffectCategory Category { get; }

    public EffectCost Cost { get; init; } = EffectCost.Light;

    /// <summary>One line describing what the effect does.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Comma-separated free-form tags, e.g. "ambient,background".</summary>
    public string Tags { get; init; } = string.Empty;

    /// <summary>True when the effect is drawn by an SKSL runtime shader.</summary>
    public bool IsShaderBased { get; init; }

    /// <summary>True when the effect responds to the pointer.</summary>
    public bool RespondsToPointer { get; init; }

    /// <summary>
    /// Which of the common knobs this effect actually honours, comma-separated.
    /// The property panel shows these and nothing else, so a slider is never
    /// offered for a value the effect ignores.
    /// </summary>
    public string CommonKnobs { get; init; } = "Count,Speed,Size,Opacity,Seed";
}

/// <summary>
/// Declares the editable range of an effect property. The gallery's property
/// panel and Hot Design's property panel both render sliders from this, so the
/// two never drift apart.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class EffectPropertyAttribute : Attribute
{
    public EffectPropertyAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public double Minimum { get; }

    public double Maximum { get; }

    /// <summary>Slider step. 0 lets the panel pick one from the range.</summary>
    public double Step { get; init; }

    /// <summary>Label override; the property name is used when unset.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Panel section, e.g. "Motion" or "Colour".</summary>
    public string Group { get; init; } = "Effect";

    /// <summary>
    /// When true, 0 means "let the effect choose", and the panel shows the
    /// effect's own natural value until the knob is touched.
    /// </summary>
    public bool ZeroIsAutomatic { get; init; }
}
