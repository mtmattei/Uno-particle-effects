using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Effects.Gallery.Catalog;
using Uno.Effects.Particles;

namespace Uno.Effects.Gallery;

/// <summary>
/// The board: every catalog effect rendered by the real
/// <see cref="ParticleView"/>, frozen on a settled frame until it is hovered.
/// </summary>
public sealed partial class GridPage : Page
{
	// Eight across is the study sheet's own board; narrower windows step down
	// rather than scrolling sideways.
	private const int MinimumColumns = 2;
	private const int MaximumColumns = 8;
	private const double TargetTileWidth = 165;

	private readonly List<Filter> _filters = [];
	private Filter _active;
	private int _columns = -1;

	public GridPage()
	{
		InitializeComponent();

		_active = Filter.All;
		BuildChips();

		SizeChanged += (_, e) => Layout(e.NewSize.Width);
		Loaded += (_, _) => Layout(ActualWidth);
	}

	private void Layout(double width)
	{
		// The board reflows rather than scrolling sideways: the study sheet is
		// eight across, this is as many as fit.
		var available = Math.Max(0, width - 112);
		var columns = Math.Clamp((int)(available / TargetTileWidth), MinimumColumns, MaximumColumns);

		if (columns == _columns)
		{
			return;
		}

		_columns = columns;
		Rebuild();
	}

	private void BuildChips()
	{
		_filters.Clear();
		_filters.Add(Filter.All);

		foreach (var category in Enum.GetValues<EffectCategory>())
		{
			_filters.Add(Filter.ForCategory(category));
		}

		_filters.Add(Filter.Shader);

		foreach (var cost in Enum.GetValues<EffectCost>())
		{
			_filters.Add(Filter.ForCost(cost));
		}

		foreach (var filter in _filters)
		{
			var chip = new ToggleButton
			{
				Content = filter.Label,
				Style = (Style)Application.Current.Resources["ChipStyle"],
				IsChecked = filter == _active,
				Tag = filter,
			};

			chip.Checked += OnChipChecked;
			chip.Unchecked += OnChipUnchecked;
			ChipHost.Items.Add(chip);
		}
	}

	private void OnChipChecked(object sender, RoutedEventArgs e)
	{
		var chip = (ToggleButton)sender;
		_active = (Filter)chip.Tag;

		foreach (var other in ChipHost.Items.OfType<ToggleButton>())
		{
			if (!ReferenceEquals(other, chip) && other.IsChecked == true)
			{
				other.IsChecked = false;
			}
		}

		Rebuild();
	}

	private void OnChipUnchecked(object sender, RoutedEventArgs e)
	{
		// A chip cannot be turned off, only replaced: there is always a filter.
		var chip = (ToggleButton)sender;
		if ((Filter)chip.Tag == _active)
		{
			chip.IsChecked = true;
		}
	}

	private void Rebuild()
	{
		if (_columns <= 0)
		{
			return;
		}

		TileGrid.Children.Clear();
		TileGrid.ColumnDefinitions.Clear();
		TileGrid.RowDefinitions.Clear();

		var entries = EffectCatalog.Entries.Where(_active.Matches).ToList();

		CountLabel.Text = entries.Count == 1 ? "1 STUDY" : $"{entries.Count} STUDIES";
		EmptyLabel.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

		for (var i = 0; i < _columns; i++)
		{
			TileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}

		var rows = (entries.Count + _columns - 1) / _columns;
		for (var i = 0; i < rows; i++)
		{
			TileGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		}

		for (var i = 0; i < entries.Count; i++)
		{
			var tile = new EffectTile(entries[i]);
			var id = entries[i].Id;
			tile.Click += (_, _) => Frame?.Navigate(typeof(DetailPage), id);

			Grid.SetColumn(tile, i % _columns);
			Grid.SetRow(tile, i / _columns);
			TileGrid.Children.Add(tile);
		}
	}

	/// <summary>
	/// The filter behind one chip. Category, shader-based and cost all reduce to
	/// the same shape, so the chip row is one list rather than three.
	/// </summary>
	private sealed class Filter
	{
		private readonly Func<CatalogEntry, bool> _predicate;

		private Filter(string label, Func<CatalogEntry, bool> predicate)
		{
			Label = label;
			_predicate = predicate;
		}

		public string Label { get; }

		public static Filter All { get; } = new("All", _ => true);

		public static Filter Shader { get; } = new("Shader", e => e.IsShaderBased);

		public static Filter ForCategory(EffectCategory category) =>
			new(category.ToString(), e => e.Category == category);

		public static Filter ForCost(EffectCost cost) =>
			new(cost.ToString().ToLowerInvariant() + " cost", e => e.Cost == cost);

		public bool Matches(CatalogEntry entry) => _predicate(entry);
	}
}
