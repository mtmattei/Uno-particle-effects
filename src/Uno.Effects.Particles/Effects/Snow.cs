using Microsoft.UI.Xaml;
using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Flakes falling with a horizontal sway. The one everybody reaches for first,
/// so it carries the extra knob the others do not: <see cref="Sway"/>.
/// </summary>
[Effect("Snow", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Flakes falling with a horizontal sway.",
	Tags = "ambient,seasonal,background",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Direction,Gravity")]
public sealed partial class Snow : ParticleEffect
{
	private struct Flake
	{
		public float X;
		public float Y;
		public float Radius;
		public float Phase;
		public float Speed;
	}

	private Flake[] _flakes = [];

	/// <summary>How far a flake drifts sideways as it falls.</summary>
	[EffectProperty(0, 4, Step = 0.05, Group = "Snow")]
	public double Sway
	{
		get => (double)GetValue(SwayProperty);
		set => SetValue(SwayProperty, value);
	}

	public static readonly DependencyProperty SwayProperty = DependencyProperty.Register(
		nameof(Sway), typeof(double), typeof(Snow), new PropertyMetadata(1d, OnVisualChanged));

	protected override int NaturalCount => 90;

	protected override void Emit(in EffectFrame frame)
	{
		_flakes = new Flake[ResolveCount(in frame)];

		for (var i = 0; i < _flakes.Length; i++)
		{
			_flakes[i] = new Flake
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Radius = Random.Next(1.8f) + 0.6f,
				Phase = Random.Next(Tau),
				Speed = Random.Next(0.5f) + 0.5f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var gravity = (float)Gravity;
		var sway = (float)Sway;

		for (var i = 0; i < _flakes.Length; i++)
		{
			ref var flake = ref _flakes[i];

			var vy = (10f + (flake.Radius * 12f)) * flake.Speed * gravity;
			var vx = MathF.Sin((frame.Time * 0.8f) + flake.Phase) * 8f * sway;
			ApplyDirection(ref vx, ref vy);

			flake.X += vx * frame.DeltaTime;
			flake.Y += vy * frame.DeltaTime;

			if (flake.Y > frame.Height + 3)
			{
				flake.Y = -3;
				flake.X = Random.Next(frame.Width);
			}
			else if (flake.Y < -3)
			{
				// Reversed gravity or direction still recycles cleanly.
				flake.Y = frame.Height + 3;
				flake.X = Random.Next(frame.Width);
			}

			if (flake.X < -4)
			{
				flake.X = frame.Width + 4;
			}
			else if (flake.X > frame.Width + 4)
			{
				flake.X = -4;
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var flake in _flakes)
		{
			Dot(canvas, flake.X, flake.Y, flake.Radius * size, Primary, 0.4f + (0.5f * flake.Speed));
		}
	}
}
