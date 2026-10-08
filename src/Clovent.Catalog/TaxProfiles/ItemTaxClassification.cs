namespace Clovent.Catalog.TaxProfiles;

/// <summary>
/// Classification of supplies under Pakistan tax jurisdiction (Goods governed federally by FBR Sales Tax Act 1990
/// vs Services governed provincially by PRA, SRB, KPRA, BRA, or ICT).
/// </summary>
public enum ItemTaxClassification
{
    /// <summary>Tangible goods / retail items governed federally by FBR Sales Tax Act, 1990.</summary>
    Goods = 1,

    /// <summary>Food, catering, and restaurant services governed provincially by PRA, SRB, KPRA, BRA, or ICT.</summary>
    Services = 2
}
