using DuoExtensions.Maui;

namespace DuoExtensionsDemo;

public partial class CompressionPage : ContentPage
{
    public CompressionPage()
    {
        InitializeComponent();
        DemoActions.Add(this, ActionFeedback, crowded: true);
        CompressionPicker.SelectedIndex = 0;
    }

    private void OnCompressionChanged(object? sender, EventArgs e)
    {
        if (CompressionPicker.SelectedIndex < 0)
            return;

        var behavior = CompressionPicker.SelectedIndex switch
        {
            1 => ToolbarVerticalCompressionBehavior.PrefersToolbarItems,
            2 => ToolbarVerticalCompressionBehavior.PrefersTabBar,
            _ => ToolbarVerticalCompressionBehavior.Automatic
        };
        ToolbarLayout.SetCompressionBehavior(this, behavior);
        DescriptionLabel.Text = behavior switch
        {
            ToolbarVerticalCompressionBehavior.PrefersToolbarItems =>
                "Favor space for actions. The tab bar can be compressed first.",
            ToolbarVerticalCompressionBehavior.PrefersTabBar =>
                "Favor space for tabs. Toolbar actions can be compressed first.",
            _ => "Let iOS balance the available space between actions and tabs."
        };
    }
}
