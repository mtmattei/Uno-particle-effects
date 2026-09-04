using System.Reflection;
using Microsoft.UI.Xaml;
using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// Base for effects whose picture is produced by an SKSL fragment shader rather
/// than by simulated particles.
/// </summary>
/// <remarks>
/// The shader source lives beside the class as an embedded <c>.sksl</c>
/// resource and is compiled lazily. <see cref="ShaderSource"/> can be replaced
/// at runtime, which is what makes the shader Hot Reloadable and lets the
/// gallery show the real source in its code tab.
/// </remarks>
public abstract partial class ShaderEffect : ParticleEffect
{
	private SKRuntimeEffect? _compiled;
	private SKRuntimeShaderBuilder? _builder;
	private string? _compiledFrom;
	private string? _compileError;

	/// <summary>
	/// The SKSL source. Defaults to the embedded resource named by
	/// <see cref="ShaderResourceName"/>; set it to replace the shader live.
	/// </summary>
	public string? ShaderSource
	{
		get => (string?)GetValue(ShaderSourceProperty);
		set => SetValue(ShaderSourceProperty, value);
	}

	public static readonly DependencyProperty ShaderSourceProperty = DependencyProperty.Register(
		nameof(ShaderSource), typeof(string), typeof(ShaderEffect), new PropertyMetadata(null, OnShaderSourceChanged));

	/// <summary>The embedded resource holding this effect's SKSL.</summary>
	protected abstract string ShaderResourceName { get; }

	/// <summary>The compiler error from the last failed compile, if any.</summary>
	public string? ShaderError => _compileError;

	/// <summary>Shader effects paint every pixel, so there is nothing to simulate.</summary>
	protected override int NaturalCount => 0;

	/// <summary>The effective SKSL: the override if set, else the embedded resource.</summary>
	public string EffectiveShaderSource => ShaderSource ?? LoadEmbeddedShader();

	private string LoadEmbeddedShader()
	{
		var assembly = typeof(ShaderEffect).Assembly;
		using var stream = assembly.GetManifestResourceStream(ShaderResourceName)
			?? throw new InvalidOperationException(
				$"Shader resource '{ShaderResourceName}' was not found in {assembly.GetName().Name}.");

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	/// <summary>Fills the uniforms for this frame. Called once per draw.</summary>
	protected abstract void SetUniforms(SKRuntimeEffectUniforms uniforms, in EffectFrame frame);

	protected override void Emit(in EffectFrame frame)
	{
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var builder = EnsureBuilder();
		if (builder is null)
		{
			return;
		}

		SetUniforms(builder.Uniforms, in frame);

		using var shader = builder.Build();

		var paint = Paint;
		paint.Style = SKPaintStyle.Fill;
		paint.Color = SKColors.White;
		paint.Shader = shader;
		canvas.DrawRect(0, 0, frame.Width, frame.Height, paint);
		paint.Shader = null;
	}

	private SKRuntimeShaderBuilder? EnsureBuilder()
	{
		var source = EffectiveShaderSource;

		// Compare against the source last compiled, not against the builder:
		// a compile that failed must be remembered too, or a broken shader
		// recompiles on every single frame and the app appears to hang.
		if (_compiledFrom == source)
		{
			return _builder;
		}

		_builder?.Dispose();
		_builder = null;
		_compiled?.Dispose();
		_compiled = null;
		_compiledFrom = source;

		_compiled = SKRuntimeEffect.CreateShader(source, out var errors);
		_compileError = string.IsNullOrWhiteSpace(errors) ? null : errors;

		if (_compiled is null)
		{
			// Surfacing this beats drawing nothing and leaving no trace; the
			// gallery shows it on the SKSL tab, and it reaches the console for
			// anyone running the render tests.
			Console.WriteLine($"SKSL compile failed for {GetType().Name}: {_compileError}");
			return null;
		}

		_builder = new SKRuntimeShaderBuilder(_compiled);
		return _builder;
	}

	/// <summary>Writes a colour as a premultiplication-ready RGBA uniform.</summary>
	protected static void SetColor(SKRuntimeEffectUniforms uniforms, string name, SKColor color, float alpha)
	{
		uniforms[name] = new[]
		{
			color.Red / 255f,
			color.Green / 255f,
			color.Blue / 255f,
			Math.Clamp(color.Alpha / 255f * alpha, 0f, 1f),
		};
	}

	private static void OnShaderSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((ShaderEffect)d).Invalidate();
}
