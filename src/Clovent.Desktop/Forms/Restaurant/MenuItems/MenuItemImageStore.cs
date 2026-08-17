using System.Drawing.Imaging;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

/// <summary>
/// Optional per-item photo for the Menu Items screen, stored as a plain PNG
/// file keyed by <c>ProductId</c> under the current user's local application
/// data folder - deliberately not a database column. <c>Product</c>/
/// <c>ProductVariant</c>/<c>ProductPrice</c> are shared Catalog aggregates
/// consumed by every current and future module (Restaurant, Retail,
/// Purchasing); adding an image column to <c>Products</c> for one
/// presentation layer's "nice to have" would be a schema change every other
/// consumer inherits for a feature only Restaurant asked for. A file
/// convention gives "optional image" with zero migration risk - the image
/// simply doesn't exist until a Restaurant user chooses one, and nothing
/// elsewhere in the solution needs to know this file convention exists.
/// </summary>
internal static class MenuItemImageStore
{
    private static readonly string RootDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "MenuItemImages");

    /// <summary>The conventional file path for <paramref name="productId"/>'s image, whether or not it currently exists.</summary>
    public static string GetPath(Guid productId) => Path.Combine(RootDirectory, $"{productId:N}.png");

    /// <summary>Whether an image has been saved for <paramref name="productId"/>.</summary>
    public static bool Exists(Guid productId) => File.Exists(GetPath(productId));

    /// <summary>Saves (overwriting any existing) <paramref name="image"/> as <paramref name="productId"/>'s photo, always normalized to PNG so lookup never has to guess an extension.</summary>
    public static void Save(Guid productId, Image image)
    {
        Directory.CreateDirectory(RootDirectory);
        using var bitmap = new Bitmap(image);
        bitmap.Save(GetPath(productId), ImageFormat.Png);
    }

    /// <summary>Removes <paramref name="productId"/>'s photo, if any. A no-op if none exists.</summary>
    public static void Delete(Guid productId)
    {
        var path = GetPath(productId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Loads <paramref name="productId"/>'s photo, or <see langword="null"/>
    /// if it has none. Returns an independent in-memory copy, not an
    /// <see cref="Image"/> tied to the file - <see cref="Image.FromFile(string)"/>
    /// keeps its source file locked for as long as the returned
    /// <see cref="Image"/> lives, which would collide with a later
    /// <see cref="Save"/>/<see cref="Delete"/> for the same product (POS
    /// caches a loaded photo for its whole session - see
    /// <c>RestaurantPosView.LoadAsync</c> - while Menu Items may
    /// simultaneously replace that same photo in another open document tab).
    /// </summary>
    public static Image? Load(Guid productId)
    {
        var path = GetPath(productId);
        if (!File.Exists(path))
        {
            return null;
        }

        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var decoded = Image.FromStream(fileStream);
        return new Bitmap(decoded);
    }
}
