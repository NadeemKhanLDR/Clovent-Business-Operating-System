using System.Reflection;

namespace Clovent.Desktop.UiQa;

/// <summary>
/// Temporary real-rendering QA harness: instantiates each edit form from
/// its real runtime constructor, shows it off-screen at the machine's real
/// DPI, pumps messages so DevExpress layout runs, then dumps every control's
/// actual rendered Name/Type/Text/Visible/Bounds. This is genuine Windows
/// rendering - not structural guesses.
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var desktopAssembly = typeof(Clovent.Desktop.MasterData.MasterDataEditFormBase).Assembly;
        var wanted = args.Length > 0 ? args[0] : "*";

        foreach (var type in desktopAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Form)) && !t.IsAbstract).OrderBy(t => t.Name))
        {
            if (wanted != "*" && !type.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Form? form = null;
            try
            {
                var ctor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
                var ctorArgs = ctor.GetParameters().Select(p => Synthesize(p.ParameterType, p.HasDefaultValue, p.DefaultValue)).ToArray();
                form = (Form)ctor.Invoke(ctorArgs);

                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-32000, -32000);
                form.Show();
                Application.DoEvents();
                System.Threading.Thread.Sleep(250);
                Application.DoEvents();

                Console.WriteLine($"=== {type.Name} | ClientSize {form.ClientSize.Width}x{form.ClientSize.Height} | DPI {form.DeviceDpi} ===");
                Dump(form, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== {type.Name} | CONSTRUCTION FAILED: {ex.GetType().Name}: {ex.Message} ===");
            }
            finally
            {
                form?.Close();
                form?.Dispose();
            }
        }
    }

    private static object? Synthesize(Type type, bool hasDefault, object? defaultValue)
    {
        if (hasDefault) return defaultValue;
        if (type == typeof(string)) return "QA";
        if (type.IsArray) return Array.CreateInstance(type.GetElementType()!, 0);
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IReadOnlyList<>) || def == typeof(IEnumerable<>) || def == typeof(List<>))
            {
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()));
            }
        }
        if (type.IsEnum) return Enum.ToObject(type, 0);
        if (type.IsValueType && Nullable.GetUnderlyingType(type) is null) return Activator.CreateInstance(type);
        return null;
    }

    private static void Dump(Control control, int depth)
    {
        var indent = new string(' ', depth * 2);
        foreach (Control child in control.Controls)
        {
            var interesting = child is DevExpress.XtraEditors.TextEdit
                or DevExpress.XtraEditors.ComboBoxEdit
                or DevExpress.XtraEditors.SpinEdit
                or DevExpress.XtraEditors.CheckEdit
                or DevExpress.XtraEditors.SimpleButton
                or DevExpress.XtraEditors.LabelControl
                or DevExpress.XtraEditors.PictureEdit
                or DevExpress.XtraEditors.ColorEdit
                or DevExpress.XtraEditors.MemoEdit
                or DevExpress.XtraEditors.CheckedListBoxControl
                or System.Windows.Forms.Label
                or System.Windows.Forms.Button
                or System.Windows.Forms.FlowLayoutPanel
                or System.Windows.Forms.TableLayoutPanel;
            if (interesting)
            {
                var text = child.Text is { Length: > 0 } t ? t.Replace("\n", " ") : "";
                var zero = child.Width <= 0 || child.Height <= 0 ? " *** ZERO-SIZE ***" : "";
                var hidden = !child.Visible ? " [hidden]" : "";
                Console.WriteLine($"{indent}{child.GetType().Name} '{child.Name}' \"{text}\" {child.Width}x{child.Height} @({child.Left},{child.Top}){hidden}{zero}");
            }
            Dump(child, depth + 1);
        }
    }
}
