using System.Text.RegularExpressions;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public sealed class DesignerSerializationRegressionTests
{
    [Fact]
    public void CustomerInitialization_UsesExplicitRowsInsteadOfUnsupportedLoop()
    {
        var source = ReadSource("Restaurant/Customers/CustomerEditForm.Designer.cs");
        Assert.DoesNotMatch(@"\b(for|foreach|while)\s*\(", source);
        Assert.DoesNotContain("row++", source);
        Assert.Equal(13, Regex.Matches(source,
            @"_(?:contentPanel|customerLayout)\.RowStyles\.Add\(new RowStyle\((?:SizeType\.AutoSize)?\)\);").Count);
    }

    [Fact]
    public void CustomerLayout_IsOwnedByDerivedDesignerAndSharedWithBaseRuntimeHelpers()
    {
        var source = ReadSource("Restaurant/Customers/CustomerEditForm.Designer.cs");
        Assert.Contains("_customerLayout = new TableLayoutPanel();", source);
        using var form = new Clovent.Desktop.Restaurant.Customers.CustomerEditForm("Edit Customer");
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var owned = typeof(Clovent.Desktop.Restaurant.Customers.CustomerEditForm)
            .GetField("_customerLayout", flags)!.GetValue(form);
        var content = typeof(Clovent.Desktop.MasterData.MasterDataEditFormBase)
            .GetField("_contentPanel", flags)!.GetValue(form);
        Assert.Same(owned, content);
        Assert.Equal(2, form.Controls.Count);
        var table = Assert.IsType<System.Windows.Forms.TableLayoutPanel>(content);
        Assert.Equal(4, table.ColumnCount);
        Assert.Equal("_contentPanel", table.Name);
        var name = (System.Windows.Forms.Control)form.GetType().GetField("_nameEdit", flags)!.GetValue(form)!;
        Assert.Equal(3, table.GetColumn(name));
        Assert.Equal(2, table.GetRow(name));
    }

    [Theory]
    [InlineData("TaxSummaryReportView")]
    [InlineData("RefundAuditReportView")]
    public void ReportLayout_IsVisibleToDesignerWithoutExecutingPrivateHelpers(string name)
    {
        var source = ReadSource($"Restaurant/Reports/{name}.Designer.cs");
        Assert.Contains("_rootLayout = new TableLayoutPanel();", source);
        Assert.DoesNotContain("BuildLayout();", source);
        Assert.DoesNotContain("WireEvents();", source);
        Assert.DoesNotMatch(@"\bvar\s|\b(for|foreach|while)\s*\(|=>", source);
        Assert.DoesNotContain("StyleKpi(", source);
        Assert.Contains("Controls.Add(_rootLayout);", source);
        Assert.Contains("_rootLayout.Controls.Add(_gridControl, 0, 2);", source);
    }

    private static string ReadSource(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName,
                   "Clovent.BusinessOperatingSystem.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, "src", "Clovent.Desktop", relativePath));
    }
}
