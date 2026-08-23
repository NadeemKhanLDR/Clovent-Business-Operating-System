using System.Reflection;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;
using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.Catalog.Variants;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.UI;

/// <summary>
/// Final-pass structural layout assertions for the shared WinForms chrome -
/// the automated stand-in for the visual checks a headless environment
/// cannot perform. These instantiate the real forms/controls (no handles,
/// no screenshots), run the same Load-time size recompute the app runs, and
/// assert the invariants every clipping screenshot violated: editors
/// visible with non-zero dimensions, buttons fully inside the client area,
/// and no button allocated less than its own PreferredSize.
/// </summary>
internal static class ControlVisibility
{
    /// <summary>
    /// <see cref="Control.Visible"/> reports false for every control on a
    /// form that has never been shown, so this reads the control's own
    /// visibility state (the flag a Hide()/Visible=false assignment clears)
    /// rather than the parent-computed one.
    /// </summary>
    public static bool IsSelfVisible(Control control) =>
        (bool)typeof(Control)
            .GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, [2])!;
}

public sealed class EditFormLayoutStructureTests
{
    private static readonly Assembly DesktopAssembly = typeof(MasterDataEditFormBase).Assembly;

    private static IEnumerable<Type> DerivedEditForms() =>
        DesktopAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(MasterDataEditFormBase)) && !t.IsAbstract);

    /// <summary>
    /// Builds a value for each constructor parameter so every derived edit
    /// form can be constructed from its real runtime constructor (not just
    /// the parameterless Designer one), covering forms whose field rows are
    /// only fully wired in the runtime constructor.
    /// </summary>
    private static object? SynthesizeArgument(Type parameterType)
    {
        if (parameterType == typeof(string))
        {
            return "Test";
        }

        if (parameterType.IsArray)
        {
            return Array.CreateInstance(parameterType.GetElementType()!, 0);
        }

        if (parameterType.IsGenericType)
        {
            var definition = parameterType.GetGenericTypeDefinition();
            if (definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>) || definition == typeof(List<>))
            {
                var listType = typeof(List<>).MakeGenericType(parameterType.GetGenericArguments());
                return Activator.CreateInstance(listType);
            }
        }

        if (parameterType.IsEnum)
        {
            return Enum.ToObject(parameterType, 0);
        }

        if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
        {
            return Activator.CreateInstance(parameterType);
        }

        return null;
    }

    [Fact]
    public void AllDerivedEditForms_VisibleEditors_NonZeroDimensions_ButtonsInsideClient()
    {
        var failures = new List<string>();
        var instantiated = 0;

        foreach (var type in DerivedEditForms())
        {
            Form? form = null;
            try
            {
                var constructor = type.GetConstructors()
                    .OrderBy(c => c.GetParameters().Length)
                    .First();
                var arguments = constructor.GetParameters()
                    .Select(p => p.HasDefaultValue ? p.DefaultValue : SynthesizeArgument(p.ParameterType))
                    .ToArray();
                form = (Form)constructor.Invoke(arguments);
                instantiated++;

                AssertStructure(type.Name, form, failures);
            }
            catch (Exception ex)
            {
                failures.Add($"{type.Name}: construction/layout threw {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                form?.Dispose();
            }
        }

        Assert.True(instantiated >= 30, $"Expected at least 30 derived edit forms, constructed {instantiated}.");
        Assert.True(failures.Count == 0, $"Structural violations:\n{string.Join("\n", failures)}");
    }

    /// <summary>See <see cref="ControlVisibility.IsSelfVisible"/>.</summary>
    private static bool IsSelfVisible(Control control) => ControlVisibility.IsSelfVisible(control);

    private static void AssertStructure(string typeName, Form form, List<string> failures)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var baseType = typeof(MasterDataEditFormBase);

        var contentPanel = (TableLayoutPanel)baseType
            .GetField("_contentPanel", flags)!
            .GetValue(form)!;
        var buttonPanel = (FlowLayoutPanel)baseType
            .GetField("_buttonPanel", flags)!
            .GetValue(form)!;
        var okButton = (Control)baseType.GetField("_okButton", flags)!.GetValue(form)!;
        var cancelButton = (Control)baseType.GetField("_cancelButton", flags)!.GetValue(form)!;

        // Run the same Load-time recompute the app runs (sizes the dialog
        // from its real content and sets MinimumSize).
        baseType.GetMethod("MasterDataEditFormBase_Load", flags)!.Invoke(form, [form, EventArgs.Empty]);
        form.PerformLayout();

        if (form.Controls.Cast<Control>().Any(c => c.Width == 0 || c.Height == 0))
        {
            failures.Add($"{typeName}: a top-level control has a zero dimension.");
        }

        var editors = contentPanel.Controls
            .Cast<Control>()
            .Where(c => contentPanel.GetColumn(c) == 1 || contentPanel.GetColumnSpan(c) == 2)
            .ToList();
        if (editors.Count == 0)
        {
            failures.Add($"{typeName}: no editor controls are parented into the content panel.");
        }

        if (editors.Count != 0 && editors.All(e => !IsSelfVisible(e)))
        {
            failures.Add($"{typeName}: no editor in the content panel is visible.");
        }

        foreach (var editor in editors)
        {
            if (editor.Width <= 0 || editor.Height <= 0)
            {
                failures.Add($"{typeName}: editor '{editor.Name}' has zero width/height ({editor.Width}x{editor.Height}).");
            }
        }

        foreach (var button in new[] { okButton, cancelButton }.Concat(buttonPanel.Controls.Cast<Control>()))
        {
            var preferred = button.GetPreferredSize(Size.Empty);
            if (button.Width < preferred.Width || button.Height < preferred.Height)
            {
                failures.Add($"{typeName}: button '{button.Text}' is {button.Width}x{button.Height} but needs {preferred.Width}x{preferred.Height}.");
            }

            var client = form.ClientRectangle;
            if (!client.Contains(button.Bounds) && button.FindForm() == form)
            {
                failures.Add($"{typeName}: button '{button.Text}' bounds {button.Bounds} fall outside the client area {client}.");
            }
        }
    }

    private static T GetField<T>(object instance, string name) where T : class
    {
        var type = instance.GetType();
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                return (T)field.GetValue(instance)!;
            }
            type = type.BaseType!;
        }
        throw new ArgumentException($"Field '{name}' not found in hierarchy of {instance.GetType().Name}");
    }

    [Fact]
    public void MenuItemEditForm_VerifyAllRequiredControls()
    {
        using var form = new MenuItemEditForm("Edit Menu Item", []);
        
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MasterDataEditFormBase).GetMethod("MasterDataEditFormBase_Load", flags)!.Invoke(form, [form, EventArgs.Empty]);
        form.PerformLayout();
        
        var nameEdit = GetField<TextEdit>(form, "_nameEdit");
        var categoryCombo = GetField<ComboBoxEdit>(form, "_categoryCombo");
        var priceEdit = GetField<SpinEdit>(form, "_priceEdit");
        var activeEdit = GetField<CheckEdit>(form, "_activeEdit");
        var pictureEdit = GetField<PictureEdit>(form, "_pictureEdit");
        var chooseImageButton = GetField<SimpleButton>(form, "_chooseImageButton");
        var clearImageButton = GetField<SimpleButton>(form, "_clearImageButton");
        
        var okButton = GetField<SimpleButton>(form, "_okButton");
        var saveAndNewButton = GetField<SimpleButton>(form, "_saveAndNewButton");
        var cancelButton = GetField<SimpleButton>(form, "_cancelButton");
        
        Assert.NotNull(nameEdit);
        Assert.NotNull(categoryCombo);
        Assert.NotNull(priceEdit);
        Assert.NotNull(activeEdit);
        Assert.NotNull(pictureEdit);
        Assert.NotNull(chooseImageButton);
        Assert.NotNull(clearImageButton);
        
        Assert.Same(form, nameEdit.FindForm());
        Assert.Same(form, categoryCombo.FindForm());
        Assert.Same(form, priceEdit.FindForm());
        Assert.Same(form, activeEdit.FindForm());
        Assert.Same(form, pictureEdit.FindForm());
        
        Assert.True(ControlVisibility.IsSelfVisible(nameEdit));
        Assert.True(ControlVisibility.IsSelfVisible(categoryCombo));
        Assert.True(ControlVisibility.IsSelfVisible(priceEdit));
        Assert.True(ControlVisibility.IsSelfVisible(activeEdit));
        Assert.True(ControlVisibility.IsSelfVisible(pictureEdit));
        Assert.True(ControlVisibility.IsSelfVisible(chooseImageButton));
        Assert.True(ControlVisibility.IsSelfVisible(clearImageButton));
        Assert.True(ControlVisibility.IsSelfVisible(okButton));
        Assert.True(ControlVisibility.IsSelfVisible(saveAndNewButton));
        Assert.True(ControlVisibility.IsSelfVisible(cancelButton));
        
        Assert.True(nameEdit.Width > 0 && nameEdit.Height > 0);
        Assert.True(categoryCombo.Width > 0 && categoryCombo.Height > 0);
        Assert.True(priceEdit.Width > 0 && priceEdit.Height > 0);
        Assert.True(activeEdit.Width > 0 && activeEdit.Height > 0);
        Assert.True(pictureEdit.Width > 0 && pictureEdit.Height > 0);
        Assert.True(chooseImageButton.Width > 0 && chooseImageButton.Height > 0);
        Assert.True(clearImageButton.Width > 0 && clearImageButton.Height > 0);
        Assert.True(okButton.Width > 0 && okButton.Height > 0);
        Assert.True(saveAndNewButton.Width > 0 && saveAndNewButton.Height > 0);
        Assert.True(cancelButton.Width > 0 && cancelButton.Height > 0);
        
        Assert.True(categoryCombo.Top >= nameEdit.Bottom, "Category combo overlaps Name edit.");
        Assert.True(priceEdit.Top >= categoryCombo.Bottom, "Price edit overlaps Category combo.");
    }

    [Fact]
    public void ProductVariantEditForm_VerifyAllRequiredControls()
    {
        using var form = new ProductVariantEditForm("Edit Variant", [(Guid.NewGuid(), "Each")]);
        
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MasterDataEditFormBase).GetMethod("MasterDataEditFormBase_Load", flags)!.Invoke(form, [form, EventArgs.Empty]);
        form.PerformLayout();
        
        var nameEdit = GetField<TextEdit>(form, "_nameEdit");
        var skuEdit = GetField<TextEdit>(form, "_skuEdit");
        var unitCombo = GetField<ComboBoxEdit>(form, "_unitCombo");
        
        var okButton = GetField<SimpleButton>(form, "_okButton");
        var cancelButton = GetField<SimpleButton>(form, "_cancelButton");
        
        Assert.NotNull(nameEdit);
        Assert.NotNull(skuEdit);
        Assert.NotNull(unitCombo);
        
        Assert.Same(form, nameEdit.FindForm());
        Assert.Same(form, skuEdit.FindForm());
        Assert.Same(form, unitCombo.FindForm());
        
        Assert.True(ControlVisibility.IsSelfVisible(nameEdit));
        Assert.True(ControlVisibility.IsSelfVisible(skuEdit));
        Assert.True(ControlVisibility.IsSelfVisible(unitCombo));
        Assert.True(ControlVisibility.IsSelfVisible(okButton));
        Assert.True(ControlVisibility.IsSelfVisible(cancelButton));
        
        Assert.True(nameEdit.Width > 0 && nameEdit.Height > 0);
        Assert.True(skuEdit.Width > 0 && skuEdit.Height > 0);
        Assert.True(unitCombo.Width > 0 && unitCombo.Height > 0);
        Assert.True(okButton.Width > 0 && okButton.Height > 0);
        Assert.True(cancelButton.Width > 0 && cancelButton.Height > 0);
        
        Assert.True(skuEdit.Top >= nameEdit.Bottom, "SKU edit overlaps Name edit.");
        Assert.True(unitCombo.Top >= skuEdit.Bottom, "Unit combo overlaps SKU edit.");
    }
}

