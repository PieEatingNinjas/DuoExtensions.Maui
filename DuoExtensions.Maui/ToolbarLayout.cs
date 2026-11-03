using Microsoft.Maui.Controls;

namespace DuoExtensions.Maui;

public enum ToolbarVerticalBehavior
{
	Automatic,
	Disabled
}

public enum ToolbarVerticalCompressionBehavior
{
	Automatic,
	PrefersToolbarItems,
	PrefersTabBar
}

public enum ToolbarVerticalEdge
{
	Unspecified,
	Leading,
	Trailing
}

public static class ToolbarLayout
{
	// Stored on Shell; the iOS renderer answers UIKit's preference query with this value.
	public static readonly BindableProperty VerticalBehaviorProperty =
		BindableProperty.CreateAttached(
			"VerticalBehavior",
			typeof(ToolbarVerticalBehavior),
			typeof(ToolbarLayout),
			ToolbarVerticalBehavior.Automatic,
			propertyChanged: OnVerticalBehaviorChanged);

	// Stored per page; its tracker applies changes to the native navigation item.
	public static readonly BindableProperty CompressionBehaviorProperty =
		BindableProperty.CreateAttached(
			"CompressionBehavior",
			typeof(ToolbarVerticalCompressionBehavior),
			typeof(ToolbarLayout),
			ToolbarVerticalCompressionBehavior.Automatic);

	// Only the platform integration can write this system-derived value; consumers can bind to it.
	private static readonly BindablePropertyKey VerticalEdgePropertyKey =
		BindableProperty.CreateAttachedReadOnly(
			"VerticalEdge",
			typeof(ToolbarVerticalEdge),
			typeof(ToolbarLayout),
			ToolbarVerticalEdge.Unspecified);

	public static readonly BindableProperty VerticalEdgeProperty = VerticalEdgePropertyKey.BindableProperty;

	public static ToolbarVerticalBehavior GetVerticalBehavior(BindableObject bindable) =>
		(ToolbarVerticalBehavior)bindable.GetValue(VerticalBehaviorProperty);

	public static void SetVerticalBehavior(Shell shell, ToolbarVerticalBehavior value) =>
		shell.SetValue(VerticalBehaviorProperty, value);

	public static ToolbarVerticalCompressionBehavior GetCompressionBehavior(BindableObject bindable) =>
		(ToolbarVerticalCompressionBehavior)bindable.GetValue(CompressionBehaviorProperty);

	public static void SetCompressionBehavior(Page page, ToolbarVerticalCompressionBehavior value) =>
		page.SetValue(CompressionBehaviorProperty, value);

	public static ToolbarVerticalEdge GetVerticalEdge(BindableObject bindable) =>
		(ToolbarVerticalEdge)bindable.GetValue(VerticalEdgeProperty);

	internal static void UpdateVerticalEdge(Page page, ToolbarVerticalEdge value) =>
		page.SetValue(VerticalEdgePropertyKey, value);

	private static void OnVerticalBehaviorChanged(BindableObject bindable, object oldValue, object newValue)
	{
#if IOS && !MACCATALYST
		if (bindable is Shell { Handler: ToolbarLayoutShellRenderer renderer }
			&& OperatingSystem.IsIOSVersionAtLeast(27, 1))
		{
#pragma warning disable CA1416 // iOS-only renderer and runtime version guard above.
			// Invalidate both controllers: ViewController is the outer flyout, not the renderer itself.
			// The outer invalidation is defensive; its necessity has not been tested separately.
			renderer.SetNeedsUpdateOfVerticalBarConfiguration();
			renderer.ViewController.SetNeedsUpdateOfVerticalBarConfiguration();
#pragma warning restore CA1416
		}
#endif
	}
}
