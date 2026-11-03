using DuoExtensions.Maui;

namespace DuoExtensionsDemo;

public partial class AxisPage : ContentPage
{
    public AxisPage()
    {
        InitializeComponent();
        AxisPicker.SelectedIndex = 0;
    }

    private void OnAxisChanged(object? sender, EventArgs e)
    {
        if (AxisPicker.SelectedIndex < 0)
            return;

        var behavior = AxisPicker.SelectedIndex switch
        {
            1 => ToolbarAxisBehavior.HorizontalOnly,
            2 => ToolbarAxisBehavior.VerticalPreferred,
            _ => ToolbarAxisBehavior.Automatic
        };
        ToolbarItemAxisBehavior.SetAxisBehavior(ShareAction, behavior);
        DescriptionLabel.Text = behavior switch
        {
            ToolbarAxisBehavior.HorizontalOnly => "Only horizontal bars can host this action.",
            ToolbarAxisBehavior.VerticalPreferred => "Supports both axes, but prefers a vertical bar when available.",
            _ => "Let iOS choose the most suitable bar for this action."
        };
    }

    private void OnShareClicked(object? sender, EventArgs e) =>
        ActionFeedback.Text = "Share selected. The same action works in either axis.";
}
