using System.Reflection;
using System.Text.Json;
using Uno.Effects.Particles;

namespace Uno.Effects.Gallery.Catalog;

/// <summary>
/// Loads Catalog.json once and exposes the effects it lists.
/// </summary>
/// <remarks>
/// The file is an embedded resource rather than a loose asset so the same code
/// path works unchanged on every head, WebAssembly included. It is parsed with
/// <see cref="JsonDocument"/> rather than a serializer, which keeps it
/// trim-safe without a source-generated context.
/// </remarks>
public static class EffectCatalog
{
	private const string ResourceName = "Uno.Effects.Gallery.Catalog.Catalog.json";

	private static IReadOnlyList<CatalogEntry>? _entries;

	public static IReadOnlyList<CatalogEntry> Entries => _entries ??= Load();

	public static CatalogEntry? Find(string id) =>
		Entries.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.Ordinal));

	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
		"Trimming",
		"IL2072:Target parameter does not satisfy DynamicallyAccessedMembersAttribute",
		Justification = "Every effect type named by Catalog.json is preserved wholesale by LinkerConfig.xml.")]
	private static IReadOnlyList<CatalogEntry> Load()
	{
		using var stream = typeof(EffectCatalog).Assembly.GetManifestResourceStream(ResourceName)
			?? throw new InvalidOperationException($"Catalog resource '{ResourceName}' was not found.");

		using var document = JsonDocument.Parse(stream);

		var effects = document.RootElement.GetProperty("effects");
		var entries = new List<CatalogEntry>(effects.GetArrayLength());
		var number = 1;

		foreach (var element in effects.EnumerateArray())
		{
			var id = element.GetProperty("id").GetString()!;
			var typeName = element.GetProperty("type").GetString()!;

			var type = typeof(ParticleEffect).Assembly.GetType(typeName)
				?? throw new InvalidOperationException($"Catalog entry '{id}' names unknown type '{typeName}'.");

			entries.Add(new CatalogEntry(id, number++, type));
		}

		return entries;
	}
}
