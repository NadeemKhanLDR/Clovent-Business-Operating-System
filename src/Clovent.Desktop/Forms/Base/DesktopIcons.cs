using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Assigns DevExpress's own URI-based image-gallery icons to command
/// buttons, so action buttons carry real vector glyphs instead of Unicode
/// text symbols pretending to be icons. Uses only DevExpress's built-in
/// <see cref="ImageOptions.ImageUri"/> mechanism (the DX Image Gallery) -
/// no second icon framework, no custom-drawn or PNG images. If the gallery
/// is unavailable in a given installation the URI simply resolves to no
/// image and the button falls back to its text caption, so this is always
/// safe to call.
/// </summary>
public static class DesktopIcons
{
    /// <summary>Standard "add/create" gallery icon.</summary>
    public const string Add = "devav/actions/add.svg";

    /// <summary>Standard "edit/pencil" gallery icon.</summary>
    public const string Edit = "devav/actions/edit.svg";

    /// <summary>Standard "apply/check" gallery icon (used for Activate).</summary>
    public const string ActivateIcon = "devav/actions/apply.svg";

    /// <summary>Standard "cancel/disable" gallery icon (used for Deactivate).</summary>
    public const string CancelIcon = "devav/actions/close.svg";

    /// <summary>Standard "refresh" gallery icon.</summary>
    public const string Refresh = "devav/actions/refresh.svg";

    /// <summary>Standard "save" gallery icon.</summary>
    public const string Save = "devav/actions/save.svg";

    /// <summary>Standard "search/magnifier" gallery icon.</summary>
    public const string Search = "devav/actions/search.svg";

    /// <summary>Standard "up arrow" gallery icon.</summary>
    public const string Up = "svgimages/actions/up.svg";

    /// <summary>Standard "down arrow" gallery icon.</summary>
    public const string Down = "svgimages/arrows/movedown.svg";

    /// <summary>Standard "idea / lightbulb" gallery icon.</summary>
    public const string Idea = "svgimages/icon%20builder/business_idea.svg";

    /// <summary>Standard "idea / lightbulb" gallery icon alternative URI.</summary>
    public const string IdeaAlt = "svgimages/icon builder/business_idea.svg";

    /// <summary>Standard "operations / properties / gear" gallery icon.</summary>
    public const string Operations = "svgimages/setup/properties.svg";

    /// <summary>Assigns <paramref name="imageUri"/> to <paramref name="button"/> - a no-op visually if the gallery cannot resolve it.</summary>
    public static void Apply(SimpleButton button, string imageUri)
    {
        button.ImageOptions.ImageUri = imageUri;
        if (button.ImageOptions.SvgImage == null)
        {
            button.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(imageUri);
        }
    }
}
