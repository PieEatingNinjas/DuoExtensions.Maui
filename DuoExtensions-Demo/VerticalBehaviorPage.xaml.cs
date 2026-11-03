using DuoExtensions.Maui;

namespace DuoExtensionsDemo;

public partial class VerticalBehaviorPage : ContentPage
{
    public VerticalBehaviorPage()
    {
        InitializeComponent();
        DemoActions.Add(this, ActionFeedback);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        VerticalSwitch.IsToggled =
            ToolbarLayout.GetVerticalBehavior(Shell.Current) == ToolbarVerticalBehavior.Automatic;
        UpdateDescription();
    }

    private void OnVerticalToggled(object? sender, ToggledEventArgs e)
    {
        ToolbarLayout.SetVerticalBehavior(Shell.Current, e.Value
            ? ToolbarVerticalBehavior.Automatic
            : ToolbarVerticalBehavior.Disabled);
        UpdateDescription();
    }

    private void UpdateDescription() =>
        BehaviorLabel.Text = VerticalSwitch.IsToggled ? "Automatic" : "Disabled";
}
