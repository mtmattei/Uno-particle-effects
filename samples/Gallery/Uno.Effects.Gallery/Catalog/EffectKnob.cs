using System.Globalization;
using System.Reflection;
using Uno.Effects.Particles;

namespace Uno.Effects.Gallery.Catalog;

/// <summary>
/// One editable property of an effect, described by its
/// <see cref="EffectPropertyAttribute"/>. The property panel renders these, and
/// the code generator compares against <see cref="DefaultValue"/> so only the
/// knobs actually changed are written out.
/// </summary>
public sealed class EffectKnob
{
	private readonly PropertyInfo _property;

	public EffectKnob(PropertyInfo property, EffectPropertyAttribute attribute, object? defaultValue)
	{
		_property = property;
		Name = property.Name;
		DisplayName = attribute.DisplayName ?? Humanise(property.Name);
		Minimum = attribute.Minimum;
		Maximum = attribute.Maximum;
		Group = attribute.Group;
		ZeroIsAutomatic = attribute.ZeroIsAutomatic;
		IsInteger = property.PropertyType == typeof(int);
		Step = attribute.Step > 0 ? attribute.Step : (IsInteger ? 1 : (attribute.Maximum - attribute.Minimum) / 100);
		DefaultValue = defaultValue;
	}

	public string Name { get; }

	public string DisplayName { get; }

	public double Minimum { get; }

	public double Maximum { get; }

	public double Step { get; }

	public string Group { get; }

	public bool IsInteger { get; }

	/// <summary>True when 0 means "let the effect choose".</summary>
	public bool ZeroIsAutomatic { get; }

	public object? DefaultValue { get; }

	public double Read(ParticleEffect effect) =>
		Convert.ToDouble(_property.GetValue(effect), CultureInfo.InvariantCulture);

	public void Write(ParticleEffect effect, double value) =>
		// Boxed deliberately: without the cast the conditional unifies to
		// double and SetValue rejects it for every int-typed knob.
		_property.SetValue(effect, IsInteger ? (object)(int)Math.Round(value) : value);

	/// <summary>True when the effect still holds this knob's default.</summary>
	public bool IsDefault(ParticleEffect effect) =>
		Equals(_property.GetValue(effect), DefaultValue);

	/// <summary>The current value formatted for generated XAML and C#.</summary>
	public string Format(ParticleEffect effect)
	{
		var value = Read(effect);
		return IsInteger
			? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
			: Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
	}

	/// <summary>The current value formatted for the panel.</summary>
	public string Display(ParticleEffect effect)
	{
		var value = Read(effect);
		if (ZeroIsAutomatic && value == 0)
		{
			return "auto";
		}

		return IsInteger
			? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
			: value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	private static string Humanise(string name)
	{
		var builder = new System.Text.StringBuilder(name.Length + 4);

		for (var i = 0; i < name.Length; i++)
		{
			if (i > 0 && char.IsUpper(name[i]))
			{
				builder.Append(' ').Append(char.ToLowerInvariant(name[i]));
			}
			else
			{
				builder.Append(name[i]);
			}
		}

		return builder.ToString();
	}
}
