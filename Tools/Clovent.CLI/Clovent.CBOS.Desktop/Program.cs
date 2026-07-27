using System;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;

namespace Clovent.CBOS.Desktop;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        BonusSkins.Register();
        SkinManager.EnableFormSkins();
        SkinManager.EnableMdiFormSkins();

        UserLookAndFeel.Default.SetSkinStyle("Office 2019 Colorful");

        Application.Run(new LoginForm());
    }
}