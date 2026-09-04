using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Stars streaming past the viewer. Each star is drawn as the streak between
/// where it was last frame and where it is now, so speed reads as length.
/// </summary>
[Effect("Starfield", EffectCategory.Paths,
	Cost = EffectCost.Light,
	Description = "Stars streaming past the viewer.",
	Tags = "motion,space,hero",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Starfield : ParticleEffect
{
	private struct Star
	{
		public float X;
		public float Y;
		public float Z;
		public float PreviousZ;
	}

	private Star[] _stars = [];

	protected override int NaturalCount => 120;

	protected override void Emit(in EffectFrame frame)
	{
		_stars = new Star[ResolveCount(in frame)];

		for (var i = 0; i < _stars.Length; i++)
		{
			var z = Random.NextSingle();
			_stars[i] = new Star
			{
				X = (Random.NextSingle() - 0.5f) * 2f,
				Y = (Random.NextSingle() - 0.5f) * 2f,
				Z = z,
				PreviousZ = z,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _stars.Length; i++)
		{
			ref var star = ref _stars[i];

			star.PreviousZ = star.Z;
			star.Z -= frame.DeltaTime * 0.5f;

			if (star.Z <= 0.02f)
			{
				star.Z = 1f;
				star.PreviousZ = 1f;
				star.X = (Random.NextSingle() - 0.5f) * 2f;
				star.Y = (Random.NextSingle() - 0.5f) * 2f;
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var centerX = frame.Width / 2f;
		var centerY = frame.Height / 2f;
		var size = ResolveSize();

		foreach (var star in _stars)
		{
			var x1 = centerX + (star.X / star.Z * centerX * 0.5f);
			var y1 = centerY + (star.Y / star.Z * centerY * 0.5f);
			var x0 = centerX + (star.X / star.PreviousZ * centerX * 0.5f);
			var y0 = centerY + (star.Y / star.PreviousZ * centerY * 0.5f);

			var width = (((1f - star.Z) * 2f) + 0.3f) * size;
			Line(canvas, x0, y0, x1, y1, Primary, (1f - star.Z) * 0.85f, width);
		}
	}
}
