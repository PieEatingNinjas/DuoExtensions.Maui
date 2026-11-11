using Microsoft.Maui.Controls.Handlers.Items2;
using UIKit;

namespace DuoExtensions.Maui;

public class EdgeToEdgeCollectionViewHandler : CollectionViewHandler2
{
	private bool appearanceActive;
	private UIViewController? layoutOwner;
	private bool previousIncludesOpaqueBars;
	private UIRectEdge previousExtendedEdges;

	protected override void ConnectHandler(UIView platformView)
	{
		base.ConnectHandler(platformView);
		Controller.CollectionView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Automatic;
		VirtualView.Loaded += OnLoaded;
		VirtualView.Unloaded += OnUnloaded;
	}

	protected override void DisconnectHandler(UIView platformView)
	{
		VirtualView.Loaded -= OnLoaded;
		VirtualView.Unloaded -= OnUnloaded;
		EndAppearance();
		RestoreOpaqueBarLayout();
		base.DisconnectHandler(platformView);
	}

	private void OnLoaded(object? sender, EventArgs e)
	{
		ConfigureOpaqueBarLayout();
		if (appearanceActive || Controller.ParentViewController is not null)
			return;

		Controller.BeginAppearanceTransition(true, false);
		Controller.EndAppearanceTransition();
		appearanceActive = true;
	}

	private void OnUnloaded(object? sender, EventArgs e)
	{
		EndAppearance();
		RestoreOpaqueBarLayout();
	}

	private void ConfigureOpaqueBarLayout()
	{
		if (layoutOwner is not null)
			return;

		Controller.ExtendedLayoutIncludesOpaqueBars = true;
		for (UIView? ancestor = PlatformView.Superview; ancestor is not null; ancestor = ancestor.Superview)
		{
			if (ancestor.NextResponder is UIViewController owner && owner != Controller)
			{
				layoutOwner = owner.NavigationController?.TopViewController ?? owner;
				previousIncludesOpaqueBars = layoutOwner.ExtendedLayoutIncludesOpaqueBars;
				previousExtendedEdges = layoutOwner.EdgesForExtendedLayout;
				layoutOwner.ExtendedLayoutIncludesOpaqueBars = true;
				layoutOwner.EdgesForExtendedLayout |= UIRectEdge.Top;
				layoutOwner.View?.SetNeedsLayout();

				break;
			}
		}
	}

	private void RestoreOpaqueBarLayout()
	{
		if (layoutOwner is null)
			return;

		layoutOwner.ExtendedLayoutIncludesOpaqueBars = previousIncludesOpaqueBars;
		layoutOwner.EdgesForExtendedLayout = previousExtendedEdges;
		layoutOwner.View?.SetNeedsLayout();
		layoutOwner = null;
	}

	private void EndAppearance()
	{
		if (!appearanceActive)
			return;

		Controller.BeginAppearanceTransition(false, false);
		Controller.EndAppearanceTransition();
		appearanceActive = false;
	}
}