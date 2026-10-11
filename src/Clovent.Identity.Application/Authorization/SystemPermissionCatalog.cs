using System.Collections.Generic;
using System.Linq;

namespace Clovent.Identity.Application.Authorization;

/// <summary>
/// Authoritative catalog of all system permissions, menu codes, feature operations,
/// and baseline role grants for Clovent Business Operating System (CBOS).
/// Used uniformly by commissioning, production upgrades, and authorization policies.
/// </summary>
public static class SystemPermissionCatalog
{
    /// <summary>All registered navigation menu keys across CBOS desktop ribbon pages.</summary>
    public static readonly IReadOnlyList<string> MenuKeys =
    [
        "dashboard",
        "users",
        "roles",
        "organizations",
        "companies",
        "branches",
        "departments",
        "warehouses",
        "terminals",
        "fiscalyears",
        "currencies",
        "businesssettings",
        "categories",
        "brands",
        "units",
        "products",
        "variants",
        "barcodes",
        "prices",
        "warehousestocks",
        "stockadjustments",
        "stocktransfers",
        "inventorytransactions",
        "diningareas",
        "tables",
        "pos",
        "runningorders",
        "holdorders",
        "kitchentickets",
        "orderhistory",
        "endofday",
        "menuitems",
        "restaurantsetup",
        "paymentmethods",
        "activitylog",
        "appearance",
        "customers",
        "customerreceivables",
        "shifts",
        "attendance",
        "recommendationrules",
        "smartcombos",
        "quickordertemplates",
        "upsellperformance",
        "branchsync",
        "quickbooks",
        "printers",
        "printerhealth",
        "auditanalytics",
        "audit.analytics",
        "taxprofiles",
        "taxsummary",
        "refundreports"
    ];

    /// <summary>All granular feature-level operations across CBOS modules.</summary>
    public static readonly IReadOnlyList<(string Feature, string[] Operations)> FeatureOperations =
    [
        ("users", ["create", "edit", "activate", "deactivate", "resetpassword", "setpin", "unlock", "assignrole", "assigncompany", "assignbranch"]),
        ("roles", ["create", "edit", "assignpermission"]),
        ("organizations", ["create", "edit", "activate", "deactivate"]),
        ("companies", ["create", "edit", "activate", "deactivate"]),
        ("branches", ["create", "edit", "activate", "deactivate"]),
        ("departments", ["create", "edit", "activate", "deactivate"]),
        ("warehouses", ["create", "edit", "activate", "deactivate"]),
        ("terminals", ["create", "edit", "activate", "deactivate"]),
        ("printers", ["manage", "test", "view", "health"]),
        ("printerhealth", ["view"]),
        ("fiscalyears", ["create", "edit", "deactivate"]),
        ("currencies", ["create", "activate", "deactivate"]),
        ("businesssettings", ["edit"]),
        ("categories", ["create", "edit", "activate", "deactivate"]),
        ("brands", ["create", "edit", "activate", "deactivate"]),
        ("units", ["create", "edit", "activate", "deactivate"]),
        ("products", ["create", "edit", "activate", "deactivate"]),
        ("variants", ["create", "edit", "activate", "deactivate"]),
        ("barcodes", ["create", "activate", "deactivate", "markprimary"]),
        ("prices", ["create", "edit", "activate", "deactivate"]),
        ("warehousestocks", ["create", "edit", "receive", "issue", "reserve", "release"]),
        ("stockadjustments", ["create", "apply", "cancel"]),
        ("stocktransfers", ["create", "complete", "cancel"]),
        ("inventorytransactions", ["view"]),
        ("diningareas", ["create", "edit", "activate", "deactivate"]),
        ("tables", ["create", "edit", "activate", "deactivate", "occupy", "vacate", "reserve", "outofservice", "returntoservice"]),
        ("pos", ["create", "hold", "resume", "void", "cancel", "reopen", "sendtokitchen", "complete", "pay", "refund",
                 "transfertable", "mergetables", "splitbill", "notes", "discount", "servicecharge", "additem", "editline",
                 "priceoverride", "creditsale", "exceedcreditlimit",
                 "smartinsights", "quickorders", "restaurantpulse", "rushmode", "printlastreceipt"]),
        ("kitchentickets", ["start", "markready", "serve", "cancel"]),
        ("endofday", ["view", "close"]),
        ("menuitems", ["create", "edit", "activate", "deactivate", "createcategory"]),
        ("restaurantsetup", ["edit"]),
        ("paymentmethods", ["create", "edit", "activate", "deactivate"]),
        ("activitylog", ["view"]),
        ("appearance", ["create", "edit", "delete"]),
        ("customers", ["create", "edit", "activate", "deactivate", "viewledger", "payment"]),
        ("customerreceivables", ["view", "export"]),
        ("shifts", ["open", "close", "cashmovement", "history", "viewdetails", "overridevariance"]),
        ("attendance", ["punchself", "viewself", "manage"]),
        ("recommendationrules", ["create", "edit", "deactivate"]),
        ("upsellperformance", ["view"]),
        ("smartcombos", ["analyze", "create", "dismiss"]),
        ("quickordertemplates", ["create", "edit", "deactivate"]),
        ("branchsync", ["sync", "view"]),
        ("quickbooks", ["sync", "view", "override"]),
        ("taxprofiles", ["create", "edit", "activate", "deactivate", "assign"]),
        ("taxsummary", ["view", "export"]),
        ("refundreports", ["view", "export"]),
        ("auditanalytics", ["view"])
    ];

