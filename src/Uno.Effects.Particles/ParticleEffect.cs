using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>A XAML-instantiable list of brushes, used for effect palettes.</summary>
public sealed class BrushCollection : ObservableCollection<Brush>
{
}

/// <summary>Why an effect asked to be redrawn.</summary>
public enum EffectInvalidation
{
	/// <summary>The next frame differs; the existing particle pool is still valid.</summary>
	Visual,

	/// <summary>The pool itself is stale and must be re-emitted.</summary>
	State,
}

/// <summary>
/// Base class for every effect. Effects are <see cref="DependencyObject"/>s, so
/// every knob below is bindable, animatable from a Storyboard or VisualState,
/// and visible to the Hot Design property panel.
/// </summary>
/// <remarks>
/// Subclasses override <see cref="Emit"/>, <see cref="Step"/> and
/// <see cref="Draw"/>. Nothing here touches the visual tree: one effect draws
/// hundreds of particles onto a single <see cref="SKCanvas"/>.
/// </remarks>
public abstract partial class ParticleEffect : DependencyObject, IDisposable
{
	/// <summary>The study sheet paper-white ink.</summary>
	public static readonly SKColor DefaultPrimary = new(0xE9, 0xE5, 0xDD);

	/// <summary>The study sheet amber accent.</summary>
	public static readonly SKColor DefaultAccent = new(0xD6, 0xA0, 0x68);

	/// <summary>The study sheet sage.</summary>
	public static readonly SKColor DefaultSage = new(0x96, 0xA3, 0x8C);

	/// <summary>The study sheet clay.</summary>
	public static readonly SKColor DefaultClay = new(0xBE, 0x78, 0x60);

	/// <summary>
	/// The palette an effect draws with when it is handed none. Four colours,
	/// because the multi-coloured studies (confetti, nebula, leaves) need more
	/// than an ink and an accent.
	/// </summary>
	private static readonly SKColor[] DefaultPalette = [DefaultPrimary, DefaultAccent, DefaultSage, DefaultClay];

	private readonly SKColor[] _resolvedPalette = new SKColor[4];
	private bool _paletteValid;
	private bool _emitted;

	protected ParticleEffect()
	{
		var palette = new BrushCollection();
		palette.CollectionChanged += OnPaletteCollectionChanged;
		SetValue(PaletteProperty, palette);
	}

	internal event EventHandler<EffectInvalidation>? Invalidated;

	#region Common properties

	/// <summary>
	/// Number of particles. 0 means the count this effect was designed around,
	/// scaled by the device tier.
	/// </summary>
	[EffectProperty(0, 400, Step = 1, Group = "Motion", ZeroIsAutomatic = true)]
	public int Count
	{
		get => (int)GetValue(CountProperty);
		set => SetValue(CountProperty, value);
	}

