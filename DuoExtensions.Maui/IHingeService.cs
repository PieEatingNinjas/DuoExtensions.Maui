using Microsoft.Maui.Controls;

namespace DuoExtensions.Maui;

/// <summary>Provides shared hinge observers for MAUI windows, independently of Shell.</summary>
public interface IHingeService
{
	/// <summary>Whether the platform supports the API; this does not detect hinge hardware.</summary>
	bool IsSupported { get; }

	/// <summary>
	/// Returns the same observer for the same window and starts monitoring automatically.
	/// Call on the window's UI thread. The observer is released when the window is destroyed.
	/// </summary>
	IHingeObserver ForWindow(Window window);
}
