using System;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.MasterData.Branches;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Identity.Application.Branches.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public class DesignerSafetyTests
{
    [Theory]
    [InlineData(typeof(BranchEditForm))]
    [InlineData(typeof(CustomerEditForm))]
    [InlineData(typeof(TextPromptForm))]
    [InlineData(typeof(ManagerAuthorizationForm))]
    public void ParameterlessConstructor_CanBeInvokedViaReflectionWithoutThrowing(Type type)
    {
        // Act & Assert
        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
        
        var form = Assert.IsAssignableFrom<Form>(instance);
        Assert.True(form.Width > 0);
        Assert.True(form.Height > 0);
        form.Dispose();
    }

    [Fact]
    public void MasterDataListView_ParameterlessConstructor_InstantiatesSuccessfully()
    {
        // Act
        var instance = new MasterDataListView<BranchDto>();
        
        // Assert
        Assert.NotNull(instance);
        instance.Dispose();
    }

    [Fact]
    public void CustomerLedgerDialog_ParameterlessConstructor_InstantiatesSuccessfully()
    {
        // Act
        var instance = Activator.CreateInstance(typeof(CustomerLedgerDialog));

        // Assert
        Assert.NotNull(instance);
        var form = Assert.IsAssignableFrom<Form>(instance);
        Assert.True(form.Width >= 500);
        form.Dispose();
    }

    [Fact]
    public void DesignerFiles_DoNotContainReportPeriodCalculatorCalls()
    {
        // The Visual Studio WinForms Designer fails when InitializeComponent calls external procedural methods
        var baseDir = AppContext.BaseDirectory;
        // Search upwards for src/Clovent.Desktop
        var dir = new System.IO.DirectoryInfo(baseDir);
        while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Clovent.BusinessOperatingSystem.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);

        var ledgerDesigner = System.IO.Path.Combine(dir.FullName, "src", "Clovent.Desktop", "Restaurant", "Customers", "CustomerLedgerDialog.Designer.cs");
        Assert.True(System.IO.File.Exists(ledgerDesigner));
        var ledgerContent = System.IO.File.ReadAllText(ledgerDesigner);
        Assert.DoesNotContain("ReportPeriodCalculator", ledgerContent);
        Assert.DoesNotContain("foreach", ledgerContent);

        var eodDesigner = System.IO.Path.Combine(dir.FullName, "src", "Clovent.Desktop", "Restaurant", "EndOfDay", "EndOfDayReportView.Designer.cs");
        Assert.True(System.IO.File.Exists(eodDesigner));
        var eodContent = System.IO.File.ReadAllText(eodDesigner);
        Assert.DoesNotContain("ReportPeriodCalculator", eodContent);
    }

    [Fact]
    public void MasterDataEditFormBase_ButtonsHaveMinimumSizeConfigured()
    {
        using var form = (Form)Activator.CreateInstance(typeof(CustomerEditForm))!;
        Assert.True(form.Width >= 500);
        Assert.True(form.Height >= 480);
    }
}
