namespace Clovent.Desktop.Forms.Base.Appearance;

/// <summary>
/// How narrowly an <see cref="AppearanceRule"/> targets controls - from
/// broadest to narrowest. <see cref="AppearanceManager"/> resolves the
/// single best-matching rule for a given control by picking the most
/// specific scope that matches, in this order: <see cref="Control"/> &gt;
/// <see cref="ControlType"/> &gt; <see cref="Form"/> &gt; <see cref="Module"/> &gt;
/// <see cref="System"/>.
/// </summary>
public enum AppearanceScopeType
{
    /// <summary>Applies everywhere in CBOS. <see cref="AppearanceRule.ScopeKey"/> is unused/empty.</summary>
    System,

    /// <summary>Applies to one module (e.g. "Restaurant"). <see cref="AppearanceRule.ScopeKey"/> is the module name.</summary>
    Module,

    /// <summary>Applies to one screen (e.g. "RestaurantPosView", "PaymentPanel"). <see cref="AppearanceRule.ScopeKey"/> is the screen's type name.</summary>
    Form,

    /// <summary>Applies to every control of one kind (e.g. "SimpleButton", "LabelControl"), across every screen. <see cref="AppearanceRule.ScopeKey"/> is the control's type name.</summary>
    ControlType,

    /// <summary>Applies to one specific named control on one specific screen (e.g. "PaymentPanel.PayButton"). <see cref="AppearanceRule.ScopeKey"/> is "{FormTypeName}.{ControlName}".</summary>
    Control,
}
