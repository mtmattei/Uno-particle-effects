namespace Uno.Effects.Particles;

/// <summary>
/// A one-off estimate of how much particle budget this device can afford.
/// </summary>
/// <remarks>
/// The recommendation asks that <c>Count</c> scale by a device tier unless it is
/// overridden. Every effect rasterizes through Skia on the CPU, so the tier that
/// matters is simply how much processor there is to do that with, and how much
/// of it this platform is likely to give us.
///
/// This is deliberately coarse and deliberately measured once. It is a budget,
/// not a benchmark: getting it roughly right on a phone matters, and the cost of
/// getting it slightly wrong on a desktop is a few dozen extra particles.
///
/// Note that this does <em>not</em> affect the render tests, which pin the
/// density to 1. Identical pixels across heads and a per-device particle budget
/// would otherwise contradict each other; pinning the tests means they compare
/// the effect, and the tier applies to the live view.
/// </remarks>
internal static class DeviceTier
{
	private static double? _density;

	/// <summary>The multiplier applied to every effect's natural count.</summary>
	public static double Density => _density ??= Measure();

	private static double Measure()
	{
		var density = 1.0;

		if (OperatingSystem.IsBrowser())
		{
			// WebAssembly rasterizes on one thread, and shares it with
			// everything else on the page.
			density *= 0.6;
		}
		else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
		{
			density *= 0.75;
		}

		var processors = Environment.ProcessorCount;

		if (processors <= 2)
		{
			density *= 0.6;
		}
		else if (processors <= 4)
		{
			density *= 0.8;
		}

		// Never below a third: past that the effects stop reading as themselves,
		// and a sparse effect is worse than a slightly expensive one.
		return Math.Clamp(density, 1.0 / 3.0, 1.0);
	}
}
