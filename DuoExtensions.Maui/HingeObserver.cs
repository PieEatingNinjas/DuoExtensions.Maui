using Microsoft.Maui.Controls;

namespace DuoExtensions.Maui;

internal sealed partial class HingeObserver : IHingeObserver
{
	private Window? _window;
	private bool _disposed;
	internal bool IsDisposed => _disposed;

	public bool IsSupported =>
#if IOS
		OperatingSystem.IsIOSVersionAtLeast(27, 1);
#else
		false;
#endif

	public HingeState? CurrentState { get; private set; }

	public event EventHandler<HingeStateChangedEventArgs>? StateChanged;

	internal void Attach(Window window)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentNullException.ThrowIfNull(window);
		EnsureUIThread(window);
		if (_window == window)
			return;

		Detach();
		_window = window;
		// Watch both initial creation and handler replacement; Created alone may already have fired.
		window.HandlerChanging += OnHandlerChanging;
		window.HandlerChanged += OnHandlerChanged;
		window.Created += OnWindowCreated;
		window.Destroying += OnWindowDestroying;
		AttachToNativeWindow(window);
	}

	private void Detach()
	{
		if (_window is { } window)
		{
			EnsureUIThread(window);
			window.HandlerChanging -= OnHandlerChanging;
			window.HandlerChanged -= OnHandlerChanged;
			window.Created -= OnWindowCreated;
			window.Destroying -= OnWindowDestroying;
			_window = null;
		}

		DetachFromNativeWindow();
		PublishState(null);
	}

	private void Dispose()
	{
		if (_disposed)
			return;

		// Check before changing state so an off-thread call does not leave a half-disposed observer.
		if (_window is { } window)
			EnsureUIThread(window);

		_disposed = true;
		Detach();
		StateChanged = null;
	}

	private void OnHandlerChanging(object? sender, HandlerChangingEventArgs e)
	{
		// Remove the interaction from the old hierarchy before MAUI replaces the handler.
		DetachFromNativeWindow();
		PublishState(null);
	}

	private void OnHandlerChanged(object? sender, EventArgs e)
	{
		if (_window is { } window)
			AttachToNativeWindow(window);
	}

	private void OnWindowCreated(object? sender, EventArgs e) => OnHandlerChanged(sender, e);

	private void OnWindowDestroying(object? sender, EventArgs e) => Dispose();

	private void PublishState(HingeState? state)
	{
		if (CurrentState == state)
			return;

		CurrentState = state;
		StateChanged?.Invoke(this, new HingeStateChangedEventArgs(state));
	}

	private static void EnsureUIThread(Window window)
	{
		if (window.Dispatcher.IsDispatchRequired)
			throw new InvalidOperationException("Hinge monitoring must be configured on the window's UI thread.");
	}

	// Shared targets keep the public API available without referencing UIKit.
	partial void AttachToNativeWindow(Window window);
	partial void DetachFromNativeWindow();
}
