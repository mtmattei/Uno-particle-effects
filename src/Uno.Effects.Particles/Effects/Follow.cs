using Microsoft.UI.Xaml;
using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// A swarm of springs chasing the pointer, each with its own stiffness, so the
/// swarm arrives as a smear rather than a clump.
/// </summary>
[Effect("Follow", EffectCategory.Interactive,
	Cost = EffectCost.Light,
	Description = "A swarm of springs chasing the pointer.",
	Tags = "pointer,cursor,playful",
	RespondsToPointer = true,
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class FollowEffect : ParticleEffect
{
	private struct Chaser
	{
		public float X;
		public float Y;
		public float VelocityX;
		public float VelocityY;
		public float Stiffness;
	}

	private Chaser[] _chasers = [];

	/// <summary>How hard the swarm is pulled towards the pointer.</summary>
	[EffectProperty(0.1, 4, Step = 0.05, Group = "Follow")]
	public double Attraction
	{
		get => (double)GetValue(AttractionProperty);
		set => SetValue(AttractionProperty, value);
	}

	public static readonly DependencyProperty AttractionProperty = DependencyProperty.Register(
		nameof(Attraction), typeof(double), typeof(FollowEffect), new PropertyMetadata(1d, OnVisualChanged));

	protected override int NaturalCount => 40;

	/// <summary>The smear is the point, so most of each frame survives.</summary>
	protected internal override float TrailFade => 0.3f;

	protected override void Emit(in EffectFrame frame)
	{
		_chasers = new Chaser[ResolveCount(in frame)];

		for (var i = 0; i < _chasers.Length; i++)
		{
			_chasers[i] = new Chaser
			{
				X = frame.Width / 2f,
				Y = frame.Height / 2f,
				Stiffness = Random.Next(3f) + 1.5f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var attraction = (float)Attraction;

		for (var i = 0; i < _chasers.Length; i++)
		{
			ref var chaser = ref _chasers[i];

			chaser.VelocityX += (frame.PointerX - chaser.X) * chaser.Stiffness * attraction * frame.DeltaTime;
			chaser.VelocityY += (frame.PointerY - chaser.Y) * chaser.Stiffness * attraction * frame.DeltaTime;
			chaser.VelocityX *= 0.94f;
			chaser.VelocityY *= 0.94f;
			chaser.X += chaser.VelocityX * frame.DeltaTime * 8f;
			chaser.Y += chaser.VelocityY * frame.DeltaTime * 8f;
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var radius = 1.2f * ResolveSize();

		foreach (var chaser in _chasers)
		{
			Dot(canvas, chaser.X, chaser.Y, radius, Primary, 0.8f);
		}
	}
}
