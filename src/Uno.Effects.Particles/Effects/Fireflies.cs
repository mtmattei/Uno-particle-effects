using Microsoft.UI.Xaml;
using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Wandering lights that blink on a cubed sine, so most of the time most of
/// them are dark. Drawn in the accent colour.
/// </summary>
[Effect("Fireflies", EffectCategory.Light,
	Cost = EffectCost.Medium,
	Description = "Wandering lights that blink in and out.",
	Tags = "ambient,glow,accent",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Fireflies : ParticleEffect
{
	private struct Fly
	{
		public float X;
		public float Y;
		public float Heading;
		public float Phase;
		public float Frequency;
	}

	private Fly[] _flies = [];

	/// <summary>How quickly each light blinks.</summary>
	[EffectProperty(0.1, 4, Step = 0.05, Group = "Fireflies")]
	public double BlinkRate
	{
		get => (double)GetValue(BlinkRateProperty);
		set => SetValue(BlinkRateProperty, value);
	}

	public static readonly DependencyProperty BlinkRateProperty = DependencyProperty.Register(
		nameof(BlinkRate), typeof(double), typeof(Fireflies), new PropertyMetadata(1d, OnVisualChanged));

	protected override int NaturalCount => 22;

	protected override void Emit(in EffectFrame frame)
	{
		_flies = new Fly[ResolveCount(in frame)];

		for (var i = 0; i < _flies.Length; i++)
		{
			_flies[i] = new Fly
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Heading = Random.Next(Tau),
				Phase = Random.Next(Tau),
				Frequency = Random.Next(0.6f) + 0.5f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _flies.Length; i++)
		{
			ref var fly = ref _flies[i];

			fly.Heading += (Random.NextSingle() - 0.5f) * 12f * frame.DeltaTime;
			fly.X += MathF.Cos(fly.Heading) * 14f * frame.DeltaTime;
			fly.Y += MathF.Sin(fly.Heading) * 14f * frame.DeltaTime;
			Wrap(ref fly.X, ref fly.Y, in frame);
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();
		var rate = (float)BlinkRate;

		foreach (var fly in _flies)
		{
			var blink = MathF.Max(0f, MathF.Sin((frame.Time * fly.Frequency * 2f * rate) + fly.Phase));
			var brightness = blink * blink * blink;

			if (brightness > 0.02f)
			{
				Glow(canvas, fly.X, fly.Y, 1.4f * size, Accent, brightness);
			}
		}
	}
}