    /// <summary>Produces all valid, non-redundant system permission codes.</summary>
    public static IEnumerable<string> GetAllPermissionCodes()
    {
        var codes = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var key in MenuKeys)
        {
            codes.Add($"menu.{key.ToLowerInvariant()}");
        }

        foreach (var (feature, operations) in FeatureOperations)
        {
            foreach (var op in operations)
            {
                codes.Add($"feature.{feature.ToLowerInvariant()}.{op.ToLowerInvariant()}");
            }
        }

        return codes;
    }

    /// <summary>Produces the baseline operational and managerial permissions for the Manager role.</summary>
    public static IEnumerable<string> GetManagerPermissionCodes()
    {
        var managerMenus = new[]
        {
            "dashboard", "pos", "runningorders", "holdorders", "orderhistory", "kitchentickets",
            "endofday", "shifts", "customers", "customerreceivables", "recommendationrules",
            "smartcombos", "quickordertemplates", "upsellperformance", "branchsync", "quickbooks",
            "printers", "printerhealth", "auditanalytics", "audit.analytics", "taxsummary", "refundreports"
        };

        var managerFeatures = new[]
        {
            "feature.pos.refund",
            "feature.pos.void",
            "feature.pos.priceoverride",
            "feature.pos.exceedcreditlimit",
            "feature.pos.discount",
            "feature.pos.notes",
            "feature.pos.splitbill",
            "feature.shifts.open",
            "feature.shifts.close",
            "feature.shifts.cashmovement",
            "feature.shifts.history",
            "feature.shifts.viewdetails",
            "feature.shifts.overridevariance",
            "feature.endofday.view",
            "feature.endofday.close",
            "feature.printers.manage",
            "feature.printers.test",
            "feature.printers.view",
            "feature.printers.health",
            "feature.printerhealth.view",
            "feature.auditanalytics.view",
            "feature.taxsummary.view",
            "feature.taxsummary.export",
            "feature.refundreports.view",
            "feature.refundreports.export",
            "feature.customerreceivables.view",
            "feature.customerreceivables.export",
            "feature.quickbooks.sync",
            "feature.quickbooks.view",
            "feature.quickbooks.override",
            "feature.branchsync.sync",
            "feature.branchsync.view",
            "feature.upsellperformance.view",
            "feature.smartcombos.analyze",
            "feature.smartcombos.create",
            "feature.smartcombos.dismiss"
        };

        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var menu in managerMenus)
        {
            set.Add($"menu.{menu.ToLowerInvariant()}");
        }
        foreach (var feature in managerFeatures)
        {
            set.Add(feature.ToLowerInvariant());
        }

        return set;
    }
}