public sealed class CategoryColorDialogTests
{
    private static T GetField<T>(object instance, string name) where T : class =>
        (T)typeof(CategoryColorDialog)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!;

    [Fact]
    public void ColorEdit_IsVisible_InContentPanel_AndLoadsCurrentColor()
    {
        using var dialog = new CategoryColorDialog("Beverages", "#3B82F6");
        var colorEdit = GetField<ColorEdit>(dialog, "_colorEdit");

        Assert.True(ControlVisibility.IsSelfVisible(colorEdit));
        Assert.True(colorEdit.Width > 0 && colorEdit.Height > 0);
        Assert.NotNull(colorEdit.Parent);
        Assert.Equal(Color.FromArgb(0x3B, 0x82, 0xF6), colorEdit.Color);
        Assert.Equal("#3B82F6", dialog.ColorHex);
    }

    [Fact]
    public void SelectedColor_IsReflectedInColorHex_UntilCleared()
    {
        using var dialog = new CategoryColorDialog("Beverages", "#3B82F6");
        var colorEdit = GetField<ColorEdit>(dialog, "_colorEdit");

        // Selecting a color in the DevExpress picker updates ColorEdit.Color;
        // the dialog's ColorHex must round-trip it.
        colorEdit.Color = Color.Red;
        Assert.Equal("#FF0000", dialog.ColorHex);

        var clearCheck = GetField<CheckEdit>(dialog, "_clearCheck");
        clearCheck.Checked = true;
        Assert.False(colorEdit.Enabled);
        Assert.Null(dialog.ColorHex);
    }

