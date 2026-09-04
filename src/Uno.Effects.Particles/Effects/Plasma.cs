using Microsoft.UI.Xaml;
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
	CommonKnobs = "Speed,Opacity")]
public sealed partial class Plasma : ShaderEffect
{
	/// <summary>
	/// Grid the field is sampled on. 28 is the resolution the study sheet used;
	/// 0 evaluates continuously, which the shader can do and the original grid
	/// could not.
	/// </summary>
	[EffectProperty(0, 96, Step = 1, Group = "Plasma")]
	public double Cells
	{
		get => (double)GetValue(CellsProperty);
		set => SetValue(CellsProperty, value);
	}

	public static readonly DependencyProperty CellsProperty = DependencyProperty.Register(
		nameof(Cells), typeof(double), typeof(Plasma), new PropertyMetadata(28d, OnVisualChanged));

	protected override string ShaderResourceName => "Uno.Effects.Particles.Shaders.Plasma.sksl";

	protected override void Step(in EffectFrame frame)
	{
	}

	protected override void SetUniforms(SKRuntimeEffectUniforms uniforms, in EffectFrame frame)
	{
		uniforms["uResolution"] = new[] { frame.Width, frame.Height };
		uniforms["uTime"] = frame.Time;
		uniforms["uCells"] = (float)Cells;
		SetColor(uniforms, "uColor", Accent, (float)Opacity);
	}
}
