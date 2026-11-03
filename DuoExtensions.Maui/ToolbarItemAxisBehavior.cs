using Microsoft.Maui.Controls;

namespace DuoExtensions.Maui;

public enum ToolbarAxisBehavior
{
	Automatic,
	HorizontalOnly,
	VerticalPreferred
}

public static class ToolbarItemAxisBehavior
{
	// Stores the MAUI preference; the iOS page tracker applies it to Shell's native button.
	// Explicitly set an initial value (even Automatic) to opt into change observation.
	public static readonly BindableProperty AxisBehaviorProperty =
		BindableProperty.CreateAttached(
			"AxisBehavior",
			typeof(ToolbarAxisBehavior),
			typeof(ToolbarItemAxisBehavior),
			ToolbarAxisBehavior.Automatic);

	public static ToolbarAxisBehavior GetAxisBehavior(BindableObject bindable) =>
		(ToolbarAxisBehavior)bindable.GetValue(AxisBehaviorProperty);

	public static void SetAxisBehavior(
		BindableObject bindable,
		ToolbarAxisBehavior value)
        {
		    bindable.SetValue(AxisBehaviorProperty, value);
        }
}