    [Fact]
    public void OkClose_ReturnsDialogResultOk_ValidationAlwaysPasses()
    {
        using var dialog = new CategoryColorDialog("Beverages", null);
        var method = typeof(MasterDataEditFormBase)
            .GetMethod("TryClose", BindingFlags.Instance | BindingFlags.NonPublic)!;

        method.Invoke(dialog, [false]);
        Assert.Equal(DialogResult.OK, dialog.DialogResult);

        // Cancel path: a dialog whose TryClose was never invoked keeps
        // DialogResult.None - the caller (MenuItemsForm) only persists
        // ColorHex after DialogResult.OK, so Cancel cannot persist.
        using var cancelled = new CategoryColorDialog("Beverages", "#3B82F6");
        Assert.Equal(DialogResult.None, cancelled.DialogResult);
        Assert.Equal("#3B82F6", cancelled.ColorHex);
    }
}

public sealed class CommandPanelLayoutTests
{
    [Fact]
    public void Splitter_SettlesAtContentDrivenWidth_AndRecoversFromCollapse()
    {
        using var host = new UserControl { Dock = DockStyle.Fill };
        var content = new Control { Dock = DockStyle.Fill };
        var commandFlow = CommandPanelLayout.Build(host, content);

        var search = new TextEdit();
        CommandPanelLayout.AddEditor(commandFlow, search);
        SimpleButton? newButton = null;
        foreach (var caption in new[] { "New", "Edit", "Activate", "Deactivate", "Refresh" })
        {
            var button = new SimpleButton { Text = caption };
            CommandPanelLayout.AddCommandButton(commandFlow, button);
            newButton ??= button;
        }

        host.Size = new Size(1000, 600);
        host.PerformLayout();

        var split = host.Controls.OfType<SplitContainer>().Single();
        var floor = DesktopDpi.Scale(CommandPanelLayout.Width, split);
        Assert.True(split.SplitterDistance >= floor,
            $"Splitter settled at {split.SplitterDistance}, below the content-driven floor {floor}.");

        Assert.True(search.Visible && search.Width > 0);
        Assert.True(newButton!.Visible && newButton.Width > 0 && newButton.Height > 0);
        Assert.True(newButton.AutoSize, "Command buttons must be AutoSize so captions never clip.");
        Assert.True(newButton.MinimumSize.Width > 0);

        // A user (or a DevExpress tab re-layout) collapsing the splitter to
        // its min must not stick: the next resize re-runs the content-driven
        // restore.
        split.SplitterDistance = split.Panel1MinSize;
        host.Width += 10;
        host.PerformLayout();
        Assert.True(split.SplitterDistance >= floor,
            $"Splitter stayed collapsed at {split.SplitterDistance} after a resize.");
    }
}

