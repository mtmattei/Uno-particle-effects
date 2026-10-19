using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Uno.Effects.Particles;

namespace Uno.Effects.Gallery.Catalog;

/// <summary>
/// One effect as the gallery sees it: its catalog identity plus everything the
/// effect type declares about itself.
/// </summary>
public sealed class CatalogEntry
{
	private readonly ParticleEffect _prototype;

	public CatalogEntry(
		string id,
		int number,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type type)
	{
		Id = id;
		Number = number.ToString("00", CultureInfo.InvariantCulture);
		EffectType = type;

		Metadata = type.GetCustomAttribute<EffectAttribute>()
			?? throw new InvalidOperationException($"{type.Name} is missing [Effect].");

		// A pristine instance is the cheapest honest source of defaults: the
		// generated XAML writes only what differs from it.
		_prototype = Create();
		Knobs = DiscoverKnobs(type, Metadata, _prototype);
	}

	public string Id { get; }

	/// <summary>Two-digit index, as printed under each tile.</summary>
	public string Number { get; }

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type EffectType { get; }

	public EffectAttribute Metadata { get; }

	public IReadOnlyList<EffectKnob> Knobs { get; }

	public string Name => Metadata.Name;

	public EffectCategory Category => Metadata.Category;

	public EffectCost Cost => Metadata.Cost;

	public string Description => Metadata.Description;

	public bool IsShaderBased => Metadata.IsShaderBased;

	public bool RespondsToPointer => Metadata.RespondsToPointer;

	public string[] Tags => Metadata.Tags.Length == 0
		? []
		: Metadata.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>The XAML element name, e.g. <c>fx:Snow</c>.</summary>
	public string XamlName => EffectType.Name;

	/// <summary>The SKSL this effect runs, or null when it is not shader based.</summary>
	public string? ShaderSource => _prototype is ShaderEffect shader ? shader.EffectiveShaderSource : null;

	public ParticleEffect Create() => (ParticleEffect)Activator.CreateInstance(EffectType)!;

	private static IReadOnlyList<EffectKnob> DiscoverKnobs(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
		EffectAttribute metadata,
		ParticleEffect prototype)
	{
		var wanted = metadata.CommonKnobs
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Distinct(StringComparer.Ordinal);

		var knobs = new List<EffectKnob>();

		// Common knobs first, in the order the effect declared them, so every
		// detail page reads the same way.
		foreach (var name in wanted)
		{
			var property = typeof(ParticleEffect).GetProperty(name);
			var attribute = property?.GetCustomAttribute<EffectPropertyAttribute>();
			if (property is not null && attribute is not null)
			{
				knobs.Add(new EffectKnob(property, attribute, property.GetValue(prototype)));
			}
		}

		// Then whatever this effect adds of its own.
		foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
		{
			var attribute = property.GetCustomAttribute<EffectPropertyAttribute>();
			if (attribute is not null)
			{
				knobs.Add(new EffectKnob(property, attribute, property.GetValue(prototype)));
			}
		}

		return knobs;
	}
}
