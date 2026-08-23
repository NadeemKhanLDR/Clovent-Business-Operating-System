using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Xunit;

namespace Clovent.Desktop.Tests.Forms.Restaurant.MenuItems;

/// <summary>
/// Regression coverage for the Menu Items edit dialog. A null
/// <c>FlowLayoutPanel</c> was once passed to
/// <c>TableLayoutControlCollection.Add</c> before being created, making the
/// dialog's constructor throw <c>ArgumentNullException ('control')</c> and
/// every Edit/New attempt fail with a generic "Action Failed" box - these
/// tests construct the dialog the way <c>MenuItemsForm.EditAsync</c> does so
/// that class of bug cannot come back silently.
/// </summary>
public class MenuItemEditFormTests
{
    private static readonly Guid CategoryA = Guid.NewGuid();
    private static readonly Guid CategoryB = Guid.NewGuid();

    private static MenuItemEditForm CreateForm(
        string? name = "Garlic Nan",
        Guid? categoryId = null,
        decimal price = 120m,
        bool isActive = true,
        Image? image = null) =>
        new("Edit Menu Item", [(CategoryA, "Breads"), (CategoryB, "Curries")], name, categoryId, price, isActive, image);

    [Fact]
    public void Constructor_WithExistingItem_DoesNotThrow()
    {
        using var form = CreateForm();
        Assert.True(form.Width > 0);
    }

    [Fact]
    public void ExistingValues_LoadIntoTheDialog()
    {
        using var form = CreateForm(name: "Leechi", categoryId: CategoryB, price: 250m, isActive: false);

        Assert.Equal("Leechi", form.NameValue);
        Assert.Equal(CategoryB, form.CategoryId);
        Assert.Equal(250m, form.SellingPrice);
        Assert.False(form.ItemIsActive);
        Assert.False(form.ImageCleared);
        Assert.Null(form.PendingImage);
    }

    [Fact]
    public void NoCategory_LoadsAsUnselected()
    {
        using var form = CreateForm(categoryId: null);
        Assert.Null(form.CategoryId);
    }

    [Fact]
    public void ChooseImage_BeforeAnyPick_IsNothingToPersist()
    {
        using var form = CreateForm();
        Assert.False(form.ImageCleared);
        Assert.Null(form.PendingImage);
    }
}
