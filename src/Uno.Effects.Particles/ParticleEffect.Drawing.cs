using SkiaSharp;

namespace Uno.Effects.Particles;

/// <summary>
/// The small drawing vocabulary every effect shares, ported from the study
/// sheet so the C# effects render the shapes the design was signed off on.
/// </summary>
public abstract partial class ParticleEffect
{
	/// <summary>Two pi.</summary>
	protected const float Tau = MathF.PI * 2f;

	private SKPaint? _paint;

	/// <summary>
	/// A single antialiased paint reused for the whole effect. Effects mutate
	/// its colour and style rather than allocating, which is what keeps the
	/// per-frame allocation budget at zero.
	/// </summary>
	protected SKPaint Paint => _paint ??= new SKPaint { IsAntialias = true };

	/// <summary>Fills a circle in the given colour at the given alpha.</summary>
	protected void Dot(SKCanvas canvas, float x, float y, float radius, SKColor color, float alpha)
	{
		var paint = Paint;
		paint.Style = SKPaintStyle.Fill;
		paint.Color = WithAlpha(color, alpha);
		canvas.DrawCircle(x, y, radius, paint);
	}

	/// <summary>A dot wrapped in two softer haloes, as used for light effects.</summary>
	protected void Glow(SKCanvas canvas, float x, float y, float radius, SKColor color, float alpha)
	{
		Dot(canvas, x, y, radius * 3.2f, color, alpha * 0.08f);
		Dot(canvas, x, y, radius * 1.9f, color, alpha * 0.2f);
		Dot(canvas, x, y, radius, color, alpha);
	}

	/// <summary>Strokes a line in the given colour at the given alpha.</summary>
	protected void Line(SKCanvas canvas, float x0, float y0, float x1, float y1, SKColor color, float alpha, float width = 1f)
	{
		var paint = Paint;
		paint.Style = SKPaintStyle.Stroke;
		paint.StrokeWidth = width;
		paint.Color = WithAlpha(color, alpha);
		canvas.DrawLine(x0, y0, x1, y1, paint);
	}

	/// <summary>Fills an axis-aligned rectangle.</summary>
	protected void Rect(SKCanvas canvas, float x, float y, float width, float height, SKColor color, float alpha)
	{
		var paint = Paint;
		paint.Style = SKPaintStyle.Fill;
		paint.Color = WithAlpha(color, alpha);
		canvas.DrawRect(x, y, width, height, paint);
	}

	/// <summary>
	/// Rotates a velocity by <see cref="Direction"/>. Direction is an offset
	/// from the motion the effect was designed with, so 0 always means
	/// "as designed".
	/// </summary>
	protected void ApplyDirection(ref float vx, ref float vy)
	{
		if (Direction == 0)
		{
			return;
		}

		var angle = (float)(Direction * Math.PI / 180.0);
		var cos = MathF.Cos(angle);
		var sin = MathF.Sin(angle);
		(vx, vy) = (vx * cos - vy * sin, vx * sin + vy * cos);
	}

	/// <summary>Wraps a particle around both edges, with a small margin.</summary>
	protected static void Wrap(ref float x, ref float y, in EffectFrame frame, float margin = 4f)
	{
		if (x < -margin)
		{
			x = frame.Width + margin;
		}
		else if (x > frame.Width + margin)
		{
			x = -margin;
		}

		if (y < -margin)
		{
			y = frame.Height + margin;
		}
		else if (y > frame.Height + margin)
		{
			y = -margin;
		}
	}
}
