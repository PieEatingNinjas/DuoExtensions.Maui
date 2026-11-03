using DuoExtensions.Maui;

namespace DuoExtensionsDemo;

internal static class DemoActions
{
    public static void Add(Page page, Label feedback, bool crowded = false)
    {
        (string Title, string Icon)[] actions =
        [
            ("Search", "search.png"), ("Favorite", "favorite.png"),
            ("Share", "share.png"), ("Edit", "edit.png"),
            ("Bookmark", "bookmark.png"), ("Download", "download.png"),
            ("Archive", "archive.png"), ("Delete", "delete.png")
        ];

        foreach (var (title, icon) in actions.Take(crowded ? actions.Length : 2))
        {
            var item = new ToolbarItem
            {
                Text = title,
                IconImageSource = icon,
                Order = ToolbarItemOrder.Primary
            };
            ToolbarItemAxisBehavior.SetAxisBehavior(item, ToolbarAxisBehavior.VerticalPreferred);
            item.Clicked += (_, _) => feedback.Text = $"{title} selected (demo action).";
            page.ToolbarItems.Add(item);
        }
    }
}
