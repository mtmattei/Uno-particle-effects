using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.Effects.Gallery.Catalog;
using Uno.Effects.Particles;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.Effects.Gallery;

/// <summary>
/// One effect at full size, with every knob it declares, and the code that
/// reproduces exactly what is on screen.
/// </summary>
public sealed partial class DetailPage : Page
{
	private enum CodeTab
	{
		Xaml,
		CSharp,
		Sksl,
	}

	private readonly DispatcherTimer _readout = new() { Interval = TimeSpan.FromMilliseconds(500) };

	private CatalogEntry _entry = null!;
	private ParticleEffect _effect = null!;
	private ParticleView _view = null!;
	private CodeTab _tab = CodeTab.Xaml;
	private bool _lightSurface;

	public DetailPage()
	{
		InitializeComponent();

		BackButton.Click += (_, _) =>
		{
			if (Frame.CanGoBack)
			{
				Frame.GoBack();
			}
		};

		PlayButton.Click += OnTogglePlay;
		SurfaceButton.Click += OnToggleSurface;
		DiagnosticsButton.Click += OnToggleDiagnostics;
		ResetButton.Click += OnReset;
		CopyButton.Click += (_, _) => CopyToClipboard(CodeBlock.Text);
		CopyInstallButton.Click += (_, _) => CopyToClipboard(CodeGenerator.InstallCommand);

		_readout.Tick += (_, _) => UpdateReadout();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		var id = e.Parameter as string ?? EffectCatalog.Entries[0].Id;
		_entry = EffectCatalog.Find(id) ?? EffectCatalog.Entries[0];
		_effect = _entry.Create();

		_view = new ParticleView
		{
			Surface = (Brush)Application.Current.Resources["TileSurfaceBrush"],
			Interactive = true,
		};
		_view.Effects.Add(_effect);
		PreviewFrame.Child = _view;

		NumberLabel.Text = _entry.Number;
		NameLabel.Text = _entry.Name;
		DescriptionLabel.Text = _entry.Description;
		InstallLabel.Text = CodeGenerator.InstallCommand;

		MatrixNote.Text =
			"Rendered by the Uno Skia renderer, so the same pixels are produced on Windows, "
			+ "WebAssembly, Android, iOS, macOS and Linux from this one code path. The measured "
			+ "per-platform frame times that belong here come from the benchmark head in Phase 2.";

		BuildMeta();
		BuildKnobs();
		BuildTabs();
		UpdateCode();

		_readout.Start();
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		_readout.Stop();

		_view.Effects.Clear();
		_effect.Dispose();
	}

	private void BuildMeta()
	{
		MetaHost.Children.Clear();

		Add(_entry.Category.ToString().ToUpperInvariant());
		Add(_entry.Cost.ToString().ToUpperInvariant() + " COST");

		if (_entry.IsShaderBased)
		{
			Add("SKSL SHADER");
		}

		if (_entry.RespondsToPointer)
		{
			Add("INTERACTIVE");
		}

		void Add(string text) => MetaHost.Children.Add(new TextBlock
		{
			Text = text,
			Style = (Style)Application.Current.Resources["EyebrowStyle"],
		});
	}

	private void BuildKnobs()
	{
		KnobHost.Children.Clear();

		foreach (var knob in _entry.Knobs)
		{
			var label = new TextBlock
			{
				Text = knob.DisplayName,
				Style = (Style)Application.Current.Resources["CaptionStyle"],
			};

			var value = new TextBlock
			{
				Text = knob.Display(_effect),
				Style = (Style)Application.Current.Resources["CodeStyle"],
				Foreground = (Brush)Application.Current.Resources["MutedBrush"],
				HorizontalAlignment = HorizontalAlignment.Right,
			};

			var slider = new Slider
			{
				Minimum = knob.Minimum,
				Maximum = knob.Maximum,
				StepFrequency = knob.Step,
				Value = knob.Read(_effect),
				Margin = new Thickness(0, -4, 0, 0),
				Foreground = (Brush)Application.Current.Resources["InkBrush"],
			};

			slider.ValueChanged += (_, e) =>
			{
				knob.Write(_effect, e.NewValue);
				value.Text = knob.Display(_effect);
				UpdateCode();
			};

			var header = new Grid();
			header.Children.Add(label);
			header.Children.Add(value);

			KnobHost.Children.Add(new StackPanel
			{
				Children = { header, slider },
			});
		}
	}