public sealed class MasterDataListViewStructureTests
{
    private static T GetField<T>(object instance, string name) where T : class =>
        (T)typeof(MasterDataListView<BranchDto>)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!;

    [Fact]
    public void SearchBoxAndCommandButtons_AreVisible_NonZero_AndDpiScaled()
    {
        using var listView = new MasterDataListView<BranchDto>();
        var searchBox = GetField<TextEdit>(listView, "_searchBox");
        var buttons = new[]
        {
            GetField<SimpleButton>(listView, "_newButton"),
            GetField<SimpleButton>(listView, "_editButton"),
            GetField<SimpleButton>(listView, "_activateButton"),
            GetField<SimpleButton>(listView, "_deactivateButton"),
            GetField<SimpleButton>(listView, "_refreshButton"),
        };

        Assert.True(searchBox.Visible);
        Assert.True(searchBox.Width > 0, "Search box must be initialized to a non-zero (DPI-scaled) width.");

        foreach (var button in buttons)
        {
            Assert.True(button.Visible, $"Command button '{button.Text}' must be visible.");
            Assert.True(button.AutoSize, $"Command button '{button.Text}' must be AutoSize.");
            Assert.True(button.MinimumSize.Width > 0, $"Command button '{button.Text}' needs a scaled minimum width.");
        }

        // After a real layout pass, the splitter must settle at the
        // content-driven sidebar width, never at the 40px collapse floor.
        listView.Size = new Size(1000, 600);
        listView.PerformLayout();
        var split = listView.Controls.OfType<SplitContainer>().Single();
        var floor = DesktopDpi.Scale(240, split);
        Assert.True(split.SplitterDistance >= floor,
            $"Splitter settled at {split.SplitterDistance}, below the content-driven floor {floor}.");
        Assert.True(searchBox.Width >= floor - 40,
            $"Search box width {searchBox.Width} cannot fill the panel (panel floor {floor}).");
    }
}
