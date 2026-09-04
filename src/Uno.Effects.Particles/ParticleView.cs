using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace Uno.Effects.Particles;

/// <summary>A XAML-instantiable list of effects, used as the layers of a view.</summary>
public sealed class EffectCollection : ObservableCollection<ParticleEffect>
{
}

/// <summary>
/// The single control this library ships. It owns a Skia surface, the frame
/// loop and the pointer, and draws one or more <see cref="ParticleEffect"/>
/// layers onto it.
/// </summary>
/// <remarks>
/// Built on <see cref="SKCanvasElement"/>, so the same pixels are produced on
/// every target the Uno Skia renderer supports, from one code path.
/// </remarks>
[ContentProperty(Name = nameof(Effects))]
public partial class ParticleView : SKCanvasElement
{
	private const float MaxDeltaTime = 0.05f;

	private readonly Stopwatch _clock = new();
	private SKSurface? _surface;
	private SKPaint? _blitPaint;
	private SKFont? _diagnosticsFont;
	private SKPaint? _diagnosticsPaint;

	private int _surfaceWidth;
	private int _surfaceHeight;
	private float _surfaceScale;
	private bool _needsEmit = true;
	private bool _blitPaintValid;
	private bool _rendering;
	private bool _inViewport = true;
	private long _lastTicks;

	private float _pointerX;
	private float _pointerY;
	private bool _pointerInside;

	// Diagnostics.
	private float _fps;
	private double _lastDrawMilliseconds;
	private int _lastParticleCount;

	public ParticleView()
	{
		var effects = new EffectCollection();
		effects.CollectionChanged += OnEffectsChanged;
		SetValue(EffectsProperty, effects);

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		EffectiveViewportChanged += OnEffectiveViewportChanged;

		PointerMoved += OnPointerMoved;
		PointerEntered += OnPointerMoved;
		PointerExited += OnPointerExited;
	}

	#region Properties

	/// <summary>
	/// The effect layers, drawn back to front. This is the content property, so
	/// effects are simply the children of the view in XAML.
	/// </summary>
	public EffectCollection Effects => (EffectCollection)GetValue(EffectsProperty);

	public static readonly DependencyProperty EffectsProperty = DependencyProperty.Register(
		nameof(Effects), typeof(EffectCollection), typeof(ParticleView), new PropertyMetadata(null, OnEffectsPropertyChanged));

	/// <summary>The surface the effects are drawn onto. Transparent is honoured.</summary>
	public Brush? Surface
	{
		get => (Brush?)GetValue(SurfaceProperty);
		set => SetValue(SurfaceProperty, value);
	}

	public static readonly DependencyProperty SurfaceProperty = DependencyProperty.Register(
		nameof(Surface), typeof(Brush), typeof(ParticleView),
		new PropertyMetadata(null, OnStateChanged));

	/// <summary>
	/// Stops the clock without tearing anything down. Bind this to a VisualState
	/// to start and stop an effect on hover.
	/// </summary>
	public bool Paused
	{
		get => (bool)GetValue(PausedProperty);
		set => SetValue(PausedProperty, value);
	}

	public static readonly DependencyProperty PausedProperty = DependencyProperty.Register(
		nameof(Paused), typeof(bool), typeof(ParticleView), new PropertyMetadata(false, OnPausedChanged));

	/// <summary>Master switch for pointer reactions.</summary>
	public bool Interactive
	{
		get => (bool)GetValue(InteractiveProperty);
		set => SetValue(InteractiveProperty, value);
	}

	public static readonly DependencyProperty InteractiveProperty = DependencyProperty.Register(
		nameof(Interactive), typeof(bool), typeof(ParticleView), new PropertyMetadata(false));

	/// <summary>
	/// Renders a single settled frame and holds it. This is what makes designer
	/// thumbnails, static previews and screenshots show a real frame rather than
	/// a blank canvas or a random first tick.
	/// </summary>
	public bool FrozenFrame
	{
		get => (bool)GetValue(FrozenFrameProperty);
		set => SetValue(FrozenFrameProperty, value);
	}

	public static readonly DependencyProperty FrozenFrameProperty = DependencyProperty.Register(
		nameof(FrozenFrame), typeof(bool), typeof(ParticleView), new PropertyMetadata(false, OnFrozenFrameChanged));

	/// <summary>
	/// Frames simulated before the first visible frame, so a view never starts
	/// from an unsettled pool. Also what a frozen frame settles for.
	/// </summary>
	public int WarmUpFrames
	{
		get => (int)GetValue(WarmUpFramesProperty);
		set => SetValue(WarmUpFramesProperty, value);
	}

