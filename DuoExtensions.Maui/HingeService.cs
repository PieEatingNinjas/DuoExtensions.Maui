using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace DuoExtensions.Maui;

internal sealed class HingeService : IHingeService
{
	// Weak keys prevent the singleton from keeping closed windows and their pages alive.
	private readonly ConditionalWeakTable<Window, HingeObserver> _observers = new();

	public bool IsSupported =>
#if IOS
		OperatingSystem.IsIOSVersionAtLeast(27, 1);
#else
		false;
#endif

	public IHingeObserver ForWindow(Window window)
	{
		ArgumentNullException.ThrowIfNull(window);
		if (window.Dispatcher.IsDispatchRequired)
			throw new InvalidOperationException("Request the hinge observer on the window's UI thread.");

		var observer = _observers.GetValue(window, static _ => new HingeObserver());
		if (observer.IsDisposed)
			throw new ObjectDisposedException(nameof(window), "The window's hinge observer has already been destroyed.");

		observer.Attach(window);
		return observer;
	}
}
