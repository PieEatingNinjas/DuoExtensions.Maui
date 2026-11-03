namespace DuoExtensions.Maui;

/// <summary>A window's hinge readings. Its lifetime is managed by IHingeService.</summary>
public interface IHingeObserver
{
	/// <summary>Whether the OS supports the API, not whether hinge hardware is present.</summary>
	bool IsSupported { get; }

	/// <summary>The latest reading, or null before a reading or when data is unavailable.</summary>
	HingeState? CurrentState { get; }

	/// <summary>Raised on the window's UI thread, including when data becomes unavailable.</summary>
	event EventHandler<HingeStateChangedEventArgs>? StateChanged;
}
