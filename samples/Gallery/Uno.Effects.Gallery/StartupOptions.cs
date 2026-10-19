using Microsoft.UI.Xaml.Media;
using Uno.Effects.Gallery.Catalog;
using Uno.Effects.Particles;
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
				: !result.DrewSomething ? "BLANK"
				: result.Deterministic ? "ok"
				: "NONDET";

			var hashes = result.Error ?? string.Join("  ", result.Frames.Select(f => $"{f.Frame}:{f.Hash}"));

			Console.WriteLine(
				$"{result.Effect,-12} {verdict,-8} {result.BytesPerFrame,10} B {result.MedianFrameMilliseconds,9:0.000}  {hashes}");
		}

		Console.WriteLine();
		Console.WriteLine(passed
			? "All effects are deterministic. Compare these hashes across heads to prove render consistency."
			: "FAILED: an effect either did not reproduce its own frames or drew nothing at all.");

		Console.WriteLine();
		passed &= CheckPaletteOverride();

		Console.WriteLine();
		passed &= CheckDeviceTierScaling();

		Console.WriteLine();
		PrintGeneratedCode();

		return passed;
	}

	/// <summary>
	/// Proves that handing an effect a different palette actually changes the
	/// pixels it draws. This is the mechanism the light/dark surface toggle and
	/// theme-resource palettes both rely on, and it is worth an assertion
	/// rather than a look.
	/// </summary>
	private static bool CheckPaletteOverride()
	{
		var entry = EffectCatalog.Find("snow") ?? EffectCatalog.Entries[0];

		var standard = RenderTestRunner.Run($"{entry.Name} default palette", entry.Create);
		var themed = RenderTestRunner.Run($"{entry.Name} dark ink palette", () =>
		{
			var effect = entry.Create();
			effect.Palette.Add(new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x2A, 0x2A, 0x28)));
			effect.Palette.Add(new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xA8, 0x72, 0x38)));
			return effect;
		});

		var changed = standard.Frames.Count == themed.Frames.Count
			&& standard.Frames.Count > 0
			&& standard.Frames.Zip(themed.Frames).Any(pair => pair.First.Hash != pair.Second.Hash);

		Console.WriteLine(changed
			? $"Palette override changes the drawn pixels for {entry.Name}: ok"
			: $"FAILED: overriding the palette did not change what {entry.Name} draws.");

		return changed;
	}

	/// <summary>
	/// Checks that the particle budget actually tracks the density it is handed,
	/// and that an explicit Count still wins. This is the behavioural rule the
	/// recommendation asks to hold on every platform, and it is the one rule
	/// that is invisible in a screenshot.
	/// </summary>
	private static bool CheckDeviceTierScaling()
	{
		using var effect = (EffectCatalog.Find("snow") ?? EffectCatalog.Entries[0]).Create();

		var full = effect.ResolveCount(Frame(1f));
		var half = effect.ResolveCount(Frame(0.5f));
		var scales = half < full && half > 0;

		effect.Count = 120;
		var overridden = effect.ResolveCount(Frame(1f)) == 120;

		Console.WriteLine($"Device tier: this device resolves to {DeviceTierProbe():0.00}x");
		Console.WriteLine(scales && overridden
			? $"Count scales with density ({full} at 1.0, {half} at 0.5) and an explicit Count still wins: ok"
			: "FAILED: density did not scale the particle count, or an explicit Count was ignored.");

		return scales && overridden;

		static EffectFrame Frame(float density) => new(256, 256, 0f, 0f, 128f, 128f, false, density);
	}

	/// <summary>What the tier resolved to here, read through a throwaway view.</summary>
	private static double DeviceTierProbe() => new Particles.ParticleView().EffectiveDensity;

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
