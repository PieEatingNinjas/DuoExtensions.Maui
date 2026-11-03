using System.Runtime.Versioning;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using UIKit;

namespace DuoExtensions.Maui;

// ShellRenderer is itself a UIViewController, so it can supply UIKit layout preferences.
internal class ToolbarLayoutShellRenderer : ShellRenderer
{
#pragma warning disable CA1416 // iOS-only renderer; new APIs are called by iOS 27.1 or guarded below.
	// UIKit queries this override; the attached property stores the Shell-wide choice.
	[SupportedOSPlatform("ios27.1")]
	public override UIVerticalBarBehavior PreferredVerticalBarBehavior =>
		Shell is { } shell
			&& ToolbarLayout.GetVerticalBehavior(shell) == ToolbarVerticalBehavior.Disabled
			? UIVerticalBarBehavior.Disabled
			: UIVerticalBarBehavior.Automatic;

	// Null means "use my own preference": do not delegate the decision to a page.
	[SupportedOSPlatform("ios27.1")]
	public override UIViewController? ChildViewControllerForPreferredVerticalBarBehavior => null;

	// Reuse MAUI's page tracking to apply compression and configure Shell-created buttons.
	protected override IShellPageRendererTracker CreatePageRendererTracker() =>
		new ToolbarLayoutPageRendererTracker(this);

	// Replace Shell's outer controller so it forwards preference lookup to this renderer.
	protected override IShellFlyoutRenderer CreateFlyoutRenderer() =>
		new ToolbarLayoutFlyoutRenderer
		{
			FlyoutTransition = new SlideFlyoutTransition() // Preserve MAUI's default menu animation.
		};
}
#pragma warning restore CA1416

internal sealed class ToolbarLayoutFlyoutRenderer : ShellFlyoutRenderer
{
	// This is Shell's outer/root controller; ask the inner Shell renderer for the preference.
	[SupportedOSPlatform("ios27.1")]
	public override UIViewController? ChildViewControllerForPreferredVerticalBarBehavior =>
		ChildViewControllers.OfType<ToolbarLayoutShellRenderer>().FirstOrDefault();
}

internal sealed class ToolbarLayoutPageRendererTracker : ShellPageRendererTracker
{
	// Cache matches without keeping replaced UIKit buttons alive.
	private readonly Dictionary<string, WeakReference<UIBarButtonItem>> _nativeItems = new();
	private readonly HashSet<ToolbarItem> _observedToolbarItems = [];
	private UIView? _edgeView;
	private IUITraitChangeRegistration? _edgeRegistration;

    public ToolbarLayoutPageRendererTracker(IShellContext context)
		: base(context)
	{
	}

    protected override void OnPageSet(Page oldPage, Page newPage)
    {
        StopObservingVerticalEdge();
        // Stop observing the old page's buttons before tracking the replacement page.
        SyncObservedToolbarItems(null);
        _nativeItems.Clear();
        base.OnPageSet(oldPage, newPage);
        SyncObservedToolbarItems(
            OperatingSystem.IsIOSVersionAtLeast(27, 1) ? newPage : null);
        UpdateCompressionBehavior();
        ObserveVerticalEdge();
    }

	protected override void OnRendererSet()
	{
		base.OnRendererSet();
		// The page may be assigned before its native controller becomes available.
		UpdateCompressionBehavior();
		StopObservingVerticalEdge();
		ObserveVerticalEdge();
	}

	private void ObserveVerticalEdge()
	{
#pragma warning disable CA1416 // iOS-only integration guarded by the runtime version check.
		if (!OperatingSystem.IsIOSVersionAtLeast(27, 1)
			|| Page is not { } page || ViewController?.View is not { } view)
			return;

		_edgeView = view;
		var weakPage = new WeakReference<Page>(page);
		// Observe this page, not Shell's root: cached tabs can change traits while offscreen.
		_edgeRegistration = view.RegisterForTraitChanges(
			UITraitCollection.SystemTraitsAffectingVerticalBarEdge,
			(environment, _) =>
			{
				if (weakPage.TryGetTarget(out var observedPage))
					UpdateVerticalEdge(observedPage, environment.TraitCollection);
			});
		UpdateVerticalEdge(page, view.TraitCollection);
#pragma warning restore CA1416
	}

