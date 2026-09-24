using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Clovent.Catalog;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Restaurant.Application.SmartCombos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class LayoutTreeDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public LayoutTreeDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Diagnostic_Dump_SmartComboBuilderView_LayoutTree()
    {
        RunSta(() =>
        {
            var services = new ServiceCollection();
            services.AddScoped<IMediator>(_ => new FakeMediator());
            services.AddScoped<ISmartComboAccess>(_ => new FakeAccess());
            using var provider = services.BuildServiceProvider();

            SmartPosLayoutTelemetry.LogAppBuildIdentity();
            using var hostForm = new Form
            {
                Size = new Size(1920, 1080),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(0, 0)
            };

            var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new SmartComboOptions { PeriodDays = 30 });
            view.Dock = DockStyle.Fill;
            hostForm.Controls.Add(view);

            hostForm.Show();
            Application.DoEvents();

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("SMART COMBO BUILDER VIEW - RUNTIME LAYOUT TREE DIAGNOSTIC");
            sb.AppendLine("================================================================================");
            DumpControlTree(view, 0, sb);

            _output.WriteLine(sb.ToString());
            File.WriteAllText("D:\\Clovent Business Operating System\\scratch\\smart_combo_layout_tree.txt", sb.ToString());
            hostForm.Close();
            Application.DoEvents();
        });
    }

    [Fact]
    public void Diagnostic_Dump_QuickOrderTemplateEditForm_LayoutTree()
    {
        RunSta(() =>
        {
            var options = new List<ProductOptionRow>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Chicken Haleem", "Full Plate", 420.00m),
                new(Guid.NewGuid(), Guid.NewGuid(), "Garlic Nan", "Standard", 50.00m),
                new(Guid.NewGuid(), Guid.NewGuid(), "Fresh Salad", "Standard", 30.00m)
            };

            var existing = new QuickOrderTemplateEditModel(
                "Special Lunch Deal",
                "1 Chicken Haleem Full Plate + 2 Garlic Nan + 1 Fresh Salad",
                3,
                [
                    (options[0].VariantId, 1m, 420.00m),
                    (options[1].VariantId, 2m, 50.00m),
                    (options[2].VariantId, 1m, 30.00m)
                ],
                true);

            using var hostForm = new Form
            {
                Size = new Size(1920, 1080),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(0, 0)
            };
            hostForm.Show();
            Application.DoEvents();

            using var form = new QuickOrderTemplateEditForm("Edit Quick Order Template", options, existing);
            form.Owner = hostForm;
            form.Show();
            Application.DoEvents();

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("QUICK ORDER TEMPLATE EDIT FORM - RUNTIME LAYOUT TREE DIAGNOSTIC");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Form.Size={form.Size}, Form.ClientSize={form.ClientSize}, Form.MinimumSize={form.MinimumSize}");
            DumpControlTree(form, 0, sb);

            _output.WriteLine(sb.ToString());
            File.WriteAllText("D:\\Clovent Business Operating System\\scratch\\quick_order_edit_layout_tree.txt", sb.ToString());
            form.Close();
            hostForm.Close();
        });
    }

    private static void DumpControlTree(Control c, int depth, StringBuilder sb)
    {
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}Control: {c.GetType().Name} [Name='{c.Name}'] Parent: {c.Parent?.GetType().Name} [Name='{c.Parent?.Name}']");
        sb.AppendLine($"{indent}  Bounds: {c.Bounds} ClientRect: {c.ClientRectangle} PreferredSize: {c.PreferredSize} Min: {c.MinimumSize} Max: {c.MaximumSize}");
        sb.AppendLine($"{indent}  Dock: {c.Dock} Anchor: {c.Anchor} AutoSize: {c.AutoSize} Visible: {c.Visible}");
        sb.AppendLine($"{indent}  Margin: {c.Margin} Padding: {c.Padding} Font: {c.Font.Name} {c.Font.Size}pt DeviceDpi: {c.DeviceDpi}");

        if (c is ContainerControl cc)
        {
            sb.AppendLine($"{indent}  Container: AutoScaleMode={cc.AutoScaleMode} AutoScaleDimensions={cc.AutoScaleDimensions} CurrentAutoScaleDimensions={cc.CurrentAutoScaleDimensions}");
        }

        if (c is TableLayoutPanel tlp)
        {
            sb.AppendLine($"{indent}  TableLayoutPanel: Rows={tlp.RowCount} Cols={tlp.ColumnCount} AutoSizeMode={tlp.AutoSizeMode}");
            for (int r = 0; r < tlp.RowStyles.Count; r++)
            {
                var rs = tlp.RowStyles[r];
                sb.AppendLine($"{indent}    Row[{r}]: SizeType={rs.SizeType}, Height={rs.Height}");
            }
            for (int col = 0; col < tlp.ColumnStyles.Count; col++)
            {
                var cs = tlp.ColumnStyles[col];
                sb.AppendLine($"{indent}    Col[{col}]: SizeType={cs.SizeType}, Width={cs.Width}");
            }
        }

        if (c.Parent is TableLayoutPanel parentTlp)
        {
            sb.AppendLine($"{indent}  InParentTable: Row={parentTlp.GetRow(c)}, Col={parentTlp.GetColumn(c)}, RowSpan={parentTlp.GetRowSpan(c)}, ColSpan={parentTlp.GetColumnSpan(c)}");
        }

        if (c is FlowLayoutPanel flp)
        {
            sb.AppendLine($"{indent}  FlowLayoutPanel: Direction={flp.FlowDirection}, Wrap={flp.WrapContents}, AutoSizeMode={flp.AutoSizeMode}");
        }

        if (c is BaseEdit be)
        {
            sb.AppendLine($"{indent}  DevExpress.BaseEdit: AutoHeight={be.Properties.AutoHeight}");
        }

        foreach (Control child in c.Controls)
        {
            DumpControlTree(child, depth + 1, sb);
        }
    }

    private static void RunSta(Action action)
    {
        Exception? err = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                err = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(20)))
        {
            thread.Interrupt();
            throw new TimeoutException("Test timed out.");
        }
        if (err != null) throw err;
    }

    private sealed class FakeMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TResponse>(default!);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            Task.CompletedTask;
    }

    private sealed class FakeAccess : ISmartComboAccess
    {
        public Task<Guid> RequireAsync(string operation, Guid warehouseId, CancellationToken ct) => Task.FromResult(Guid.NewGuid());
        public Task<IReadOnlyList<ComboLocation>> LocationsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ComboLocation>>([new(Guid.NewGuid(), "Main Warehouse")]);
        public Task<ComboCurrency> CurrencyAsync(Guid warehouseId, CancellationToken ct) => Task.FromResult(new ComboCurrency(Guid.NewGuid(), 2, "Rs."));
    }
}