	public static readonly DependencyProperty WarmUpFramesProperty = DependencyProperty.Register(
		nameof(WarmUpFrames), typeof(int), typeof(ParticleView), new PropertyMetadata(90, OnStateChanged));

	/// <summary>Global time multiplier applied on top of each effect Speed.</summary>
	public double Speed
	{
		get => (double)GetValue(SpeedProperty);
		set => SetValue(SpeedProperty, value);
	}

	public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
		nameof(Speed), typeof(double), typeof(ParticleView), new PropertyMetadata(1d));

	/// <summary>
	/// Particle budget multiplier. Defaults to a device-tier heuristic; set it
	/// explicitly to override.
	/// </summary>
	public double Density
	{
		get => (double)GetValue(DensityProperty);
		set => SetValue(DensityProperty, value);
	}

	public static readonly DependencyProperty DensityProperty = DependencyProperty.Register(
		nameof(Density), typeof(double), typeof(ParticleView), new PropertyMetadata(1d, OnStateChanged));

	/// <summary>Gaussian blur applied to the whole composed layer stack.</summary>
	public double Blur
	{
		get => (double)GetValue(BlurProperty);
		set => SetValue(BlurProperty, value);
	}

	public static readonly DependencyProperty BlurProperty = DependencyProperty.Register(
		nameof(Blur), typeof(double), typeof(ParticleView), new PropertyMetadata(0d, OnBlitPaintChanged));

	/// <summary>How the composed layer stack blends with what is behind it.</summary>
	public SKBlendMode BlendMode
	{
		get => (SKBlendMode)GetValue(BlendModeProperty);
		set => SetValue(BlendModeProperty, value);
	}

	public static readonly DependencyProperty BlendModeProperty = DependencyProperty.Register(
		nameof(BlendMode), typeof(SKBlendMode), typeof(ParticleView),
		new PropertyMetadata(SKBlendMode.SrcOver, OnBlitPaintChanged));

	/// <summary>
	/// Overlays frames per second, particle count and draw time. The same
	/// overlay the gallery uses, available on any device.
	/// </summary>
	public bool ShowDiagnostics
	{
		get => (bool)GetValue(ShowDiagnosticsProperty);
		set => SetValue(ShowDiagnosticsProperty, value);
	}

	public static readonly DependencyProperty ShowDiagnosticsProperty = DependencyProperty.Register(
		nameof(ShowDiagnostics), typeof(bool), typeof(ParticleView), new PropertyMetadata(false));

	/// <summary>
	/// When true (the default) a system reduced-motion preference freezes the
	/// view on a settled frame instead of animating.
	/// </summary>
	public bool RespectReducedMotion
	{
		get => (bool)GetValue(RespectReducedMotionProperty);
		set => SetValue(RespectReducedMotionProperty, value);
	}

	public static readonly DependencyProperty RespectReducedMotionProperty = DependencyProperty.Register(
		nameof(RespectReducedMotion), typeof(bool), typeof(ParticleView), new PropertyMetadata(true, OnPausedChanged));

	#endregion

	/// <summary>Frames per second measured over the last second.</summary>
	public float FramesPerSecond => _fps;

	/// <summary>Milliseconds spent in the last simulate-and-draw pass.</summary>
	public double DrawMilliseconds => _lastDrawMilliseconds;

	/// <summary>Particles simulated in the last pass, across all layers.</summary>
	public int ParticleCount => _lastParticleCount;

	#region Lifecycle

	protected override Size MeasureOverride(Size availableSize)
	{
		// SKCanvasElement derives from FrameworkElement, which measures to zero.
		// Take the space offered so the view works in a StackPanel too.
		var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
		var height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
		return new Size(width, height);
	}

	private void OnLoaded(object sender, RoutedEventArgs e) => UpdateClock();

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		StopClock();
		_surface?.Dispose();
		_surface = null;
		_surfaceWidth = _surfaceHeight = 0;
		_needsEmit = true;
	}

	private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
	{
		// A tile scrolled out of sight costs nothing: one of the behavioural
		// rules the recommendation asks to hold on every platform.
		var visible = args.BringIntoViewDistanceX < 400 && args.BringIntoViewDistanceY < 400;
		if (visible != _inViewport)
		{
			_inViewport = visible;
			UpdateClock();
		}
	}

	private bool ShouldRun =>
		IsLoaded
		&& _inViewport
		&& !Paused
		&& !FrozenFrame
		&& !(RespectReducedMotion && ReducedMotion.IsEnabled);

	private void UpdateClock()
	{
		if (ShouldRun)
		{
			StartClock();
		}
		else
		{
			StopClock();
			Invalidate();
		}
	}

	private void StartClock()
	{
		if (_rendering)
		{
			return;
		}

		_rendering = true;
		_clock.Restart();
		_lastTicks = 0;
		CompositionTarget.Rendering += OnRendering;
	}

	private void StopClock()
	{
		if (!_rendering)
		{
			return;
		}

		_rendering = false;
		CompositionTarget.Rendering -= OnRendering;
		_clock.Stop();
	}

	private void OnRendering(object? sender, object e) => Invalidate();

	#endregion

	#region Pointer

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		var point = e.GetCurrentPoint(this).Position;
		_pointerX = (float)point.X;
		_pointerY = (float)point.Y;
		_pointerInside = true;
	}

	private void OnPointerExited(object sender, PointerRoutedEventArgs e) => _pointerInside = false;

	#endregion

	#region Effects plumbing

	private static void OnEffectsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var view = (ParticleView)d;

		if (e.OldValue is EffectCollection old)
		{
			old.CollectionChanged -= view.OnEffectsChanged;
			foreach (var effect in old)
			{
				effect.Invalidated -= view.OnEffectInvalidated;
			}
		}

		if (e.NewValue is EffectCollection added)
		{
			added.CollectionChanged += view.OnEffectsChanged;
			foreach (var effect in added)
			{
				effect.Invalidated += view.OnEffectInvalidated;
			}
		}

		view._needsEmit = true;
		view.Invalidate();
	}

	private void OnEffectsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		foreach (var effect in e.OldItems?.OfType<ParticleEffect>() ?? [])
		{
			effect.Invalidated -= OnEffectInvalidated;
		}

		foreach (var effect in e.NewItems?.OfType<ParticleEffect>() ?? [])
		{
			effect.Invalidated += OnEffectInvalidated;
		}

		_needsEmit = true;
		Invalidate();
	}

	private void OnEffectInvalidated(object? sender, EffectInvalidation kind)
	{
		if (kind == EffectInvalidation.State)
		{
			_needsEmit = true;
		}

		Invalidate();
	}

	private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var view = (ParticleView)d;
		view._needsEmit = true;
		view.Invalidate();
	}

	private static void OnPausedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((ParticleView)d).UpdateClock();

	private static void OnFrozenFrameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var view = (ParticleView)d;
		view._needsEmit = true;
		view.UpdateClock();
	}

	private static void OnBlitPaintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var view = (ParticleView)d;
		view._blitPaintValid = false;
		view.Invalidate();
	}

	#endregion

	#region Rendering

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		var width = (float)area.Width;
		var height = (float)area.Height;
		if (width <= 0 || height <= 0)
		{
			return;
		}

		var scale = canvas.TotalMatrix.ScaleX;
		if (scale <= 0)
		{
			scale = 1f;
		}

		var surface = EnsureSurface(width, height, scale);
		if (surface is null)
		{
			return;
		}

		var start = Stopwatch.GetTimestamp();
		var offscreen = surface.Canvas;

		offscreen.Save();
		offscreen.Scale(scale);

		if (_needsEmit)
		{
			_needsEmit = false;
			offscreen.Clear(SurfaceColor);

			foreach (var effect in Effects)
			{
				effect.MarkUnemitted();
				effect.ElapsedTime = 0f;
			}

			// Settle the pool before anything is shown, exactly as the study
			// sheet does: an idle tile is a real frame, not a first tick.
			var warmUp = Math.Max(0, WarmUpFrames);
			for (var i = 0; i < warmUp; i++)
			{
				RenderPass(offscreen, width, height, 1f / 60f);
			}

			if (warmUp == 0)
			{
				RenderPass(offscreen, width, height, 0f);
			}
		}
		else
		{
			RenderPass(offscreen, width, height, NextDeltaTime());
		}

		offscreen.Restore();

		Blit(canvas, surface, scale);

		_lastDrawMilliseconds = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;

		if (ShowDiagnostics)
		{
			DrawDiagnostics(canvas, width, height);
		}
	}

	private float NextDeltaTime()
	{
		if (!_rendering)
		{
			return 0f;
		}

		var ticks = _clock.ElapsedTicks;
		var delta = (float)((ticks - _lastTicks) / (double)Stopwatch.Frequency);
		_lastTicks = ticks;

		if (delta <= 0f)
		{
			return 0f;
		}

		_fps = _fps <= 0 ? 1f / delta : _fps + (1f / delta - _fps) * 0.1f;

		return Math.Min(delta, MaxDeltaTime) * (float)Speed;
	}

	private void RenderPass(SKCanvas canvas, float width, float height, float delta)
	{
		var effects = Effects;
		if (effects.Count == 0)
		{
			canvas.Clear(SurfaceColor);
			_lastParticleCount = 0;
			return;
		}

		FadeSurface(canvas, width, height, effects[0].TrailFade);

		var density = (float)Math.Clamp(Density, 0.05, 4);
		var total = 0;

		foreach (var effect in effects)
		{
			var effectDelta = delta * (float)effect.Speed;
			effect.ElapsedTime += effectDelta;

			var interactive = Interactive && effect.Interactive;
			var inside = interactive && _pointerInside;

			// Off-pointer, the view supplies a slow synthetic drift so an
			// interactive effect still reads as alive in a grid of tiles.
			var pointerX = inside ? _pointerX : width * (0.5f + 0.3f * MathF.Sin(effect.ElapsedTime * 0.6f));
			var pointerY = inside ? _pointerY : height * (0.5f + 0.3f * MathF.Cos(effect.ElapsedTime * 0.45f));

			var frame = new EffectFrame(
				width, height, effectDelta, effect.ElapsedTime, pointerX, pointerY, inside, density);

			effect.Render(canvas, in frame, reemit: false);
			total += effect.ResolveCount(in frame);
		}

		_lastParticleCount = total;
	}

	private void FadeSurface(SKCanvas canvas, float width, float height, float fade)
	{
		var background = SurfaceColor;

		if (fade >= 1f)
		{
			canvas.Clear(background);
			return;
		}

		using var paint = new SKPaint { IsAntialias = false };

		if (background.Alpha == 0)
		{
			// Nothing to fade towards, so erase a fraction of the alpha instead.
			paint.Color = SKColors.Black.WithAlpha((byte)(fade * 255));
			paint.BlendMode = SKBlendMode.DstOut;
		}
		else
		{
			paint.Color = background.WithAlpha((byte)(fade * 255));
			paint.BlendMode = SKBlendMode.SrcOver;
		}

		canvas.DrawRect(0, 0, width, height, paint);
	}

	private SKSurface? EnsureSurface(float width, float height, float scale)
	{
		var pixelWidth = Math.Max(1, (int)MathF.Round(width * scale));
		var pixelHeight = Math.Max(1, (int)MathF.Round(height * scale));

		if (_surface is not null
			&& _surfaceWidth == pixelWidth
			&& _surfaceHeight == pixelHeight
			&& Math.Abs(_surfaceScale - scale) < 0.001f)
		{
			return _surface;
		}

		_surface?.Dispose();
		_surface = SKSurface.Create(new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
		_surfaceWidth = pixelWidth;
		_surfaceHeight = pixelHeight;
		_surfaceScale = scale;
		_needsEmit = true;

		return _surface;
	}

	private void Blit(SKCanvas canvas, SKSurface surface, float scale)
	{
		if (!_blitPaintValid)
		{
			_blitPaint?.Dispose();
			_blitPaint = null;

			if (Blur > 0 || BlendMode != SKBlendMode.SrcOver)
			{
				_blitPaint = new SKPaint { BlendMode = BlendMode };
				if (Blur > 0)
				{
					var sigma = (float)Blur * scale;
					_blitPaint.ImageFilter = SKImageFilter.CreateBlur(sigma, sigma);
				}
			}

			_blitPaintValid = true;
		}

		canvas.Save();
		canvas.Scale(1f / scale);
		surface.Draw(canvas, 0, 0, _blitPaint);
		canvas.Restore();
	}

	private SKColor SurfaceColor =>
		Surface is SolidColorBrush solid
			? new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A)
			: SKColors.Transparent;

	private void DrawDiagnostics(SKCanvas canvas, float width, float height)
	{
		_diagnosticsFont ??= new SKFont(SKTypeface.Default, 10f);
		_diagnosticsPaint ??= new SKPaint { IsAntialias = true };

		var text = $"{_fps:0} fps · {_lastParticleCount} particles · {_lastDrawMilliseconds:0.00} ms";

		_diagnosticsPaint.Color = SKColors.Black.WithAlpha(140);
		canvas.DrawRect(6, height - 22, _diagnosticsFont.MeasureText(text) + 12, 16, _diagnosticsPaint);

		_diagnosticsPaint.Color = new SKColor(0xE9, 0xE5, 0xDD);
		canvas.DrawText(text, 12, height - 10, _diagnosticsFont, _diagnosticsPaint);
	}

	#endregion
}
