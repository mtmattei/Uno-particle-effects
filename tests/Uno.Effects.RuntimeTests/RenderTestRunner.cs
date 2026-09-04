using System.Diagnostics;
using System.Security.Cryptography;
using SkiaSharp;
using Uno.Effects.Particles;

namespace Uno.Effects.RuntimeTests;

/// <summary>The hash of one sampled frame.</summary>
public sealed record FrameHash(int Frame, string Hash);

/// <summary>What one effect did under the harness.</summary>
public sealed record RenderTestResult(
	string Effect,
	bool Deterministic,
	IReadOnlyList<FrameHash> Frames,
	long BytesPerFrame,
	double MedianFrameMilliseconds,
	string? Error)
{
	public bool Passed => Error is null && Deterministic;
}

/// <summary>
/// Renders each effect at a fixed timestep and hashes the sampled frames.
/// </summary>
/// <remarks>
/// This is the foundation the rest of the test story rests on. Because an
/// effect is driven only by its <see cref="ParticleEffect.Seed"/> and by the
/// deltas it is handed, the same seed and the same fixed timestep must produce
/// byte-identical frames — first between two runs in one process, and then
/// between heads, by comparing the hashes this produces on each.
///
/// Nothing here touches the visual tree, so the harness runs anywhere the
/// effects assembly runs.
/// </remarks>
public static class RenderTestRunner
{
	/// <summary>The frames compared, matching the recommendation.</summary>
	public static readonly int[] SampledFrames = [0, 60, 300];

	public const int Size = 256;

	private const float FixedDelta = 1f / 60f;

	/// <summary>The surface colour frames are rendered onto.</summary>
	private static readonly SKColor Surface = new(0x2A, 0x2A, 0x28);

	/// <summary>Runs every effect and returns one result each.</summary>
	public static IReadOnlyList<RenderTestResult> RunAll(
		IReadOnlyList<(string Name, Func<ParticleEffect> Factory)> effects,
		string? outputDirectory = null)
	{
		var results = new List<RenderTestResult>(effects.Count);

		foreach (var (name, factory) in effects)
		{
			results.Add(Run(name, factory, outputDirectory));
		}

		return results;
	}

	/// <summary>Runs one effect twice and compares the two runs.</summary>
	public static RenderTestResult Run(string name, Func<ParticleEffect> factory, string? outputDirectory = null)
	{
		try
		{
			var first = Render(factory(), outputDirectory, name);
			var second = Render(factory(), null, null);

			var deterministic = first.Hashes.Count == second.Hashes.Count;
			for (var i = 0; deterministic && i < first.Hashes.Count; i++)
			{
				deterministic = first.Hashes[i].Hash == second.Hashes[i].Hash;
			}

			return new RenderTestResult(
				name, deterministic, first.Hashes, first.BytesPerFrame, first.MedianMilliseconds, null);
		}
		catch (Exception ex)
		{
			return new RenderTestResult(name, false, [], 0, 0, ex.Message);
		}
	}

	private static (List<FrameHash> Hashes, long BytesPerFrame, double MedianMilliseconds) Render(
		ParticleEffect effect,
		string? outputDirectory,
		string? name)
	{
		var last = SampledFrames.Max();
		var hashes = new List<FrameHash>(SampledFrames.Length);
		var timings = new List<double>(last + 1);

		using var surface = SKSurface.Create(
			new SKImageInfo(Size, Size, SKColorType.Rgba8888, SKAlphaType.Premul));

		var canvas = surface.Canvas;
		canvas.Clear(Surface);

		long allocatedAtStart = 0;
		var time = 0f;

		for (var frame = 0; frame <= last; frame++)
		{
			// Frame 0 is the emitted state before any time has passed.
			var delta = frame == 0 ? 0f : FixedDelta;
			time += delta;

			// The same synthetic pointer the view supplies when nothing is
			// hovering, so interactive effects are exercised too.
			var pointerX = Size * (0.5f + (0.3f * MathF.Sin(time * 0.6f)));
			var pointerY = Size * (0.5f + (0.3f * MathF.Cos(time * 0.45f)));

			var context = new EffectFrame(Size, Size, delta, time, pointerX, pointerY, false, 1f);

			var start = Stopwatch.GetTimestamp();

			Fade(canvas, effect.TrailFade);
			effect.Render(canvas, in context, reemit: false);

			timings.Add((Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency);

			// Measured after the pools exist, so it reports steady-state churn
			// rather than the one-off cost of emitting.
			if (frame == 1)
			{
				allocatedAtStart = GC.GetAllocatedBytesForCurrentThread();
			}

			if (Array.IndexOf(SampledFrames, frame) >= 0)
			{
				hashes.Add(new FrameHash(frame, Hash(surface)));
				Save(surface, outputDirectory, name, frame);
			}
		}

		var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedAtStart;
		timings.Sort();

		return (hashes, allocated / Math.Max(1, last), timings[timings.Count / 2]);
	}

	private static void Fade(SKCanvas canvas, float trailFade)
	{
		if (trailFade >= 1f)
		{
			canvas.Clear(Surface);
			return;
		}

		using var paint = new SKPaint
		{
			IsAntialias = false,
			Color = Surface.WithAlpha((byte)(trailFade * 255)),
		};

		canvas.DrawRect(0, 0, Size, Size, paint);
	}

	private static string Hash(SKSurface surface)
	{
		using var pixmap = surface.PeekPixels();
		return Convert.ToHexString(SHA256.HashData(pixmap.GetPixelSpan()))[..16];
	}

	private static void Save(SKSurface surface, string? directory, string? name, int frame)
	{
		if (directory is null || name is null)
		{
			return;
		}

		Directory.CreateDirectory(directory);

		using var image = surface.Snapshot();
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		using var file = File.Create(Path.Combine(directory, $"{Slug(name)}-{frame:000}.png"));
		data.SaveTo(file);
	}

	private static string Slug(string name) =>
		string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-'));
}
