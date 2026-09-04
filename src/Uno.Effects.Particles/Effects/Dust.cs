using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Motes drifting on a slow noise field with a gentle upward bias. The quietest
/// thing in the set: made to sit behind content.
/// </summary>
[Effect("Dust", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Motes drifting on a slow noise field.",
	Tags = "ambient,background,subtle",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Direction,Gravity")]
public sealed partial class DustEffect : ParticleEffect
{
	private struct Mote
	{
		public float X;
		public float Y;
		public float Radius;
		public float Phase;
		public float Depth;
	}

	private Mote[] _motes = [];

	protected override int NaturalCount => 70;

	protected override void Emit(in EffectFrame frame)
	{
		_motes = new Mote[ResolveCount(in frame)];

		for (var i = 0; i < _motes.Length; i++)
		{
			_motes[i] = new Mote
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Radius = Random.Next(1.2f) + 0.4f,
				Phase = Random.Next(Tau),
				Depth = Random.NextSingle(),
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var gravity = (float)Gravity;

		for (var i = 0; i < _motes.Length; i++)
		{
			ref var mote = ref _motes[i];

			var angle = Noise.Sample(mote.X * 0.01f + frame.Time * 0.05f, mote.Y * 0.01f) * Tau * 2f;
			var speed = 6f * (0.5f + mote.Depth);
			var vx = MathF.Cos(angle) * speed;
			var vy = (MathF.Sin(angle) * speed) - (2f * gravity);
			ApplyDirection(ref vx, ref vy);

			mote.X += vx * frame.DeltaTime;
			mote.Y += vy * frame.DeltaTime;
			Wrap(ref mote.X, ref mote.Y, in frame);
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var mote in _motes)
		{
			var twinkle = 0.5f + (0.5f * MathF.Sin((frame.Time * 1.5f) + mote.Phase));
			Dot(canvas, mote.X, mote.Y, mote.Radius * size, Primary, 0.25f + (0.5f * twinkle));
		}
	}
}