	private void StopObservingVerticalEdge()
	{
		if (OperatingSystem.IsIOSVersionAtLeast(27, 1) && _edgeRegistration is not null)
			_edgeView?.UnregisterForTraitChanges(_edgeRegistration);
		_edgeRegistration = null;
		_edgeView = null;
	}

	[SupportedOSPlatform("ios27.1")]
	private static void UpdateVerticalEdge(Page page, UITraitCollection traits)
	{
#pragma warning disable CA1416 // iOS-only integration.
		ToolbarLayout.UpdateVerticalEdge(page, traits.VerticalBarEdge switch
		{
			UIVerticalBarEdge.Leading => ToolbarVerticalEdge.Leading,
			UIVerticalBarEdge.Trailing => ToolbarVerticalEdge.Trailing,
			_ => ToolbarVerticalEdge.Unspecified
		});
#pragma warning restore CA1416
	}

	protected override void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		base.OnPagePropertyChanged(sender, e);
		if (e.PropertyName == ToolbarLayout.CompressionBehaviorProperty.PropertyName)
		{
			// The base tracker already observes the page, including later attached-property changes.
			UpdateCompressionBehavior();
		}
	}

	private void UpdateCompressionBehavior()
	{
#pragma warning disable CA1416 // Guarded iOS-only integration; IsIOSVersionAtLeast also includes Mac Catalyst in the analyzer.
		if (OperatingSystem.IsIOSVersionAtLeast(27, 1)
			&& Page is { } page && ViewController is { } controller)
		{
			// Compression belongs to the page's navigation item, not an individual button.
			controller.NavigationItem.VerticalBarCompressionBehavior =
				ToolbarLayout.GetCompressionBehavior(page) switch
				{
					ToolbarVerticalCompressionBehavior.PrefersToolbarItems =>
						UIVerticalBarCompressionBehavior.PrefersBarItems,
					ToolbarVerticalCompressionBehavior.PrefersTabBar =>
						UIVerticalBarCompressionBehavior.PrefersTabBar,
					_ => UIVerticalBarCompressionBehavior.Automatic
				};
		}
#pragma warning restore CA1416
	}

	protected override void UpdateToolbarItems()
	{
        if (!OperatingSystem.IsIOSVersionAtLeast(27, 1))
		{
            base.UpdateToolbarItems();
			return;
		}

        if (Page is { } currentPage)
        {
            // Assign identifiers before MAUI creates the corresponding native buttons.
            SyncObservedToolbarItems(currentPage);
        }

        // Let Shell create/rebuild its buttons before looking them up.
        base.UpdateToolbarItems();
        UpdateCompressionBehavior();

        if (Page is not { } page)
		{
			return;
		}

        // Only explicitly configured items opt into native axis updates.
        var configuredItems = page.ToolbarItems
            .Where(item => item.IsSet(ToolbarItemAxisBehavior.AxisBehaviorProperty))
            .ToArray();
            
        if (configuredItems.Length == 0)
        {
            return;
        }

        if (page.Handler?.PlatformView is not UIView platformView
			|| platformView.Window?.RootViewController is not UIViewController rootViewController)
        {
            return;
        }

        // ToolbarItem has no view handler exposing Shell's existing UIBarButtonItem.
        var nativeItems = EnumerateViewControllers(rootViewController)
			.SelectMany(viewController =>
				(viewController.NavigationItem.LeftBarButtonItems ?? [])
				.Concat(viewController.NavigationItem.RightBarButtonItems ?? []))
			.ToArray();

        // A toolbar rebuild may have replaced previously matched native instances.
        _nativeItems.Clear();

        foreach (var toolbarItem in configuredItems)
		{
            if (string.IsNullOrEmpty(toolbarItem.AutomationId))
			{
				continue;
			}

            // MAUI copies AutomationId to the native accessibility identifier.
            var nativeItem = nativeItems.FirstOrDefault(item =>
                item.AccessibilityIdentifier == toolbarItem.AutomationId);

            if (nativeItem is null)
            {
                continue;
            }

            _nativeItems[toolbarItem.AutomationId] = new WeakReference<UIBarButtonItem>(nativeItem);

            // MAUI omits the native title for icon items; UIKit needs it in overflow.
            nativeItem.Title = toolbarItem.Text;
            SetAxisBehavior(nativeItem, ToolbarItemAxisBehavior.GetAxisBehavior(toolbarItem));
        }
    }

    // Search Shell's native hierarchy, including navigation, tabs and presented controllers.
    private IEnumerable<UIViewController> EnumerateViewControllers(UIViewController viewController)
    {
        yield return viewController;

        if (viewController is UINavigationController navigationController)
        {
            foreach (var child in navigationController.ViewControllers ?? [])
            {
                foreach (var nested in EnumerateViewControllers(child))
                {
                    yield return nested;
                }
            }
        }

        if (viewController is UITabBarController tabBarController)
        {
            foreach (var child in tabBarController.ViewControllers ?? [])
            {
                foreach (var nested in EnumerateViewControllers(child))
                {
                    yield return nested;
                }
            }
        }

        foreach (var child in viewController.ChildViewControllers)
        {
            foreach (var nested in EnumerateViewControllers(child))
            {
                yield return nested;
            }
        }

        if (viewController.PresentedViewController is { } presentedViewController)
        {
            foreach (var nested in EnumerateViewControllers(presentedViewController))
            {
                yield return nested;
            }
        }
    }

    private void SyncObservedToolbarItems(Page? page)
    {
        // Unset items stay unobserved; setting a first value later needs another toolbar update.
        var currentItems = page?.ToolbarItems
            .Where(item => item.IsSet(ToolbarItemAxisBehavior.AxisBehaviorProperty))
            .ToHashSet() ?? [];

        foreach (var item in currentItems)
        {
            EnsureAutomationId(item);
        }

        // Detach removed items so they cannot keep triggering updates for this page.
        foreach (var item in _observedToolbarItems.Except(currentItems).ToArray())
        {
            item.PropertyChanged -= OnToolbarItemPropertyChanged;
            _observedToolbarItems.Remove(item);
        }

        foreach (var item in currentItems.Except(_observedToolbarItems))
        {
            item.PropertyChanged += OnToolbarItemPropertyChanged;
            _observedToolbarItems.Add(item);
        }
    }

    private static void EnsureAutomationId(ToolbarItem toolbarItem)
    {
        if (string.IsNullOrWhiteSpace(toolbarItem.AutomationId))
        {
            // Preserve caller-supplied IDs; generate one only when matching requires it.
            toolbarItem.AutomationId = $"ToolbarAxis_{Guid.NewGuid():N}";
        }
    }

    // Translate the shared enum to UIKit without recreating the native button.
    [SupportedOSPlatform("ios27.1")]
    private void SetAxisBehavior(UIBarButtonItem nativeItem, ToolbarAxisBehavior behavior)
    {
#pragma warning disable CA1416
        nativeItem.AxisBehavior = behavior switch
        {
            ToolbarAxisBehavior.HorizontalOnly => UIBarButtonItemAxisBehavior.HorizontalOnly,
            ToolbarAxisBehavior.VerticalPreferred => UIBarButtonItemAxisBehavior.VerticalPreferred,
            _ => UIBarButtonItemAxisBehavior.Automatic
        };
#pragma warning restore CA1416
    }

    private void OnToolbarItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName != ToolbarItemAxisBehavior.AxisBehaviorProperty.PropertyName
                && e.PropertyName != MenuItem.TextProperty.PropertyName)
            || sender is not ToolbarItem item)
        {
            return;
        }

        if (string.IsNullOrEmpty(item.AutomationId)
            || !_nativeItems.TryGetValue(item.AutomationId, out var weakReference)
            || !weakReference.TryGetTarget(out var nativeItem))
        {
            return;
        }

        if (!OperatingSystem.IsIOSVersionAtLeast(27, 1))
        {
            return;
        }

        if (e.PropertyName == MenuItem.TextProperty.PropertyName)
        {
            nativeItem.Title = item.Text;
            return;
        }

        // Clearing the attached property restores UIKit's automatic behavior.
        var behavior = item.IsSet(ToolbarItemAxisBehavior.AxisBehaviorProperty)
            ? ToolbarItemAxisBehavior.GetAxisBehavior(item)
            : ToolbarAxisBehavior.Automatic;
        SetAxisBehavior(nativeItem, behavior);

        if (behavior == ToolbarAxisBehavior.Automatic
            && !item.IsSet(ToolbarItemAxisBehavior.AxisBehaviorProperty))
        {
            // Clearing the property also opts this item out of further observation.
            _nativeItems.Remove(item.AutomationId);
            _observedToolbarItems.Remove(item);
            item.PropertyChanged -= OnToolbarItemPropertyChanged;
        }
    }
}