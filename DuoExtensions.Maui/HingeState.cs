namespace DuoExtensions.Maui;

public enum HingeStatus
{
	Unknown,
	Closed,
	PartiallyOpen,
	FullyOpen
}

/// <summary>A hinge reading. The angle is expressed in degrees, not UIKit's radians.</summary>
public sealed record HingeState(HingeStatus Status, double AngleDegrees);

public sealed class HingeStateChangedEventArgs(HingeState? state) : EventArgs
{
	/// <summary>Null when the observed hierarchy no longer provides hinge information.</summary>
	public HingeState? State { get; } = state;
}
