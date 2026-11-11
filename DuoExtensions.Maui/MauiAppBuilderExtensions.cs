using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuoExtensions.Maui;

public static class MauiAppBuilderExtensions
{
	/// <summary>Registers hinge monitoring and enables the iOS toolbar extensions for MAUI Shell.</summary>
	public static MauiAppBuilder UseDuoExtensions(this MauiAppBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		return builder
			.UseHingeMonitoring()
			.UseDuoShellToolbars();
	}

	/// <summary>Registers window-aware hinge monitoring without changing Shell handlers.</summary>
	public static MauiAppBuilder UseHingeMonitoring(this MauiAppBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		// One access point; observers and readings remain tied to their native window hierarchy.
		builder.Services.TryAddSingleton<IHingeService, HingeService>();

		return builder;
	}

	/// <summary>Enables the iOS toolbar extensions for MAUI Shell without registering hinge monitoring.</summary>
	public static MauiAppBuilder UseDuoShellToolbars(this MauiAppBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

#if IOS
		builder.ConfigureMauiHandlers(handlers =>
		{
			handlers.AddHandler(typeof(Shell), typeof(ToolbarLayoutShellRenderer));
		});
#endif

		return builder;
	}

	/// <summary>Enables edge-to-edge collection view on iPhone Duo.</summary>
	public static MauiAppBuilder UseEdgeToEdgeCollectionView(this MauiAppBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

#if IOS
		builder.ConfigureMauiHandlers(handlers =>
		{
			handlers.AddHandler(typeof(CollectionView), typeof(EdgeToEdgeCollectionViewHandler));
		});
#endif

		return builder;
	}
}
