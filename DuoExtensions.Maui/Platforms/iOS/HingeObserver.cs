using Microsoft.Maui.Controls;
using UIKit;

namespace DuoExtensions.Maui;

internal sealed partial class HingeObserver
{
#pragma warning disable CA1416 // Native calls are guarded by the iOS 27.1 check; this file is compiled only for iOS.
	private UIHingeInteraction? _interaction;
	private UIView? _nativeView;

	partial void AttachToNativeWindow(Window window)
	{
		if (!OperatingSystem.IsIOSVersionAtLeast(27, 1) || window.Handler is null)
			return;

		if (window.Handler.PlatformView is not UIView view)
			throw new InvalidOperationException("The MAUI window handler must expose a UIKit view for hinge monitoring.");

		if (_nativeView == view)
			return;

		DetachFromNativeWindow();
		_nativeView = view;
		// UIKit retains the callback; a weak reference avoids retaining the service through the view.
		var weakService = new WeakReference<HingeObserver>(this);
		var interaction = new UIHingeInteraction((nativeInteraction, update) =>
		{
			if (!weakService.TryGetTarget(out var service) || service._interaction != nativeInteraction)
				return;

			// Null is a real availability update, not a reason to keep the previous reading.
			var hinge = update.Hinge;
			service.PublishState(hinge is null ? null : new HingeState(
				hinge.Status switch
				{
					UIHingeStatus.Closed => HingeStatus.Closed,
					UIHingeStatus.PartiallyOpen => HingeStatus.PartiallyOpen,
					UIHingeStatus.FullyOpen => HingeStatus.FullyOpen,
					_ => HingeStatus.Unknown
				},
				(double)hinge.Angle * 180.0 / Math.PI));
		});

		_interaction = interaction;
		// Adding can immediately publish a reading; a subscriber may detach or dispose in response.
		interaction.Enabled = true;
		view.AddInteraction(interaction);
	}

	partial void DetachFromNativeWindow()
	{
		if (_interaction is not { } interaction)
			return;

		// Invalidate the callback first, since removal can itself deliver an unavailable update.
		_interaction = null;
		interaction.Enabled = false;
		_nativeView?.RemoveInteraction(interaction);
		_nativeView = null;
		interaction.Dispose();
	}
#pragma warning restore CA1416
}
