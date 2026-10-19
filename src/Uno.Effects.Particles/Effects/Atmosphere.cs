using Microsoft.UI.Xaml;
using SkiaSharp;

namespace Uno.Effects.Particles;

// The Atmosphere family: things in the air. Quiet by design, made to sit behind
// content rather than to be looked at.

/// <summary>
/// Motes drifting on a slow noise field with a gentle upward bias. The quietest
/// thing in the set.
/// </summary>
[Effect("Dust", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Motes drifting on a slow noise field.",
	Tags = "ambient,background,subtle",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Direction,Gravity")]
public sealed partial class Dust : ParticleEffect
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

			var angle = Noise.Sample((mote.X * 0.01f) + (frame.Time * 0.05f), mote.Y * 0.01f) * Tau * 2f;
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

/// <summary>
/// Flakes falling with a horizontal sway. The one everybody reaches for first,
/// so it carries the extra knob the others do not.
/// </summary>
[Effect("Snow", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Flakes falling with a horizontal sway.",
	Tags = "ambient,seasonal,background",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Direction,Gravity")]
public sealed partial class Snow : ParticleEffect
{
	private struct Flake
	{
		public float X;
		public float Y;
		public float Radius;
		public float Phase;
		public float Speed;
	}

	private Flake[] _flakes = [];

	/// <summary>How far a flake drifts sideways as it falls.</summary>
	[EffectProperty(0, 4, Step = 0.05, Group = "Snow")]
	public double Sway
	{
		get => (double)GetValue(SwayProperty);
		set => SetValue(SwayProperty, value);
	}

	public static readonly DependencyProperty SwayProperty = DependencyProperty.Register(
		nameof(Sway), typeof(double), typeof(Snow), new PropertyMetadata(1d, OnVisualChanged));

	protected override int NaturalCount => 90;

	protected override void Emit(in EffectFrame frame)
	{
		_flakes = new Flake[ResolveCount(in frame)];

		for (var i = 0; i < _flakes.Length; i++)
		{
			_flakes[i] = new Flake
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Radius = Random.Next(1.8f) + 0.6f,
				Phase = Random.Next(Tau),
				Speed = Random.Next(0.5f) + 0.5f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var gravity = (float)Gravity;
		var sway = (float)Sway;

		for (var i = 0; i < _flakes.Length; i++)
		{
			ref var flake = ref _flakes[i];

			var vy = (10f + (flake.Radius * 12f)) * flake.Speed * gravity;
			var vx = MathF.Sin((frame.Time * 0.8f) + flake.Phase) * 8f * sway;
			ApplyDirection(ref vx, ref vy);

			flake.X += vx * frame.DeltaTime;
			flake.Y += vy * frame.DeltaTime;

			if (flake.Y > frame.Height + 3)
			{
				flake.Y = -3;
				flake.X = Random.Next(frame.Width);
			}
			else if (flake.Y < -3)
			{
				// Reversed gravity or direction still recycles cleanly.
				flake.Y = frame.Height + 3;
				flake.X = Random.Next(frame.Width);
			}

			if (flake.X < -4)
			{
				flake.X = frame.Width + 4;
			}
			else if (flake.X > frame.Width + 4)
			{
				flake.X = -4;
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var flake in _flakes)
		{
			Dot(canvas, flake.X, flake.Y, flake.Radius * size, Primary, 0.4f + (0.5f * flake.Speed));
		}
	}
}

/// <summary>Slanted streaks falling fast. Drawn as lines, not dots.</summary>
[Effect("Rain", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Slanted streaks falling fast.",
	Tags = "ambient,weather,background",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Gravity")]
public sealed partial class Rain : ParticleEffect
{
	private struct Drop
	{
		public float X;
		public float Y;
		public float Length;
		public float Speed;
	}

	private Drop[] _drops = [];

	protected override int NaturalCount => 70;

	protected override void Emit(in EffectFrame frame)
	{
		_drops = new Drop[ResolveCount(in frame)];

		for (var i = 0; i < _drops.Length; i++)
		{
			_drops[i] = new Drop
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Length = Random.Next(10f) + 6f,
				Speed = Random.Next(0.5f) + 0.7f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var gravity = (float)Gravity;

		for (var i = 0; i < _drops.Length; i++)
		{
			ref var drop = ref _drops[i];

			drop.Y += 260f * drop.Speed * gravity * frame.DeltaTime;
			drop.X -= 30f * drop.Speed * frame.DeltaTime;

			if (drop.Y > frame.Height + drop.Length)
			{
				drop.Y = -drop.Length;
				drop.X = Random.Next(frame.Width + 30f);
			}
			else if (drop.Y < -drop.Length)
			{
				// Reversed gravity still recycles cleanly.
				drop.Y = frame.Height + drop.Length;
				drop.X = Random.Next(frame.Width + 30f);
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var drop in _drops)
		{
			var length = drop.Length * size;
			Line(canvas, drop.X, drop.Y, drop.X + (length * 0.12f), drop.Y - length, Primary, 0.5f, size);
		}
	}
}

/// <summary>Soft banks drifting sideways, built from stacked translucent discs.</summary>
[Effect("Fog", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Soft banks drifting sideways.",
	Tags = "ambient,soft,background",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Fog : ParticleEffect
{
	private struct Bank
	{
		public float X;
		public float Y;
		public float Radius;
		public float Speed;
		public float Phase;
	}

	private Bank[] _banks = [];

	protected override int NaturalCount => 10;

	protected override void Emit(in EffectFrame frame)
	{
		_banks = new Bank[ResolveCount(in frame)];

		for (var i = 0; i < _banks.Length; i++)
		{
			_banks[i] = new Bank
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Radius = Random.Next(25f) + 20f,
				Speed = Random.Next(6f) + 3f,
				Phase = Random.Next(Tau),
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _banks.Length; i++)
		{
			ref var bank = ref _banks[i];

			bank.X += bank.Speed * frame.DeltaTime;

			if (bank.X > frame.Width + bank.Radius)
			{
				bank.X = -bank.Radius;
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var bank in _banks)
		{
			var y = bank.Y + (MathF.Sin((frame.Time * 0.3f) + bank.Phase) * 6f);
			var radius = bank.Radius * size;

			// Four nested discs at very low alpha; the overlap is the softness.
			for (var i = 4; i > 0; i--)
			{
				Dot(canvas, bank.X, y, radius * i / 4f, Primary, 0.035f);
			}
		}
	}
}

/// <summary>A column rising from the bottom edge, spreading and thinning as it goes.</summary>
[Effect("Smoke", EffectCategory.Atmosphere,
	Cost = EffectCost.Medium,
	Description = "A column rising, spreading and thinning as it goes.",
	Tags = "ambient,soft,rising",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Smoke : ParticleEffect
{
	private struct Puff
	{
		public float X;
		public float Life;
		public float Phase;
	}

	private Puff[] _puffs = [];

	protected override int NaturalCount => 26;

	protected override void Emit(in EffectFrame frame)
	{
		_puffs = new Puff[ResolveCount(in frame)];

		for (var i = 0; i < _puffs.Length; i++)
		{
			_puffs[i] = new Puff
			{
				X = frame.Width / 2f,
				Life = Random.NextSingle(),
				Phase = Random.Next(Tau),
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _puffs.Length; i++)
		{
			ref var puff = ref _puffs[i];

			puff.Life += frame.DeltaTime * 0.25f;

			if (puff.Life > 1f)
			{
				puff.Life = 0f;
				puff.X = (frame.Width / 2f) + ((Random.NextSingle() - 0.5f) * 8f);
				puff.Phase = Random.Next(Tau);
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var puff in _puffs)
		{
			var life = puff.Life;
			var y = (frame.Height * 0.95f) - (life * frame.Height * 0.9f);
			var x = puff.X + (MathF.Sin((life * 4f) + puff.Phase) * 10f * life) + (life * life * 10f);
			var radius = (3f + (life * 22f)) * size;

			// Fades in and out across its life, so nothing pops at either end.
			var alpha = (1f - life) * life * 4f * 0.12f;

			for (var i = 3; i > 0; i--)
			{
				Dot(canvas, x, y, radius * i / 3f, Primary, alpha / 3f);
			}
		}
	}
}

/// <summary>Falling rectangles that tumble and flip, in all four palette colours.</summary>
[Effect("Confetti", EffectCategory.Atmosphere,
	Cost = EffectCost.Medium,
	Description = "Falling rectangles that tumble and flip.",
	Tags = "celebration,colour,playful",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Gravity")]
public sealed partial class Confetti : ParticleEffect
{
	private struct Piece
	{
		public float X;
		public float Y;
		public float Angle;
		public float Spin;
		public float Width;
		public float Height;
		public int Colour;
		public float Phase;
		public float Speed;
	}

	private Piece[] _pieces = [];

	protected override int NaturalCount => 60;

	protected override void Emit(in EffectFrame frame)
	{
		_pieces = new Piece[ResolveCount(in frame)];

		for (var i = 0; i < _pieces.Length; i++)
		{
			_pieces[i] = new Piece
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Angle = Random.Next(Tau),
				Spin = (Random.NextSingle() - 0.5f) * 6f,
				Width = Random.Next(3f) + 2f,
				Height = Random.Next(2f) + 1f,
				Colour = Random.NextInt(4),
				Phase = Random.Next(Tau),
				Speed = Random.Next(0.5f) + 0.6f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var gravity = (float)Gravity;

		for (var i = 0; i < _pieces.Length; i++)
		{
			ref var piece = ref _pieces[i];

			piece.Y += 40f * piece.Speed * gravity * frame.DeltaTime;
			piece.X += MathF.Sin((frame.Time * 2f) + piece.Phase) * 15f * frame.DeltaTime;
			piece.Angle += piece.Spin * frame.DeltaTime;

			if (piece.Y > frame.Height + 5)
			{
				piece.Y = -5;
				piece.X = Random.Next(frame.Width);
			}
			else if (piece.Y < -5)
			{
				// Reversed gravity still recycles cleanly.
				piece.Y = frame.Height + 5;
				piece.X = Random.Next(frame.Width);
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();
		var paint = Paint;
		paint.Style = SKPaintStyle.Fill;

		foreach (var piece in _pieces)
		{
			var width = piece.Width * size;
			var height = piece.Height * size;

			canvas.Save();
			canvas.Translate(piece.X, piece.Y);
			canvas.RotateRadians(piece.Angle);

			// The vertical squash is the piece turning edge-on; it passes
			// through zero, which is what sells the tumble.
			canvas.Scale(1f, MathF.Sin((frame.Time * 3f) + piece.Phase));

			paint.Color = WithAlpha(PaletteAt(piece.Colour), 0.9f);
			canvas.DrawRect(-width / 2f, -height / 2f, width, height, paint);
			canvas.Restore();
		}
	}
}

/// <summary>Outlined circles rising with a highlight, wobbling as they go.</summary>
[Effect("Bubbles", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Outlined circles rising with a highlight.",
	Tags = "ambient,water,rising",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Bubbles : ParticleEffect
{
	private struct Bubble
	{
		public float X;
		public float Y;
		public float Radius;
		public float Phase;
	}

	private Bubble[] _bubbles = [];

	protected override int NaturalCount => 24;

	protected override void Emit(in EffectFrame frame)
	{
		_bubbles = new Bubble[ResolveCount(in frame)];

		for (var i = 0; i < _bubbles.Length; i++)
		{
			_bubbles[i] = new Bubble
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Radius = Random.Next(4f) + 1.5f,
				Phase = Random.Next(Tau),
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _bubbles.Length; i++)
		{
			ref var bubble = ref _bubbles[i];

			bubble.Y -= (8f + (bubble.Radius * 5f)) * frame.DeltaTime;
			bubble.X += MathF.Sin((frame.Time * 2f) + bubble.Phase) * 6f * frame.DeltaTime;

			if (bubble.Y < -bubble.Radius)
			{
				bubble.Y = frame.Height + bubble.Radius;
				bubble.X = Random.Next(frame.Width);
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();
		var paint = Paint;

		foreach (var bubble in _bubbles)
		{
			var radius = bubble.Radius * size;

			paint.Style = SKPaintStyle.Stroke;
			paint.StrokeWidth = 1f;
			paint.Color = WithAlpha(Primary, 0.55f);
			canvas.DrawCircle(bubble.X, bubble.Y, radius, paint);

			Dot(canvas, bubble.X - (radius * 0.35f), bubble.Y - (radius * 0.35f), radius * 0.2f, Primary, 0.8f);
		}
	}
}

/// <summary>Spiked grains wandering on a noise field, turning slowly.</summary>
[Effect("Pollen", EffectCategory.Atmosphere,
	Cost = EffectCost.Medium,
	Description = "Spiked grains wandering on a noise field.",
	Tags = "ambient,organic,detail",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Pollen : ParticleEffect
{
	private struct Grain
	{
		public float X;
		public float Y;
		public float Phase;
		public float Radius;
	}

	private Grain[] _grains = [];

	protected override int NaturalCount => 30;

	protected override void Emit(in EffectFrame frame)
	{
		_grains = new Grain[ResolveCount(in frame)];

		for (var i = 0; i < _grains.Length; i++)
		{
			_grains[i] = new Grain
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Phase = Random.Next(Tau),
				Radius = Random.Next(1.5f) + 1.5f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _grains.Length; i++)
		{
			ref var grain = ref _grains[i];

			grain.X += (Noise.Sample(grain.Y * 0.02f, (frame.Time * 0.1f) + grain.Phase) - 0.5f) * 20f * frame.DeltaTime;
			grain.Y += ((Noise.Sample((grain.X * 0.02f) + 9f, (frame.Time * 0.1f) + grain.Phase) - 0.5f) * 20f * frame.DeltaTime)
				- (3f * frame.DeltaTime);

			Wrap(ref grain.X, ref grain.Y, in frame);
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();
		var paint = Paint;

		foreach (var grain in _grains)
		{
			var radius = grain.Radius * size;
			Dot(canvas, grain.X, grain.Y, radius, Primary, 0.7f);

			paint.Style = SKPaintStyle.Stroke;
			paint.StrokeWidth = 0.6f;
			paint.Color = WithAlpha(Primary, 0.5f);

			for (var k = 0; k < 6; k++)
			{
				var angle = (k * Tau / 6f) + (frame.Time * 0.3f) + grain.Phase;
				var cos = MathF.Cos(angle);
				var sin = MathF.Sin(angle);
				canvas.DrawLine(
					grain.X + (cos * radius), grain.Y + (sin * radius),
					grain.X + (cos * (radius + (2f * size))), grain.Y + (sin * (radius + (2f * size))),
					paint);
			}
		}
	}
}

/// <summary>Flecks blown up and to the left, recycled off whichever edge they leave.</summary>
[Effect("Ash", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Flecks blown up and to the left.",
	Tags = "ambient,drifting,background",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Ash : ParticleEffect
{
	private struct Fleck
	{
		public float X;
		public float Y;
		public float Radius;
		public float Phase;
		public float Speed;
	}

	private Fleck[] _flecks = [];

	protected override int NaturalCount => 60;

	protected override void Emit(in EffectFrame frame)
	{
		_flecks = new Fleck[ResolveCount(in frame)];

		for (var i = 0; i < _flecks.Length; i++)
		{
			_flecks[i] = new Fleck
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Radius = Random.NextSingle() + 0.4f,
				Phase = Random.Next(Tau),
				Speed = Random.Next(0.5f) + 0.5f,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _flecks.Length; i++)
		{
			ref var fleck = ref _flecks[i];

			fleck.Y -= 12f * fleck.Speed * frame.DeltaTime;
			fleck.X -= (8f + (MathF.Sin(frame.Time + fleck.Phase) * 10f)) * fleck.Speed * frame.DeltaTime;

			if (fleck.Y < -3 || fleck.X < -3)
			{
				// Re-enter from the bottom or the right, whichever keeps the
				// drift reading as a single direction.
				if (Random.NextSingle() < 0.5f)
				{
					fleck.Y = frame.Height + 3;
					fleck.X = Random.Next(frame.Width);
				}
				else
				{
					fleck.X = frame.Width + 3;
					fleck.Y = Random.Next(frame.Height);
				}
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var fleck in _flecks)
		{
			Rect(canvas, fleck.X, fleck.Y, fleck.Radius * 2f * size, fleck.Radius * 1.2f * size,
				Primary, 0.25f + (0.35f * fleck.Speed));
		}
	}
}

/// <summary>Amber grit driven hard across the surface in gusts.</summary>
[Effect("Sandstorm", EffectCategory.Atmosphere,
	Cost = EffectCost.Medium,
	Description = "Amber grit driven across the surface in gusts.",
	Tags = "weather,fast,accent",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed")]
public sealed partial class Sandstorm : ParticleEffect
{
	private struct Grit
	{
		public float X;
		public float Y;
		public float Speed;
		public float Phase;
	}

	private Grit[] _grit = [];

	protected override int NaturalCount => 150;

	protected override void Emit(in EffectFrame frame)
	{
		_grit = new Grit[ResolveCount(in frame)];

		for (var i = 0; i < _grit.Length; i++)
		{
			_grit[i] = new Grit
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Speed = Random.Next(0.6f) + 0.4f,
				Phase = Random.Next(Tau),
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		for (var i = 0; i < _grit.Length; i++)
		{
			ref var grain = ref _grit[i];

			grain.X += (120f + (MathF.Sin((frame.Time * 3f) + grain.Phase) * 40f)) * grain.Speed * frame.DeltaTime;
			grain.Y += MathF.Sin((frame.Time * 5f) + grain.Phase) * 20f * frame.DeltaTime;

			if (grain.X > frame.Width + 4)
			{
				grain.X = -4;
				grain.Y = Random.Next(frame.Height);
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();

		foreach (var grain in _grit)
		{
			Rect(canvas, grain.X, grain.Y, 3f * grain.Speed * size, 0.8f * size, Accent, 0.5f);
		}
	}
}

/// <summary>Leaves falling and turning, in the two warm palette colours.</summary>
[Effect("Leaves", EffectCategory.Atmosphere,
	Cost = EffectCost.Light,
	Description = "Leaves falling and turning.",
	Tags = "seasonal,colour,ambient",
	CommonKnobs = "Count,Speed,Size,Opacity,Seed,Gravity")]
public sealed partial class Leaves : ParticleEffect
{
	private struct Leaf
	{
		public float X;
		public float Y;
		public float Angle;
		public float Phase;
		public float Speed;
		public int Colour;
	}

	private Leaf[] _leaves = [];

	protected override int NaturalCount => 18;

	protected override void Emit(in EffectFrame frame)
	{
		_leaves = new Leaf[ResolveCount(in frame)];

		for (var i = 0; i < _leaves.Length; i++)
		{
			_leaves[i] = new Leaf
			{
				X = Random.Next(frame.Width),
				Y = Random.Next(frame.Height),
				Angle = Random.Next(Tau),
				Phase = Random.Next(Tau),
				Speed = Random.Next(0.5f) + 0.5f,
				Colour = Random.NextSingle() < 0.5f ? 1 : 3,
			};
		}
	}

	protected override void Step(in EffectFrame frame)
	{
		var gravity = (float)Gravity;

		for (var i = 0; i < _leaves.Length; i++)
		{
			ref var leaf = ref _leaves[i];

			leaf.Y += 18f * leaf.Speed * gravity * frame.DeltaTime;
			leaf.X += MathF.Sin((frame.Time * 1.5f) + leaf.Phase) * 20f * frame.DeltaTime;
			leaf.Angle += MathF.Sin(frame.Time + leaf.Phase) * 1.5f * frame.DeltaTime;

			if (leaf.Y > frame.Height + 6)
			{
				leaf.Y = -6;
				leaf.X = Random.Next(frame.Width);
			}
			else if (leaf.Y < -6)
			{
				// Reversed gravity still recycles cleanly.
				leaf.Y = frame.Height + 6;
				leaf.X = Random.Next(frame.Width);
			}
		}
	}

	protected override void Draw(SKCanvas canvas, in EffectFrame frame)
	{
		var size = ResolveSize();
		var paint = Paint;
		paint.Style = SKPaintStyle.Fill;

		foreach (var leaf in _leaves)
		{
			canvas.Save();
			canvas.Translate(leaf.X, leaf.Y);
			canvas.RotateRadians(leaf.Angle);

			paint.Color = WithAlpha(PaletteAt(leaf.Colour), 0.85f);
			canvas.DrawOval(0, 0, 4f * size, 1.8f * size, paint);
			canvas.Restore();
		}
	}
}
