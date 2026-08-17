using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Shared;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Shared;

public class TextPromptFormTests
{
    private static T GetField<T>(object target, string fieldName) where T : class
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                return (T)field.GetValue(target)!;
            }
            type = type.BaseType;
        }
        throw new InvalidOperationException($"Field {fieldName} not found on {target.GetType()}");
    }

    private static void RaiseLoad(Form form)
    {
        var method = typeof(MasterDataEditFormBase).GetMethod(
            "MasterDataEditFormBase_Load",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("MasterDataEditFormBase_Load method not found");
        }
        method.Invoke(form, new object[] { null!, EventArgs.Empty });
    }

    private static bool InvokeValidateFields(object form, out string error)
    {
        var method = form.GetType().GetMethod("ValidateFields", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("ValidateFields method not found");
        }
        var args = new object[] { "" };
        var result = (bool)method.Invoke(form, args)!;
        error = (string)args[0];
        return result;
    }

    [Fact]
    public void VoidReason_EmptyInput_IsRejected()
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        RaiseLoad(form);

        var textEdit = GetField<DevExpress.XtraEditors.MemoEdit>(form, "_textEdit");
        textEdit.Text = "   "; // whitespace

        var success = InvokeValidateFields(form, out var error);
        Assert.False(success);
        Assert.Equal("This field is required.", error);
        Assert.Null(form.Value);
    }

    [Fact]
    public void VoidReason_ValidInput_IsAccepted()
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        RaiseLoad(form);

        var textEdit = GetField<DevExpress.XtraEditors.MemoEdit>(form, "_textEdit");
        textEdit.Text = "  Spilled drink  ";

        var success = InvokeValidateFields(form, out var error);
        Assert.True(success);
        Assert.Empty(error);
        Assert.Equal("Spilled drink", form.Value);
    }

    [Fact]
    public void Form_LayoutAndButtons_AreConfiguredCorrectly()
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        RaiseLoad(form);

        var textEdit = GetField<DevExpress.XtraEditors.MemoEdit>(form, "_textEdit");
        var okButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_okButton");
        var cancelButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_cancelButton");

        // Verify button text
        Assert.Equal("Confirm", okButton.Text);
        Assert.Equal("Cancel", cancelButton.Text);

        // Verify layout styles
        Assert.Equal(DockStyle.Fill, textEdit.Dock);
        Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
        Assert.False(form.MaximizeBox);
        Assert.False(form.MinimizeBox);

        // Verify sizing bounds
        Assert.True(form.Width > 0);
        Assert.True(form.Height > 0);
        Assert.True(form.MaximumSize.Width <= 600);
        Assert.True(form.MaximumSize.Height <= 250);

        // Verify controls fit within form bounds
        var clientW = form.ClientSize.Width;
        var clientH = form.ClientSize.Height;

        Assert.True(textEdit.Right <= clientW);
        Assert.True(okButton.Right <= clientW);
        Assert.True(cancelButton.Right <= clientW);
    }

    [Theory]
    [InlineData("Short Label")]
    [InlineData("Very long label text that wraps across multiple lines and would normally cause layout constraints if the form did not scale correctly")]
    public void Form_ButtonsDoNotOverlapInput_WithVariousLabelLengths(string labelText)
    {
        using var form = new TextPromptForm("Void Order", labelText, required: true);
        RaiseLoad(form);
        form.Show();
        
        var contentPanel = GetField<TableLayoutPanel>(form, "_contentPanel");
        var buttonPanel = GetField<FlowLayoutPanel>(form, "_buttonPanel");
        contentPanel.Dock = DockStyle.None;
        contentPanel.Size = new Size(form.ClientSize.Width, form.ClientSize.Height - buttonPanel.Height);
        contentPanel.Location = new Point(0, 0);
        contentPanel.PerformLayout();

        var textEdit = GetField<DevExpress.XtraEditors.MemoEdit>(form, "_textEdit");
        var okButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_okButton");
        var cancelButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_cancelButton");

        Point GetLocationOnForm(Control control)
        {
            Point p = control.Location;
            Control? parent = control.Parent;
            while (parent != null && parent != form)
            {
                p.Offset(parent.Location);
                parent = parent.Parent;
            }
            return p;
        }

        var parent1 = textEdit.Parent;
        var parent2 = parent1?.Parent;
        var inputRect = new Rectangle(GetLocationOnForm(textEdit), textEdit.Size);
        var okRect = new Rectangle(GetLocationOnForm(okButton), okButton.Size);
        var cancelRect = new Rectangle(GetLocationOnForm(cancelButton), cancelButton.Size);

        // Assert no overlap
        Assert.True(okRect.Top >= inputRect.Bottom, $"OK button overlaps input field (okRect: {okRect}, inputRect: {inputRect})");
        Assert.True(cancelRect.Top >= inputRect.Bottom, $"Cancel button overlaps input field (cancelRect: {cancelRect}, inputRect: {inputRect})");

        // Confirm buttons are visible and active
        Assert.True(okButton.Visible);
        Assert.True(cancelButton.Visible);
    }
}
