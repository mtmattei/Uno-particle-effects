using Uno.Effects.Gallery.Catalog;
using Uno.Effects.RuntimeTests;

namespace Uno.Effects.Gallery;

/// <summary>
/// The two things the gallery can be asked to do besides open the board.
/// </summary>
/// <remarks>
/// Both matter to the recommendation. The deep link is the app-side half of
/// the <c>?effect=Snow</c> route the docs embed; the render tests run inside
/// the real app, which is what lets one harness cover every head rather than a
/// second, headless implementation of the same thing.
/// </remarks>
public sealed record StartupOptions(string? EffectId, bool RunRenderTests, string? OutputDirectory)
{
	public static StartupOptions Parse(IReadOnlyList<string> args)
	{
		string? effect = null;
		string? output = null;
		var renderTests = false;

		for (var i = 0; i < args.Count; i++)
		{
			switch (args[i])
			{
				case "--effect" when i + 1 < args.Count:
					effect = args[++i];
					break;
				case "--render-tests":
					renderTests = true;
					break;
				case "--output" when i + 1 < args.Count:
					output = args[++i];
					break;
			}
		}

		return new StartupOptions(effect, renderTests, output);
	}

	/// <summary>
	/// Runs the deterministic render tests over the whole catalog and prints a
	/// report. Returns true when everything passed.
	/// </summary>
	public static bool RunRenderTestSuite(string? outputDirectory)
	{
		var directory = outputDirectory
			?? Path.Combine(Path.GetTempPath(), "uno-effects-render-tests");

		var effects = EffectCatalog.Entries
			.Select(entry => (entry.Name, (Func<Particles.ParticleEffect>)entry.Create))
			.ToList();

		Console.WriteLine($"Render tests  {RenderTestRunner.Size}x{RenderTestRunner.Size}, fixed 1/60 s timestep, "
			+ $"frames {string.Join(", ", RenderTestRunner.SampledFrames)}");
		Console.WriteLine($"Frames written to {directory}");
		Console.WriteLine();
		Console.WriteLine($"{"effect",-12} {"result",-8} {"alloc/frame",12} {"median ms",10}  frame hashes");

		var passed = true;

		foreach (var result in RenderTestRunner.RunAll(effects, directory))
		{
			passed &= result.Passed;

			var verdict = result.Error is not null ? "ERROR"
				: result.Deterministic ? "ok"
				: "NONDETERMINISTIC";

			var hashes = result.Error ?? string.Join("  ", result.Frames.Select(f => $"{f.Frame}:{f.Hash}"));

			Console.WriteLine(
				$"{result.Effect,-12} {verdict,-8} {result.BytesPerFrame,10} B {result.MedianFrameMilliseconds,9:0.000}  {hashes}");
		}

		Console.WriteLine();
		Console.WriteLine(passed
			? "All effects are deterministic. Compare these hashes across heads to prove render consistency."
			: "FAILED: at least one effect did not reproduce its own frames.");

		Console.WriteLine();
		PrintGeneratedCode();

		return passed;
	}

	/// <summary>
	/// Prints what "Copy XAML" and "Copy C#" would produce for a tweaked
	/// effect. The point being checked is that only the knobs that differ from
	/// the effect's own defaults are written out.
	/// </summary>
	private static void PrintGeneratedCode()
	{
		var entry = EffectCatalog.Find("snow") ?? EffectCatalog.Entries[0];
		var effect = entry.Create();

		Console.WriteLine($"Generated code for {entry.Name}, untouched:");
		Console.WriteLine(CodeGenerator.ToXaml(entry, effect));
		Console.WriteLine();

		foreach (var knob in entry.Knobs)
		{
			if (knob.Name is "Count")
			{
				knob.Write(effect, 120);
			}
			else if (knob.Name is "Speed")
			{
				knob.Write(effect, 0.8);
			}
		}

		Console.WriteLine($"Generated code for {entry.Name} with Count and Speed changed:");
		Console.WriteLine(CodeGenerator.ToXaml(entry, effect));
		Console.WriteLine();
		Console.WriteLine(CodeGenerator.ToCSharp(entry, effect));
	}
}
