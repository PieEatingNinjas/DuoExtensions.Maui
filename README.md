<img src="assets/logo.png" alt="DuoExtensions.Maui logo" width="96" height="96" />

# DuoExtensions.Maui

Adaptive toolbars in .NET MAUI Shell and window-aware hinge monitoring for iPhone Duo.

**DuoExtensions.Maui** makes selected UIKit APIs easier to use from MAUI through attached properties and a dependency-injected hinge service. Use XAML and C# to configure toolbar preferences, observe the vertical bar edge, and react to hinge status and angle without repeating the native plumbing in your app.

> **Experimental:** this is a playground library, so use it with caution. APIs and implementation may change. If you encounter an issue, please get in touch or open an issue! Feedback and simpler approaches are welcome. Future .NET MAUI support may replace parts of this library; if useful, some of the functionality might eventually fit in the .NET MAUI Community Toolkit.

## Features

| Feature | API | Scope |
| --- | --- | --- |
| Toolbar item axis | `ToolbarItemAxisBehavior.AxisBehavior` | Individual Shell toolbar action |
| Vertical bars | `ToolbarLayout.VerticalBehavior` | Entire Shell |
| Bar compression | `ToolbarLayout.CompressionBehavior` | Shell page |
| Vertical bar edge | `ToolbarLayout.VerticalEdge` (read-only) | Shell page |
| Hinge status and angle | `IHingeService.ForWindow(Window)` | Any MAUI window; no Shell dependency |

These settings are system preferences, not custom toolbar rendering. UIKit determines the final layout.

## Requirements

- .NET 10 and the .NET MAUI workload.
- The .NET for iOS SDK exposing the iOS 27.1 APIs.
- Xcode 27.1 and a compatible simulator or device for native testing.
- An iOS app targeting `net10.0-ios27.1` to select the library's native implementation.

The library targets `net10.0` and `net10.0-ios27.1`. Its iOS deployment minimum is 15.0, with new native APIs guarded at runtime. Toolbar extensions have no effect before iOS 27.1, and hinge monitoring reports unsupported.

The shared target keeps public types available in cross-platform apps, but **Android, Mac Catalyst and Windows have no native implementation**. Hinge API support indicates OS capability, not the presence of hinge hardware or an available reading.

## Installation and setup

Install the latest NuGet package using:

```bash
dotnet add package DuoExtensions.Maui
```

Enable the integration in `MauiProgram.cs`:

```csharp
using DuoExtensions.Maui;

builder
    .UseMauiApp<App>()
    .UseDuoExtensions();
```

This registers the iOS Shell renderer and the singleton `IHingeService`. You do not need to register or attach the hinge observer yourself.

For hinge monitoring without Shell (or without replacing a custom Shell renderer), use `builder.UseHingeMonitoring()`. For only Shell toolbars, use `builder.UseDuoShellToolbars()`. `UseDuoExtensions()` combines both.

For XAML examples, declare:

```xml
xmlns:duo="clr-namespace:DuoExtensions.Maui;assembly=DuoExtensions.Maui"
```

> The toolbar integration replaces the iOS `Shell` renderer registration. If your app already uses a custom Shell renderer, the implementations must be combined; registering multiple renderers does not compose them.

## Toolbar item axis

Configure an individual action:

```xml
<ContentPage.ToolbarItems>
    <ToolbarItem x:Name="ShareAction"
                 Text="Share"
                 IconImageSource="share.png"
                 duo:ToolbarItemAxisBehavior.AxisBehavior="Automatic" />
</ContentPage.ToolbarItems>
```


| Value | Meaning |
| --- | --- |
| `Automatic` | Let UIKit choose the action's placement. |
| `HorizontalOnly` | Restrict the action to horizontal bars. |
| `VerticalPreferred` | Support both axes, preferring a vertical bar when available. |

Change the preference at runtime:

```csharp
ToolbarItemAxisBehavior.SetAxisBehavior(
    ShareAction, ToolbarAxisBehavior.VerticalPreferred);
```

**Explicitly set an initial value**, including `Automatic`, on actions you want to update later. This opts the item into native tracking when Shell creates its toolbar. A first assignment made only after the toolbar is created is not guaranteed to be observed until Shell rebuilds it.

The tracker matches Shell-created native buttons using `AutomationId`, assigning one if necessary. Caller-supplied identifiers should be unique.

For explicitly configured actions, the tracker also copies `ToolbarItem.Text` to the native button title, including later text changes. This preserves labels in UIKit's overflow menu when the action has an icon.

`HorizontalOnly` specifies an axis, not top or bottom. On the tested iOS 27.1 Duo simulator, initial configuration and runtime transitions can produce different placement. Do not rely on an exact bar position.

## Vertical bar behavior

Set the preference on your Shell:

```xml
<Shell ...
       xmlns:duo="clr-namespace:DuoExtensions.Maui;assembly=DuoExtensions.Maui"
       duo:ToolbarLayout.VerticalBehavior="Automatic">
```

Or change it in code:

```csharp
ToolbarLayout.SetVerticalBehavior(
    Shell.Current, ToolbarVerticalBehavior.Disabled);
```

- `Automatic` lets UIKit choose whether vertical bars are appropriate.
- `Disabled` opts the entire Shell out of vertical bars.

This is **Shell-wide**, not page-local. A change remains active when switching tabs. `Automatic` does not force a vertical layout.

## Vertical bar compression

