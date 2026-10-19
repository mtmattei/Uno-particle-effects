using Microsoft.UI.Xaml;
using SkiaSharp;

namespace Uno.Effects.Particles;

// The Interactive family: effects whose subject is the pointer. Off-pointer the
// view feeds them a slow synthetic position, so a grid of idle tiles still
// reads as alive.

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
public sealed partial class Follow : ParticleEffect
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
		nameof(Attraction), typeof(double), typeof(Follow), new PropertyMetadata(1d, OnVisualChanged));

	protected override int NaturalCount => 40;

	/// <summary>The smear is the point, so most of each frame survives.</summary>
	public override float TrailFade => 0.3f;

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

/// <summary>A regular grid that pushes away from the pointer and springs back.</summary>
[Effect("Repel", EffectCategory.Interactive,
	Cost = EffectCost.Light,
	Description = "A grid that pushes away from the pointer and springs back.",
	Tags = "pointer,grid,tactile",
	RespondsToPointer = true,
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Repel : ParticleEffect
{
	private struct Node
	{
		public float HomeX;
		public float HomeY;
		public float X;
		public float Y;
	}

	private Node[] _nodes = [];

	/// <summary>How far from the pointer the push reaches, in pixels.</summary>
	[EffectProperty(5, 150, Step = 1, Group = "Repel")]
	public double Reach
	{
		get => (double)GetValue(ReachProperty);
		set => SetValue(ReachProperty, value);
	}

	public static readonly DependencyProperty ReachProperty = DependencyProperty.Register(
		nameof(Reach), typeof(double), typeof(Repel), new PropertyMetadata(40d, OnVisualChanged));

	protected override int NaturalCount => 12;

	protected override void Emit(in EffectFrame frame)
	{
		var columns = ResolveGridColumns(in frame);
		var rows = columns;
		_nodes = new Node[columns * rows];

		var index = 0;
		for (var i = 0; i < columns; i++)
		{
			for (var j = 0; j < rows; j++)
			{
				var x = (i + 0.5f) * frame.Width / columns;
				var y = (j + 0.5f) * frame.Height / rows;
				_nodes[index++] = new Node { HomeX = x, HomeY = y, X = x, Y = y };
			}
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var reach = (float)Reach;

		// Framerate-independent approach to the target, capped at 1 so a long
		// frame cannot overshoot.
		var approach = MathF.Min(1f, frame.DeltaTime * 8f);

		for (var i = 0; i < _nodes.Length; i++)
		{
			ref var node = ref _nodes[i];

			var dx = node.HomeX - frame.PointerX;
			var dy = node.HomeY - frame.PointerY;
			var distance = MathF.Sqrt((dx * dx) + (dy * dy));
			if (distance <= 0)
			{
				distance = 1f;
			}

			var push = MathF.Max(0f, reach - distance) / distance * 1.2f;

			node.X += (node.HomeX + (dx * push) - node.X) * approach;
			node.Y += (node.HomeY + (dy * push) - node.Y) * approach;
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var radius = 1.2f * ResolveSize();

		foreach (var node in _nodes)
		{
			Dot(canvas, node.X, node.Y, radius, Primary, 0.7f);
		}
	}
}

/// <summary>A hexagonal lattice rippling outwards from the pointer.</summary>
[Effect("Lattice", EffectCategory.Interactive,
	Cost = EffectCost.Medium,
	Description = "A hexagonal lattice rippling outwards from the pointer.",
	Tags = "pointer,grid,ripple",
	RespondsToPointer = true,
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Lattice : ParticleEffect
{
	/// <summary>Row spacing as a fraction of column spacing, for a regular hex grid.</summary>
	private const float RowRatio = 0.866f;

	protected override int NaturalCount => 12;

	protected override void Emit(in EffectFrame frame)
	{
	}

	protected override void Step(in EffectFrame frame)
	{
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var columns = ResolveGridColumns(in frame);
		var cell = frame.Width / columns;
		var rowHeight = cell * RowRatio;
		var rows = (int)MathF.Ceiling(frame.Height / rowHeight) + 1;
		var size = ResolveSize();

		for (var j = 0; j < rows; j++)
		{
			// Every other row is offset by half a cell: that is the whole
			// difference between a square grid and a hexagonal one.
			var offset = (j % 2) * cell / 2f;

			for (var i = 0; i <= columns; i++)
			{
				var x = (i * cell) + offset;
				var y = j * rowHeight;

				var dx = x - frame.PointerX;
				var dy = y - frame.PointerY;
				var distance = MathF.Sqrt((dx * dx) + (dy * dy));
				var wave = 0.5f + (0.5f * MathF.Sin((frame.Time * 2f) - (distance * 0.08f)));

				Dot(canvas, x, y, (0.6f + (wave * 1.6f)) * size, Primary, 0.3f + (wave * 0.6f));
			}
		}
	}
}

/// <summary>Iron filings aligning to the field around the pointer.</summary>
[Effect("Filings", EffectCategory.Interactive,
	Cost = EffectCost.Medium,
	Description = "Iron filings aligning to the field around the pointer.",
	Tags = "pointer,magnetic,grid",
	RespondsToPointer = true,
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Filings : ParticleEffect
{
	protected override int NaturalCount => 14;

	protected override void Emit(in EffectFrame frame)
	{
	}

	protected override void Step(in EffectFrame frame)
	{
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var columns = ResolveGridColumns(in frame);
		var cell = frame.Width / columns;
		var rows = GridRows(cell, in frame);
		var size = ResolveSize();
		var length = 2.5f * size;

		var paint = Paint;
		paint.Style = SKPaintStyle.Stroke;
		paint.StrokeWidth = 1f;
		paint.Color = WithAlpha(Primary, 0.7f);

		for (var i = 0; i < columns; i++)
		{
			for (var j = 0; j < rows; j++)
			{
				var x = (i + 0.5f) * cell;
				var y = (j + 0.5f) * cell;

				// A quarter turn from the direction to the pointer, so each
				// filing lies along the field rather than pointing at it.
				var angle = MathF.Atan2(y - frame.PointerY, x - frame.PointerX) + (MathF.PI / 2f);
				var dx = MathF.Cos(angle) * length;
				var dy = MathF.Sin(angle) * length;

				canvas.DrawLine(x - dx, y - dy, x + dx, y + dy, paint);
			}
		}

		Dot(canvas, frame.PointerX, frame.PointerY, 2f * size, Accent, 0.9f);
	}
}
