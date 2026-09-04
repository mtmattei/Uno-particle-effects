using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Four interfering sine fields, drawn by an SKSL shader. Continuous rather
/// than the grid of cells the study sheet used, and the same cost at any size.
/// </summary>
[Effect("Plasma", EffectCategory.Procedural,
	Cost = EffectCost.Medium,
	Description = "Four interfering sine fields, drawn by a shader.",
	Tags = "shader,generative,background",
	IsShaderBased = true,
	CommonKnobs = "Speed,Opacity,Seed")]
public sealed partial class PlasmaEffect : ShaderEffect
{
	protected override string ShaderResourceName => "Uno.Effects.Particles.Shaders.Plasma.sksl";

	protected override void Step(in EffectFrame frame)
	{
	}

	protected override void SetUniforms(SKRuntimeEffectUniforms uniforms, in EffectFrame frame)
	{
		uniforms["uResolution"] = new[] { frame.Width, frame.Height };
		uniforms["uTime"] = frame.Time;
		SetColor(uniforms, "uColor", Accent, (float)Opacity);
	}
}
