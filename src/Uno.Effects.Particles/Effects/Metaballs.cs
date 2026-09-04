using Microsoft.UI.Xaml;
using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Blobs that merge as they pass, drawn by an SKSL shader evaluating an
/// inverse-square field. The orbits are simulated in C#; only the field is
/// shaded.
/// </summary>
[Effect("Metaballs", EffectCategory.Procedural,
	Cost = EffectCost.Heavy,
	Description = "Blobs that merge as they pass, shaded from a scalar field.",
	Tags = "shader,organic,hero",
	IsShaderBased = true,
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class MetaballsEffect : ShaderEffect
{
	/// <summary>Matches the array length declared in Metaballs.sksl.</summary>
	private const int MaxBalls = 8;

	private struct Ball
	{
		public float Phase;
		public float Rate;
		public float Radius;
	}

	private readonly float[] _uniformBalls = new float[MaxBalls * 3];
	private Ball[] _balls = [];

	/// <summary>
	/// Field value at which the surface becomes solid. Lower values fatten the
	/// blobs and make them merge sooner.
	/// </summary>
	[EffectProperty(0.4, 2.5, Step = 0.05, Group = "Metaballs")]
	public double Threshold
	{
		get => (double)GetValue(ThresholdProperty);
		set => SetValue(ThresholdProperty, value);
	}

	public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
		nameof(Threshold), typeof(double), typeof(MetaballsEffect), new PropertyMetadata(1d, OnVisualChanged));

	protected override string ShaderResourceName => "Uno.Effects.Particles.Shaders.Metaballs.sksl";

	protected override int NaturalCount => 5;

	protected override void Emit(in EffectFrame frame)
	{
		var count = Math.Min(MaxBalls, ResolveCount(in frame));
		_balls = new Ball[count];

		for (var i = 0; i < count; i++)
		{
			_balls[i] = new Ball
			{
				Phase = Random.Next(Tau),
				Rate = Random.Next(0.4f) + 0.3f,
				Radius = Random.Next(10f) + 14f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
	}

	protected override void SetUniforms(SKRuntimeEffectUniforms uniforms, in EffectFrame frame)
	{
		// The study sheet was tuned on a ~128px tile; keeping the blobs
		// proportional means the effect reads the same in a grid cell and on a
		// full-page preview.
		var scale = MathF.Min(frame.Width, frame.Height) / 128f * ResolveSize();

		Array.Clear(_uniformBalls);

		for (var i = 0; i < _balls.Length; i++)
		{
			var ball = _balls[i];
			_uniformBalls[i * 3] = (frame.Width / 2f)
				+ (MathF.Sin((frame.Time * ball.Rate) + ball.Phase) * frame.Width * 0.28f);
			_uniformBalls[(i * 3) + 1] = (frame.Height / 2f)
				+ (MathF.Cos((frame.Time * ball.Rate * 1.3f) + ball.Phase) * frame.Height * 0.28f);
			_uniformBalls[(i * 3) + 2] = ball.Radius * scale;
		}

		uniforms["uResolution"] = new[] { frame.Width, frame.Height };
		uniforms["uBalls"] = _uniformBalls;
		uniforms["uThreshold"] = (float)Threshold;
		SetColor(uniforms, "uColor", Primary, (float)Opacity);
	}
}
