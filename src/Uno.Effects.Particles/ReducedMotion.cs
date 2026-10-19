using Windows.UI.ViewManagement;

namespace Uno.Effects.Particles;

/// <summary>
/// Reads the system reduced-motion preference once, so honouring it costs
/// nothing per frame. Ambient motion is exactly the kind of thing the setting
/// exists for, so <see cref="ParticleView"/> honours it by default.
/// </summary>
internal static class ReducedMotion
{
	private static bool? _isEnabled;

	public static bool IsEnabled => _isEnabled ??= Read();

	private static bool Read()
	{
		try
		{
			return !new UISettings().AnimationsEnabled;
		}
		catch (Exception)
		{
			// Not every head exposes UISettings; motion stays on when unknown.
			return false;
		}
	}
}
