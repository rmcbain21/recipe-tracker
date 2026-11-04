using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace RecipeTracker.Web.Layout;

/// <summary>A top-level destination, shared by the bottom navigation and the drawer.</summary>
public sealed record NavItem(string Title, string Href, string Icon, NavLinkMatch Match)
{
    public static readonly IReadOnlyList<NavItem> All =
    [
        new("Recipes", "", Icons.Material.Filled.MenuBook, NavLinkMatch.All),
    ];
}