	private void BuildTabs()
	{
		TabHost.Items.Clear();

		Add("XAML", CodeTab.Xaml);
		Add("C#", CodeTab.CSharp);

		if (_entry.IsShaderBased)
		{
			Add("SKSL", CodeTab.Sksl);
		}

		void Add(string label, CodeTab tab)
		{
			var button = new ToggleButton
			{
				Content = label,
				Style = (Style)Application.Current.Resources["ChipStyle"],
				IsChecked = tab == _tab,
				Tag = tab,
			};

			button.Checked += (s, _) =>
			{
				_tab = (CodeTab)((ToggleButton)s).Tag;

				foreach (var other in TabHost.Items.OfType<ToggleButton>())
				{
					if (!ReferenceEquals(other, s) && other.IsChecked == true)
					{
						other.IsChecked = false;
					}
				}

				UpdateCode();
			};

			button.Unchecked += (s, _) =>
			{
				if ((CodeTab)((ToggleButton)s).Tag == _tab)
				{
					((ToggleButton)s).IsChecked = true;
				}
			};

			TabHost.Items.Add(button);
		}
	}

	private void UpdateCode() => CodeBlock.Text = _tab switch
	{
		CodeTab.CSharp => CodeGenerator.ToCSharp(_entry, _effect),
		CodeTab.Sksl => _entry.ShaderSource ?? string.Empty,
		_ => CodeGenerator.ToXaml(_entry, _effect),
	};

	private void UpdateReadout()
	{
		if (_view.Paused)
		{
			FpsLabel.Text = "PAUSED";
			return;
		}

		var particles = _view.ParticleCount > 0 ? $"{_view.ParticleCount} PARTICLES · " : string.Empty;
		FpsLabel.Text = $"{_view.FramesPerSecond:0} FPS · {particles}{_view.DrawMilliseconds:0.00} MS";
	}

	private void OnTogglePlay(object sender, RoutedEventArgs e)
	{
		_view.Paused = !_view.Paused;
		PlayButton.Content = _view.Paused ? "Play" : "Pause";
		UpdateReadout();
	}

	private void OnToggleSurface(object sender, RoutedEventArgs e)
	{
		_lightSurface = !_lightSurface;

		var key = _lightSurface ? "LightSurfaceBrush" : "TileSurfaceBrush";
		var brush = (Brush)Application.Current.Resources[key];

		_view.Surface = brush;
		PreviewFrame.Background = brush;
		SurfaceButton.Content = _lightSurface ? "Dark surface" : "Light surface";

		ApplyPalette();
	}

	/// <summary>
	/// On a light surface the paper-white ink would vanish, so the palette flips
	/// with it. This is the palette-as-brushes story in miniature: no effect
	/// knows a theme changed.
	/// </summary>
	private void ApplyPalette()
	{
		_effect.Palette.Clear();

		if (_lightSurface)
		{
			_effect.Palette.Add(new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x2A, 0x2A, 0x28)));
			_effect.Palette.Add(new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xA8, 0x72, 0x38)));
		}
	}

	private void OnToggleDiagnostics(object sender, RoutedEventArgs e) =>
		_view.ShowDiagnostics = !_view.ShowDiagnostics;

	private void OnReset(object sender, RoutedEventArgs e)
	{
		var replaced = _effect;
		_effect = _entry.Create();
		ApplyPalette();

		_view.Effects.Clear();
		_view.Effects.Add(_effect);

		BuildKnobs();
		UpdateCode();

		replaced.Dispose();
	}

	private static void CopyToClipboard(string text)
	{
		try
		{
			var package = new DataPackage();
			package.SetText(text);
			Clipboard.SetContent(package);
		}
		catch (Exception)
		{
			// Clipboard access is not guaranteed on every head; copying is a
			// convenience, never the only way to get the code.
		}
	}
}