Choose which group should retain space when tabs and actions share a crowded vertical bar:

```xml
<ContentPage ...
             xmlns:duo="clr-namespace:DuoExtensions.Maui;assembly=DuoExtensions.Maui"
             duo:ToolbarLayout.CompressionBehavior="PrefersToolbarItems">
```

```csharp
ToolbarLayout.SetCompressionBehavior(
    this, ToolbarVerticalCompressionBehavior.PrefersTabBar);
```

| Value | Meaning |
| --- | --- |
| `Automatic` | Let UIKit balance actions and tabs. |
| `PrefersToolbarItems` | Favor toolbar actions; tabs can be compressed first. |
| `PrefersTabBar` | Favor tabs; actions can be compressed first. |

This preference belongs to the page's navigation item. Compare it in a vertical layout with limited space: roomy or horizontal layouts may show no difference. UIKit decides the final overflow presentation.

## Observe the vertical edge

`VerticalEdge` reports UIKit's trait; it does not move the bar.

```xml
<ContentPage x:Name="PageRoot" ...
             xmlns:duo="clr-namespace:DuoExtensions.Maui;assembly=DuoExtensions.Maui">
    <Label Text="{Binding Source={x:Reference PageRoot},
                          Path=(duo:ToolbarLayout.VerticalEdge)}" />
</ContentPage>
```

For code-based UI, read the current value and observe the page's `PropertyChanged` event for `ToolbarLayout.VerticalEdgeProperty.PropertyName`:

```csharp
var edge = ToolbarLayout.GetVerticalEdge(this);
```

- `Leading` and `Trailing` are logical edges; account for right-to-left flow when drawing a physical direction.
- `Unspecified` means UIKit is not reporting a vertical edge for that page. Do not infer a side from screen orientation.

The tracker observes each page's native traits, including changes while a tab is cached.

## Hinge monitoring

Hinge monitoring works with or without Shell. Inject the singleton `IHingeService` into your page and obtain the observer for its window when the page appears:

```csharp
using DuoExtensions.Maui;

public sealed class HingePage : ContentPage
{
    private readonly IHingeService _service;
    private readonly Label _reading = new();
    private IHingeObserver? _observer;

    public HingePage(IHingeService service)
    {
        _service = service;
        Content = _reading;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _observer = _service.ForWindow(Window
            ?? throw new InvalidOperationException("The page must belong to a window."));
        _observer.StateChanged += OnStateChanged;
        ShowReading(_observer.CurrentState);
    }

    protected override void OnDisappearing()
    {
        if (_observer is not null)
        {
            _observer.StateChanged -= OnStateChanged;
            _observer = null;
        }
        base.OnDisappearing();
    }

    private void OnStateChanged(object? sender, HingeStateChangedEventArgs e) =>
        ShowReading(e.State);

    private void ShowReading(HingeState? state)
    {
        _reading.Text = !_service.IsSupported
            ? "Hinge API unsupported."
            : state is null
                ? "No hinge reading available."
                : $"{state.Status}: {state.AngleDegrees:F1}\u00B0";
    }
}
```

### Reading semantics and lifetime

- `HingeState` contains `Status` and `AngleDegrees`. Status is `Unknown`, `Closed`, `PartiallyOpen` or `FullyOpen`.
- `CurrentState` is nullable: no reading is **not** equivalent to a closed hinge or a zero-degree angle.
- `StateChanged` also fires when a previous reading becomes unavailable, with `State = null`.
- Call `ForWindow` on that window's UI thread. Events are delivered on that thread.
- The same window receives the same shared observer. Consumers unsubscribe from their own events; they do not dispose the observer.
- Monitoring continues across page navigation and ends when the window is destroyed. Read `CurrentState` after subscribing so a returning page immediately displays the latest state.

### One physical hinge, why one observer per window?

UIKit delivers hinge information through `UIHingeInteraction`, attached to a view hierarchy, rather than a global device sensor. Two windows can describe the same physical hinge while having different data availability and lifetimes.

The singleton service provides a convenient app-wide access point, while `ForWindow(Window)` preserves that context. An unavailable reading from one window must not erase a valid reading for another. Passing the window explicitly also avoids an ambiguous global "current window" in multi-window apps.

## Demo

The [DuoExtensions-Demo](DuoExtensions-Demo/DuoExtensions-Demo.csproj) app references the library and contains five Shell tabs:

- **Axis:** change a Share action's axis preference.
- **Compression:** compare preferences using eight distinct action icons.
- **Vertical:** toggle the Shell-wide vertical bar preference.
- **Edge:** display the live edge with a direction arrow.
- **Hinge:** display status and angle using constructor injection.

The demo targets the iOS 27.1 SDK but supports deployment to iOS 15.0 and later.


## How it works

- A custom iOS Shell renderer supplies the Shell-wide vertical preference.
- A Shell page tracker applies compression to `UINavigationItem`, maps configured toolbar actions to existing `UIBarButtonItem` instances, and observes page traits.
- The hinge service attaches a `UIHingeInteraction` to each observed native window and manages handler replacement and destruction.

The library uses native UIKit behavior. Toolbar integration currently depends on MAUI's compatibility Shell renderer infrastructure.

## Feedback

Found an unexpected layout, a lifecycle issue, or a simpler integration? Please open an issue with the .NET MAUI, iOS and Xcode versions, the device or simulator configuration, and steps to reproduce it. Contributions and suggestions are welcome!
