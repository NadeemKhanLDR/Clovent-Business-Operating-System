using System;
using System.Windows.Forms;
using System.Threading;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraEditors;
using DevExpress.XtraBars;

namespace Clovent.Desktop.Forms.Base.Localization;

/// <summary>
/// Helper utility to apply translations and RTL settings recursively to forms, user controls, ribbons, and grid columns.
/// </summary>
public static class LocalizationHelper
{
    /// <summary>Returns true if the current UI culture is Urdu.</summary>
    public static bool IsRtl => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "ur";

    /// <summary>Recursively localizes a control and its children, applying translation and setting RightToLeft.</summary>
    public static void LocalizeControl(Control control)
    {
        if (control == null) return;

        bool keepLtr = false;
        var name = control.Name ?? "";
        if (control is TextEdit || control is TextBox || control.GetType().Name.Contains("ComboBox"))
        {
            if (name.Contains("Code", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Email", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Phone", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Number", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Balance", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Discount", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Tax", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Qty", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Quantity", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Capacity", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Rate", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Barcode", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Sku", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Percent", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Format", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Password", StringComparison.OrdinalIgnoreCase))
            {
                keepLtr = true;
            }
        }
        else if (control.GetType().Name.Contains("SpinEdit") || 
                 control.GetType().Name.Contains("CalcEdit") || 
                 control.GetType().Name.Contains("Numeric"))
        {
            keepLtr = true;
        }

        control.RightToLeft = (IsRtl && !keepLtr) ? RightToLeft.Yes : RightToLeft.No;

        if (control is SimpleButton simpleButton)
        {
            var text = simpleButton.Text;
            if (!string.IsNullOrEmpty(text))
            {
                string prefix = "";
                if (text.StartsWith("✕  ")) { prefix = "✕  "; text = text.Substring(3); }
                else if (text.StartsWith("+  ")) { prefix = "+  "; text = text.Substring(3); }
                else if (text.StartsWith("▦  ")) { prefix = "▦  "; text = text.Substring(3); }

                var localized = PosStrings.Get(text.Trim());
                if (localized != text)
                {
                    simpleButton.Text = prefix + localized;
                }
            }
        }
        else if (control is LabelControl labelControl)
        {
            var text = labelControl.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var cleanText = text.TrimEnd(':', ' ', '*');
                var suffix = text.Substring(cleanText.Length);
                var localized = PosStrings.Get(cleanText);
                if (localized != cleanText)
                {
                    labelControl.Text = localized + suffix;
                }
            }
        }
        else if (control is Label label)
        {
            var text = label.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var cleanText = text.TrimEnd(':', ' ', '*');
                var suffix = text.Substring(cleanText.Length);
                var localized = PosStrings.Get(cleanText);
                if (localized != cleanText)
                {
                    label.Text = localized + suffix;
                }
            }
        }
        else if (control is TextEdit textEdit)
        {
            if (!string.IsNullOrEmpty(textEdit.Properties.NullValuePrompt))
            {
                var localizedPrompt = PosStrings.Get(textEdit.Properties.NullValuePrompt);
                if (localizedPrompt != textEdit.Properties.NullValuePrompt)
                {
                    textEdit.Properties.NullValuePrompt = localizedPrompt;
                }
            }
        }
        else if (control is GridControl gridControl)
        {
            foreach (var view in gridControl.ViewCollection)
            {
                if (view is GridView gridView)
                {
                    gridView.CustomColumnDisplayText -= GridView_DateTimeFormattingHandler;
                    gridView.CustomColumnDisplayText += GridView_DateTimeFormattingHandler;

                    foreach (GridColumn col in gridView.Columns)
                    {
                        var caption = col.Caption;
                        if (caption.EndsWith(" (UTC)", StringComparison.OrdinalIgnoreCase))
                        {
                            caption = caption.Substring(0, caption.Length - 6);
                        }

                        if (!string.IsNullOrEmpty(caption))
                        {
                            var localized = PosStrings.Get(caption);
                            col.Caption = localized;
                        }
                    }
                }
            }
        }

        foreach (Control child in control.Controls)
        {
            LocalizeControl(child);
        }
    }

    /// <summary>Localizes a DevExpress RibbonControl, translating all pages, groups, and items.</summary>
    public static void LocalizeRibbon(DevExpress.XtraBars.Ribbon.RibbonControl ribbon)
    {
        if (ribbon == null) return;

        ribbon.RightToLeft = IsRtl ? RightToLeft.Yes : RightToLeft.No;

        foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in ribbon.Pages)
        {
            if (!string.IsNullOrEmpty(page.Text))
            {
                page.Text = PosStrings.Get(page.Text);
            }

            foreach (DevExpress.XtraBars.Ribbon.RibbonPageGroup group in page.Groups)
            {
                if (!string.IsNullOrEmpty(group.Text))
                {
                    group.Text = PosStrings.Get(group.Text);
                }
            }
        }

        foreach (BarItem item in ribbon.Items)
        {
            if (!string.IsNullOrEmpty(item.Caption))
            {
                if (item.Name == "_profileMenu" || item.Name == "_userStatusItem")
                {
                    continue;
                }

                if (item.Caption.StartsWith("Notifications"))
                {
                    var index = item.Caption.IndexOf('(');
                    if (index >= 0)
                    {
                        var suffix = item.Caption.Substring(index);
                        item.Caption = PosStrings.Get("Notifications") + " " + suffix;
                    }
                    else
                    {
                        item.Caption = PosStrings.Get(item.Caption);
                    }
                }
                else
                {
                    item.Caption = PosStrings.Get(item.Caption);
                }
            }
        }
    }

    private static void GridView_DateTimeFormattingHandler(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Value is DateTimeOffset dto)
        {
            e.DisplayText = DateTimeDisplay.Format(dto);
        }
        else if (e.Value is DateTime dt)
        {
            e.DisplayText = DateTimeDisplay.Format(dt);
        }
    }
}
