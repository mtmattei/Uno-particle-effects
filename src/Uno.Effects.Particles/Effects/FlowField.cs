using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Particles carried by a slowly evolving noise field, leaving trails. The
/// field itself is never drawn; the accumulated paths are the picture.
/// </summary>
[Effect("Flow field", EffectCategory.Fields,
	Cost = EffectCost.Medium,
	Description = "Particles carried by a slowly evolving noise field.",
	Tags = "generative,trails,hero",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Direction")]
public sealed partial class FlowFieldEffect : ParticleEffect
{
	private struct Tracer
	{
		public float X;
		public float Y;
	}

	private Tracer[] _tracers = [];

	protected override int NaturalCount => 140;

	/// <summary>Only a twelfth of each frame is cleared, which is what leaves the trails.</summary>
	protected internal override float TrailFade => 0.08f;

	protected override void Emit(in EffectFrame frame)
	{
		_tracers = new Tracer[ResolveCount(in frame)];

		for (var i = 0; i < _tracers.Length; i++)
		{
			_tracers[i] = new Tracer
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _tracers.Length; i++)
		{
			ref var tracer = ref _tracers[i];

			var angle = Noise.Sample(tracer.X * 0.012f, (tracer.Y * 0.012f) + (frame.Time * 0.08f)) * Tau * 1.5f;
			var vx = MathF.Cos(angle) * 30f;
			var vy = MathF.Sin(angle) * 30f;
			ApplyDirection(ref vx, ref vy);

			tracer.X += vx * frame.DeltaTime;
			tracer.Y += vy * frame.DeltaTime;

			// A trickle of random respawns keeps the field from settling into
			// a handful of attracting paths.
			if (tracer.X < 0 || tracer.X > frame.Width
				|| tracer.Y < 0 || tracer.Y > frame.Height
				|| Random.NextSingle() < 0.004f)
			{
				tracer.X = Random.Next(frame.Width);
				tracer.Y = Random.Next(frame.Height);
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = 1.2f * ResolveSize();

		foreach (var tracer in _tracers)
		{
			Rect(canvas, tracer.X, tracer.Y, size, size, Primary, 0.7f);
		}
	}
}
