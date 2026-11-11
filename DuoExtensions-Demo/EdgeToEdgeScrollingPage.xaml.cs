using System.ComponentModel;
using DuoExtensions.Maui;

namespace DuoExtensionsDemo;

public partial class EdgeToEdgeScrollingPage : ContentPage
{
    public EdgeToEdgeScrollingPage()
    {
        InitializeComponent();
        EdgeToEdgeCollectionView.ItemsSource = Enumerable.Range(1, 100)
			.Select(itemNumber => $"Item {itemNumber}").ToList();
    }
}
