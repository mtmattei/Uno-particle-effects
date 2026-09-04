using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Effects.Gallery.Catalog;
using Uno.Effects.Particles;

namespace Uno.Effects.Gallery;

/// <summary>
/// One square of the board: a live <see cref="ParticleView"/> above its number
/// and name.
/// </summary>
/// <remarks>
/// Idle tiles hold a settled frame rather than running, so a board of them
/// costs nothing until the pointer arrives. That is the whole reason
/// <see cref="ParticleView.WarmUpFrames"/> exists.
/// </remarks>
public sealed partial class EffectTile : Button
{
	private readonly ParticleView _view;

	public EffectTile(CatalogEntry entry)
	{
		Entry = entry;

		_view = new ParticleView
		{
			Surface = (Brush)Application.Current.Resources["TileSurfaceBrush"],
			Interactive = entry.RespondsToPointer,
			Paused = true,
		};

		_view.Effects.Add(entry.Create());

		var frame = new Border
		{
			CornerRadius = new CornerRadius(3),
			Background = (Brush)Application.Current.Resources["TileSurfaceBrush"],
			Child = _view,
		};

		// A square tile without binding to ActualWidth, which does not raise
		// change notifications everywhere.
		frame.SizeChanged += (_, e) => frame.Height = e.NewSize.Width;

		var number = new TextBlock
		{
			Text = entry.Number,
			Style = (Style)Application.Current.Resources["CaptionStyle"],
			Foreground = (Brush)Application.Current.Resources["MutedBrush"],
		};

		var name = new TextBlock
		{
			Text = entry.Name,
			Style = (Style)Application.Current.Resources["CaptionStyle"],
			TextTrimming = TextTrimming.CharacterEllipsis,
		};

		var caption = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Children = { number, name },
		};

		Style = (Style)Application.Current.Resources["TileButtonStyle"];
		Content = new StackPanel
		{
			Spacing = 9,
			Children = { frame, caption },
		};

		AutomationProperties.SetName(this, $"{entry.Number} {entry.Name}");
		PointerEntered += OnPointerEntered;
		PointerExited += OnPointerExited;
	}

	public CatalogEntry Entry { get; }

	private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		_view.Paused = false;
		ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
	}

	private void OnPointerExited(object sender, PointerRoutedEventArgs e) => _view.Paused = true;
}
