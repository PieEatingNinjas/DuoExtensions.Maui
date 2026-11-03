using System.ComponentModel;
using DuoExtensions.Maui;

namespace DuoExtensionsDemo;

public partial class VerticalEdgePage : ContentPage
{
    public VerticalEdgePage()
    {
        InitializeComponent();
        DemoActions.Add(this, ActionFeedback);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PropertyChanged += OnPagePropertyChanged;
        UpdateEdge();
    }

    protected override void OnDisappearing()
    {
        PropertyChanged -= OnPagePropertyChanged;
        base.OnDisappearing();
    }

    private void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == ToolbarLayout.VerticalEdgeProperty.PropertyName
            || e.PropertyName == nameof(FlowDirection))
            UpdateEdge();
    }

    private void UpdateEdge()
    {
        var edge = ToolbarLayout.GetVerticalEdge(this);
        EdgeLabel.Text = edge.ToString();
        DirectionArrow.IsVisible = edge != ToolbarVerticalEdge.Unspecified;
        if (edge == ToolbarVerticalEdge.Unspecified)
        {
            DirectionLabel.Text = "No vertical edge reported.";
            return;
        }

        var pointsLeft = (edge == ToolbarVerticalEdge.Leading)
            != (FlowDirection == FlowDirection.RightToLeft);
        DirectionArrow.Rotation = pointsLeft ? 180 : 0;
        DirectionLabel.Text = pointsLeft ? "The vertical bar is on the left." : "The vertical bar is on the right.";
    }
}
