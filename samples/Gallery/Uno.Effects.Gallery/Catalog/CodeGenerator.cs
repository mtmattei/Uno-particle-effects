using System.Text;
using Uno.Effects.Particles;

namespace Uno.Effects.Gallery.Catalog;

/// <summary>
/// Turns the live effect on a detail page into the code a developer pastes.
/// </summary>
/// <remarks>
/// Only knobs that differ from the effect's own defaults are written, which is
/// what keeps the output as short as the snippet in the recommendation however
/// long the property panel gets.
/// </remarks>
public static class CodeGenerator
{
	public static string ToXaml(CatalogEntry entry, ParticleEffect effect)
	{
		var builder = new StringBuilder();

		builder.Append("<fx:ParticleView");
		if (entry.RespondsToPointer)
		{
			builder.Append(" Interactive=\"True\"");
		}

		builder.AppendLine(">");

		builder.Append("    <fx:").Append(entry.XamlName);

		var written = 0;
		foreach (var knob in entry.Knobs)
		{
			if (knob.IsDefault(effect))
			{
				continue;
			}

			// Wrap the attribute list the way a person would, rather than
			// running one very long line.
			builder.Append(written > 0 && written % 2 == 0
				? Environment.NewLine + new string(' ', 5 + entry.XamlName.Length)
				: " ");

			builder.Append(knob.Name).Append("=\"").Append(knob.Format(effect)).Append('"');
			written++;
		}

		builder.AppendLine(" />");
		builder.Append("</fx:ParticleView>");

		return builder.ToString();
	}

	public static string ToCSharp(CatalogEntry entry, ParticleEffect effect)
	{
		var builder = new StringBuilder();

		builder.Append("var view = new ParticleView");
		builder.AppendLine(entry.RespondsToPointer ? " { Interactive = true };" : "();");

		var changed = entry.Knobs.Where(k => !k.IsDefault(effect)).ToList();

		if (changed.Count == 0)
		{
			builder.Append("view.Effects.Add(new ").Append(entry.XamlName).Append("());");
			return builder.ToString();
		}

		builder.Append("view.Effects.Add(new ").Append(entry.XamlName).AppendLine();
		builder.AppendLine("{");

		foreach (var knob in changed)
		{
			builder.Append("    ").Append(knob.Name).Append(" = ").Append(knob.Format(effect));

			if (!knob.IsInteger)
			{
				builder.Append('d');
			}

			builder.AppendLine(",");
		}

		builder.Append("});");

		return builder.ToString();
	}

	/// <summary>The install line the detail page offers alongside the markup.</summary>
	public static string InstallCommand => "dotnet add package Uno.Effects.Particles";
}
