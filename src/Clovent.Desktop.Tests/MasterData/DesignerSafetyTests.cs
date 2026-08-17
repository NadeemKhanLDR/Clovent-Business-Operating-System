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
}