	public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
		nameof(Count), typeof(int), typeof(ParticleEffect), new PropertyMetadata(0, OnStateChanged));

	/// <summary>Time multiplier. 1 is the speed the effect was tuned at.</summary>
	[EffectProperty(0, 4, Step = 0.05, Group = "Motion")]
	public double Speed
	{
		get => (double)GetValue(SpeedProperty);
		set => SetValue(SpeedProperty, value);
	}

	public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
		nameof(Speed), typeof(double), typeof(ParticleEffect), new PropertyMetadata(1d, OnVisualChanged));

	/// <summary>Particle size multiplier. 0 means the size the effect defines.</summary>
	[EffectProperty(0, 4, Step = 0.05, Group = "Motion", ZeroIsAutomatic = true)]
	public double Size
	{
		get => (double)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
		nameof(Size), typeof(double), typeof(ParticleEffect), new PropertyMetadata(0d, OnVisualChanged));

	/// <summary>Overall opacity of this layer.</summary>
	[EffectProperty(0, 1, Step = 0.01, Group = "Colour")]
	public double Opacity
	{
		get => (double)GetValue(OpacityProperty);
		set => SetValue(OpacityProperty, value);
	}

	public static readonly DependencyProperty OpacityProperty = DependencyProperty.Register(
		nameof(Opacity), typeof(double), typeof(ParticleEffect), new PropertyMetadata(1d, OnVisualChanged));

	/// <summary>
	/// Seed for the random source. Changing it re-emits the pool; the same seed
	/// always produces the same frames, on every platform.
	/// </summary>
	[EffectProperty(1, 999, Step = 1, Group = "Motion")]
	public int Seed
	{
		get => (int)GetValue(SeedProperty);
		set => SetValue(SeedProperty, value);
	}

	public static readonly DependencyProperty SeedProperty = DependencyProperty.Register(
		nameof(Seed), typeof(int), typeof(ParticleEffect), new PropertyMetadata(1, OnStateChanged));

	/// <summary>
	/// Rotates the motion the effect was designed with, in degrees. 0 leaves it
	/// as designed, so snow still falls downwards at the default.
	/// </summary>
	[EffectProperty(0, 360, Step = 1, Group = "Motion")]
	public double Direction
	{
		get => (double)GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
		nameof(Direction), typeof(double), typeof(ParticleEffect), new PropertyMetadata(0d, OnVisualChanged));

	/// <summary>Downward acceleration multiplier, where the effect defines 1.</summary>
	[EffectProperty(-2, 4, Step = 0.05, Group = "Motion")]
	public double Gravity
	{
		get => (double)GetValue(GravityProperty);
		set => SetValue(GravityProperty, value);
	}

	public static readonly DependencyProperty GravityProperty = DependencyProperty.Register(
		nameof(Gravity), typeof(double), typeof(ParticleEffect), new PropertyMetadata(1d, OnVisualChanged));

	/// <summary>How widely particles scatter from their emitter.</summary>
	[EffectProperty(0, 3, Step = 0.05, Group = "Motion")]
	public double Spread
	{
		get => (double)GetValue(SpreadProperty);
		set => SetValue(SpreadProperty, value);
	}

	public static readonly DependencyProperty SpreadProperty = DependencyProperty.Register(
		nameof(Spread), typeof(double), typeof(ParticleEffect), new PropertyMetadata(1d, OnStateChanged));

	/// <summary>Whether this layer reacts to the pointer.</summary>
	public bool Interactive
	{
		get => (bool)GetValue(InteractiveProperty);
		set => SetValue(InteractiveProperty, value);
	}

	public static readonly DependencyProperty InteractiveProperty = DependencyProperty.Register(
		nameof(Interactive), typeof(bool), typeof(ParticleEffect), new PropertyMetadata(true, OnVisualChanged));

	/// <summary>
	/// Colours the effect draws with. Index 0 is the primary ink, index 1 the
	/// accent. Because these are brushes they can come from theme resources, so
	/// an effect follows light/dark switching without any code.
	/// </summary>
	public BrushCollection Palette => (BrushCollection)GetValue(PaletteProperty);

	public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
		nameof(Palette), typeof(BrushCollection), typeof(ParticleEffect), new PropertyMetadata(null, OnPaletteChanged));

	#endregion

	#region Resolved values

	/// <summary>The particle count actually used, after auto and device tier.</summary>
	public int ResolveCount(in EffectFrame frame) =>
		Math.Max(1, (int)MathF.Round((Count > 0 ? Count : NaturalCount) * frame.Density));

	/// <summary>
	/// Whether this effect simulates particles at all. A shader effect paints
	/// every pixel instead, and reporting a particle count for one would make
	/// the diagnostics overlay lie.
	/// </summary>
	public bool SimulatesParticles => NaturalCount > 0;

	/// <summary>The size multiplier actually used, after auto.</summary>
	public float ResolveSize() => Size > 0 ? (float)Size : 1f;

	/// <summary>Primary ink.</summary>
	protected SKColor Primary
	{
		get
		{
			EnsurePalette();
			return _resolvedPalette[0];
		}
	}

	/// <summary>Accent colour; falls back to the primary when unset.</summary>
	protected SKColor Accent
	{
		get
		{
			EnsurePalette();
			return _resolvedPalette[1];
		}
	}

	/// <summary>
	/// A colour from the palette by index, wrapping round. Effects that scatter
	/// several colours use this rather than naming each one, so a four-brush
	/// palette recolours them completely.
	/// </summary>
	protected SKColor PaletteAt(int index)
	{
		EnsurePalette();
		return _resolvedPalette[((index % _resolvedPalette.Length) + _resolvedPalette.Length) % _resolvedPalette.Length];
	}

	/// <summary>Applies <see cref="Opacity"/> to an alpha value in [0, 1].</summary>
	protected byte Alpha(float alpha) =>
		(byte)Math.Clamp(alpha * (float)Opacity * 255f, 0f, 255f);

	/// <summary>A colour at the given alpha, after <see cref="Opacity"/>.</summary>
	protected SKColor WithAlpha(SKColor color, float alpha) => color.WithAlpha(Alpha(alpha));

	/// <summary>
	/// Drops the resolved palette if a brush's colour changed in place since it
	/// was resolved. The owning view calls this once per frame.
	/// </summary>
	internal void RefreshPalette()
	{
		if (!_paletteValid || Palette is not { Count: > 0 } palette)
		{
			return;
		}

		for (var i = 0; i < _resolvedPalette.Length; i++)
		{
			if (palette[i % palette.Count] is SolidColorBrush solid)
			{
				var c = solid.Color;
				if (_resolvedPalette[i] != new SKColor(c.R, c.G, c.B, c.A))
				{
					_paletteValid = false;
					return;
				}
			}
		}
	}

	private void EnsurePalette()
	{
		if (_paletteValid)
		{
			return;
		}

		var palette = Palette;
		var supplied = palette?.Count ?? 0;

		for (var i = 0; i < _resolvedPalette.Length; i++)
		{
			// Once a palette is supplied it governs entirely: an index past its
			// end wraps round inside it rather than falling back to a default
			// colour the caller never asked for.
			_resolvedPalette[i] = supplied > 0
				? Resolve(palette![i % supplied], DefaultPalette[i])
				: DefaultPalette[i];
		}

		_paletteValid = true;

		static SKColor Resolve(Brush brush, SKColor fallback)
		{
			if (brush is SolidColorBrush solid)
			{
				var c = solid.Color;
				return new SKColor(c.R, c.G, c.B, c.A);
			}

			return fallback;
		}
	}

	#endregion

	#region Overridables

	/// <summary>The count this effect was designed around, before any scaling.</summary>
	protected abstract int NaturalCount { get; }

	/// <summary>
	/// How much of the previous frame survives into this one: 1 clears
	/// completely, lower values leave motion trails.
	/// </summary>
	public virtual float TrailFade => 1f;

	/// <summary>
	/// Creates the particle pool for a surface of the given size. Called on
	/// first draw, on resize, and whenever a state property changes. Must draw
	/// all randomness from <see cref="Random"/> so it stays deterministic.
	/// </summary>
	protected abstract void Emit(in EffectFrame frame);

	/// <summary>Advances the simulation by <see cref="EffectFrame.DeltaTime"/>.</summary>
	protected abstract void Step(in EffectFrame frame);

	/// <summary>Draws the current state. Must not mutate simulation state.</summary>
	protected abstract void Draw(SKCanvas canvas, in EffectFrame frame);

	#endregion

	/// <summary>The deterministic random source, reset by every <see cref="Emit"/>.</summary>
	protected EffectRandom Random;

	/// <summary>
	/// Simulated seconds this effect has run for, kept by the owning view so
	/// each layer advances at its own <see cref="Speed"/>.
	/// </summary>
	internal float ElapsedTime;

	/// <summary>
	/// Emits if needed, steps, and draws one frame. <see cref="ParticleView"/>
	/// calls this; so does the render-test harness, which is the point of it
	/// being public — an effect can be driven at a fixed timestep onto any
	/// canvas, with no visual tree involved.
	/// </summary>
	public void Render(SKCanvas canvas, in EffectFrame frame, bool reemit)
	{
		if (reemit || !_emitted)
		{
			Random = new EffectRandom(Seed);
			ResetSpawnCarry();
			Emit(in frame);
			_emitted = true;
		}

		if (frame.DeltaTime > 0)
		{
			Step(in frame);
		}

		Draw(canvas, in frame);
	}

	internal void MarkUnemitted() => _emitted = false;

	/// <summary>
	/// Releases the native drawing resources this effect holds. An effect that
	/// is replaced rather than reused - the gallery does this on every reset and
	/// every navigation - should be disposed, or its paint waits for
	/// finalization.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			DisposeDrawingObjects();
		}
	}

	/// <summary>Requests a redraw, optionally re-emitting the particle pool.</summary>
	protected void Invalidate(EffectInvalidation kind = EffectInvalidation.Visual)
	{
		if (kind == EffectInvalidation.State)
		{
			_emitted = false;
		}

		Invalidated?.Invoke(this, kind);
	}

	protected static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((ParticleEffect)d).Invalidate();

	protected static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((ParticleEffect)d).Invalidate(EffectInvalidation.State);

	private static void OnPaletteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var effect = (ParticleEffect)d;

		if (e.OldValue is BrushCollection old)
		{
			old.CollectionChanged -= effect.OnPaletteCollectionChanged;
		}

		if (e.NewValue is BrushCollection added)
		{
			added.CollectionChanged += effect.OnPaletteCollectionChanged;
		}

		effect._paletteValid = false;
		effect.Invalidate();
	}

	private void OnPaletteCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		_paletteValid = false;
		Invalidate();
	}
}
