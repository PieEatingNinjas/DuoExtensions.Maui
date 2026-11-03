using DuoExtensions.Maui;

namespace DuoExtensionsDemo;

public partial class HingePage : ContentPage
{
    private readonly IHingeService _hingeService;
    private IHingeObserver? _hinge;

    public HingePage(IHingeService hingeService)
    {
        InitializeComponent();
        _hingeService = hingeService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hinge = _hingeService.ForWindow(Window
            ?? throw new InvalidOperationException("The hinge page must belong to a window."));
        _hinge.StateChanged += OnHingeStateChanged;
        UpdateReading(_hinge.CurrentState);
    }

    protected override void OnDisappearing()
    {
        if (_hinge is not null)
        {
            _hinge.StateChanged -= OnHingeStateChanged;
            _hinge = null;
        }
        base.OnDisappearing();
    }

    private void OnHingeStateChanged(object? sender, HingeStateChangedEventArgs e) =>
        UpdateReading(e.State);

    private void UpdateReading(HingeState? state)
    {
        StatusLabel.Text = state?.Status switch
        {
            HingeStatus.FullyOpen => "Fully open",
            HingeStatus.PartiallyOpen => "Partially open",
            HingeStatus.Closed => "Closed",
            HingeStatus.Unknown => "Unknown",
            _ => "Unavailable"
        };
        AngleLabel.Text = state is null ? "Unavailable" : $"{state.AngleDegrees:F1}\u00B0";
        AvailabilityLabel.Text = !_hingeService.IsSupported
            ? "The hinge API requires iOS 27.1 or later."
            : state is null ? "No hinge reading available for this window." : "Receiving live hinge readings.";
    }
}
