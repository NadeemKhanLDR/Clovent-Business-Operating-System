using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Forms.Identity;

partial class LoginForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    // Predefined, not computed inline at the InitializeComponent call site -
    // every argument expression written directly inside InitializeComponent
    // must be something the Visual Studio Designer's own restricted code
    // parser can round-trip, and only a plain field reference is confirmed
    // safe there (an inline Color.FromArgb(...) call is an unconfirmed,
    // likely-unsafe expression shape - see RestaurantPosView.Designer.cs's
    // own AccentColor/SuccessColor/etc. fields for the established,
    // already-working pattern this copies).
    private static readonly Color PosAccentColor = Color.FromArgb(13, 148, 136);
    private static readonly Color BackOfficeAccentColor = Color.FromArgb(32, 45, 74);

    /// <summary>Clean up any resources being used.</summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    /// <remarks>
    /// Structure: <c>pnlBranding</c> (Dock=Left, fixed width) holds
    /// <c>tlpBranding</c> - a 1-column, 4-row <see cref="TableLayoutPanel"/>
    /// (Percent/AutoSize/AutoSize/Percent) that vertically centers the
    /// logo+tagline regardless of window height. <c>pnlForm</c> (Dock=Fill)
    /// holds <c>tlpForm</c> - a 1-column TableLayoutPanel with one row per
    /// field, a Percent spacer above and below so the whole field group
    /// centers vertically too; every field control is <c>Dock=Fill</c>
    /// within its own exclusive row/cell (never shared with a sibling), so
    /// it stretches to the full available width with zero Anchor math and
    /// zero risk of the old fixed-Location/no-Anchor overlap bug. The
    /// module choice - previously a separate screen shown after a
    /// successful login - is now the last two rows of this same
    /// <c>tlpForm</c>: a "SELECT MODULE" caption (<c>lblSelectModule</c>)
    /// and <c>tlpModules</c>, a 2-column row hosting <c>cardPos</c>/
    /// <c>cardBackOffice</c> - each card is a <see cref="DevExpress.XtraEditors.PanelControl"/>
    /// containing three of its own child <see cref="DevExpress.XtraEditors.LabelControl"/>s
    /// (icon, title, subtitle), every one a genuine, individually
    /// Designer-selectable/editable control (never built at runtime) - a
    /// plain <see cref="DevExpress.XtraEditors.SimpleButton"/> cannot host
    /// child controls, which is why these are panels, not buttons; the
    /// click/hover/pressed/keyboard-focus behavior a real button would give
    /// for free is instead wired by hand in <c>LoginForm.cs</c> (see
    /// <c>Card_MouseEnter</c>/<c>Card_MouseLeave</c>/<c>Card_MouseDown</c>/
    /// <c>Card_MouseUp</c>/<c>Card_Enter</c>/<c>Card_Leave</c>/<c>Card_KeyDown</c>),
    /// applied identically to a card and each of its three labels so hovering
    /// or pressing anywhere on the card - not just its bare background -
    /// reacts (a child label's own mouse events fire independently of its
    /// parent panel's, exactly like every other "whole-card-clickable"
    /// surface already built this way elsewhere in this codebase).
    /// </remarks>
    private void InitializeComponent()
    {
        pnlBranding = new DevExpress.XtraEditors.PanelControl();
        tlpBranding = new TableLayoutPanel();
        lblLogo = new DevExpress.XtraEditors.LabelControl();
        lblTagline = new DevExpress.XtraEditors.LabelControl();
        pnlForm = new DevExpress.XtraEditors.PanelControl();
        tlpForm = new TableLayoutPanel();
        lblTitle = new DevExpress.XtraEditors.LabelControl();
        lblUsername = new DevExpress.XtraEditors.LabelControl();
        txtUsername = new DevExpress.XtraEditors.TextEdit();
        lblPassword = new DevExpress.XtraEditors.LabelControl();
        txtPassword = new DevExpress.XtraEditors.TextEdit();
        lblPin = new DevExpress.XtraEditors.LabelControl();
        txtPin = new DevExpress.XtraEditors.TextEdit();
        chkRememberMe = new DevExpress.XtraEditors.CheckEdit();
        lblLanguage = new DevExpress.XtraEditors.LabelControl();
        cmbLanguage = new DevExpress.XtraEditors.ComboBoxEdit();
        lblTheme = new DevExpress.XtraEditors.LabelControl();
        cmbTheme = new DevExpress.XtraEditors.ComboBoxEdit();
        lblError = new DevExpress.XtraEditors.LabelControl();
        prgLoading = new DevExpress.XtraEditors.ProgressBarControl();
        lblSelectModule = new DevExpress.XtraEditors.LabelControl();
        tlpModules = new TableLayoutPanel();
        cardPos = new DevExpress.XtraEditors.PanelControl();
        tlpCardPos = new TableLayoutPanel();
        lblPosSubtitle = new DevExpress.XtraEditors.LabelControl();
        lblPosTitle = new DevExpress.XtraEditors.LabelControl();
        lblPosIcon = new DevExpress.XtraEditors.LabelControl();
        cardBackOffice = new DevExpress.XtraEditors.PanelControl();
        tlpCardBackOffice = new TableLayoutPanel();
        lblBackOfficeSubtitle = new DevExpress.XtraEditors.LabelControl();
        lblBackOfficeTitle = new DevExpress.XtraEditors.LabelControl();
        lblBackOfficeIcon = new DevExpress.XtraEditors.LabelControl();
        btnCancelHidden = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)pnlBranding).BeginInit();
        pnlBranding.SuspendLayout();
        tlpBranding.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlForm).BeginInit();
        pnlForm.SuspendLayout();
        tlpForm.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)txtUsername.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)txtPassword.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)txtPin.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)chkRememberMe.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)cmbLanguage.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)cmbTheme.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)prgLoading.Properties).BeginInit();
        tlpModules.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cardPos).BeginInit();
        cardPos.SuspendLayout();
        tlpCardPos.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cardBackOffice).BeginInit();
        cardBackOffice.SuspendLayout();
        tlpCardBackOffice.SuspendLayout();
        SuspendLayout();
        // 
        // pnlBranding
        // 
        pnlBranding.Appearance.BackColor = Color.FromArgb(32, 45, 74);
        pnlBranding.Appearance.Options.UseBackColor = true;
        pnlBranding.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        pnlBranding.Controls.Add(tlpBranding);
        pnlBranding.Dock = DockStyle.Left;
        pnlBranding.Location = new Point(0, 0);
        pnlBranding.Margin = new Padding(4, 4, 4, 4);
        pnlBranding.Name = "pnlBranding";
        pnlBranding.Size = new Size(260, 580);
        pnlBranding.TabIndex = 0;
        // 
        // tlpBranding
        // 
        tlpBranding.ColumnCount = 1;
        tlpBranding.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpBranding.Controls.Add(lblLogo, 0, 1);
        tlpBranding.Controls.Add(lblTagline, 0, 2);
        tlpBranding.Dock = DockStyle.Fill;
        tlpBranding.Location = new Point(0, 0);
        tlpBranding.Margin = new Padding(4, 4, 4, 4);
        tlpBranding.Name = "tlpBranding";
        tlpBranding.Padding = new Padding(20, 0, 20, 0);
        tlpBranding.RowCount = 4;
        tlpBranding.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpBranding.RowStyles.Add(new RowStyle());
        tlpBranding.RowStyles.Add(new RowStyle());
        tlpBranding.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpBranding.Size = new Size(260, 580);
        tlpBranding.TabIndex = 0;
        // 
        // lblLogo
        // 
        lblLogo.AccessibleName = "Clovent Business Operating System logo";
        lblLogo.Appearance.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
        lblLogo.Appearance.ForeColor = Color.White;
        lblLogo.Appearance.Options.UseFont = true;
        lblLogo.Appearance.Options.UseForeColor = true;
        lblLogo.Appearance.Options.UseTextOptions = true;
        lblLogo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblLogo.Dock = DockStyle.Top;
        lblLogo.Location = new Point(20, 110);
        lblLogo.Margin = new Padding(4, 4, 4, 4);
        lblLogo.Name = "lblLogo";
        lblLogo.Padding = new Padding(0, 20, 0, 10);
        lblLogo.Size = new Size(220, 120);
        lblLogo.TabIndex = 0;
        lblLogo.Text = "CBOS";
        // 
        // lblTagline
        // 
        lblTagline.Appearance.Font = new Font("Segoe UI", 10F);
        lblTagline.Appearance.ForeColor = Color.FromArgb(200, 210, 230);
        lblTagline.Appearance.Options.UseFont = true;
        lblTagline.Appearance.Options.UseForeColor = true;
        lblTagline.Appearance.Options.UseTextOptions = true;
        lblTagline.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblTagline.Dock = DockStyle.Top;
        lblTagline.Location = new Point(20, 240);
        lblTagline.Margin = new Padding(4, 4, 4, 4);
        lblTagline.Name = "lblTagline";
        lblTagline.Padding = new Padding(0, 0, 0, 20);
        lblTagline.Size = new Size(220, 80);
        lblTagline.TabIndex = 1;
        lblTagline.Text = "Clovent Business\nOperating System";
        // 
        // pnlForm
        // 
        pnlForm.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        pnlForm.Controls.Add(tlpForm);
        pnlForm.Dock = DockStyle.Fill;
        pnlForm.Location = new Point(260, 0);
        pnlForm.Margin = new Padding(4, 4, 4, 4);
        pnlForm.Name = "pnlForm";
        pnlForm.Size = new Size(540, 580);
        pnlForm.TabIndex = 1;
        // 
        // tlpForm
        // 
        tlpForm.ColumnCount = 1;
        tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpForm.Controls.Add(lblTitle, 0, 1);
        tlpForm.Controls.Add(lblUsername, 0, 2);
        tlpForm.Controls.Add(txtUsername, 0, 3);
        tlpForm.Controls.Add(lblPassword, 0, 4);
        tlpForm.Controls.Add(txtPassword, 0, 5);
        tlpForm.Controls.Add(lblPin, 0, 6);
        tlpForm.Controls.Add(txtPin, 0, 7);
        tlpForm.Controls.Add(chkRememberMe, 0, 8);
        tlpForm.Controls.Add(lblLanguage, 0, 9);
        tlpForm.Controls.Add(cmbLanguage, 0, 10);
        tlpForm.Controls.Add(lblTheme, 0, 11);
        tlpForm.Controls.Add(cmbTheme, 0, 12);
        tlpForm.Controls.Add(lblError, 0, 13);
        tlpForm.Controls.Add(prgLoading, 0, 14);
        tlpForm.Controls.Add(lblSelectModule, 0, 15);
        tlpForm.Controls.Add(tlpModules, 0, 16);
        tlpForm.Dock = DockStyle.Fill;
        tlpForm.Location = new Point(0, 0);
        tlpForm.Margin = new Padding(4, 4, 4, 4);
        tlpForm.Name = "tlpForm";
        tlpForm.Padding = new Padding(40, 0, 40, 0);
        tlpForm.RowCount = 18;
        tlpForm.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle());
        tlpForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        tlpForm.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpForm.Size = new Size(540, 580);
        tlpForm.TabIndex = 0;
        // 
        // lblTitle
        // 
        lblTitle.Appearance.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.Appearance.Options.UseFont = true;
        lblTitle.Dock = DockStyle.Fill;
        lblTitle.Location = new Point(40, 10);
        lblTitle.Margin = new Padding(0, 8, 0, 6);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(460, 25);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Welcome back";
        // 
        // lblUsername
        // 
        lblUsername.Dock = DockStyle.Fill;
        lblUsername.Location = new Point(40, 42);
        lblUsername.Margin = new Padding(0, 4, 0, 2);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(460, 15);
        lblUsername.TabIndex = 1;
        lblUsername.Text = "&Username";
        // 
        // txtUsername
        // 
        txtUsername.Dock = DockStyle.Fill;
        txtUsername.Location = new Point(40, 60);
        txtUsername.Margin = new Padding(0, 2, 0, 4);
        txtUsername.Name = "txtUsername";
        txtUsername.Font = new Font("Segoe UI", 9.5F);
        txtUsername.Properties.AccessibleName = "Username";
        txtUsername.Size = new Size(460, 26);
        txtUsername.TabIndex = 2;
        // 
        // lblPassword
        // 
        lblPassword.Dock = DockStyle.Fill;
        lblPassword.Location = new Point(40, 92);
        lblPassword.Margin = new Padding(0, 4, 0, 2);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(460, 15);
        lblPassword.TabIndex = 3;
        lblPassword.Text = "&Password";
        // 
        // txtPassword
        // 
        txtPassword.Dock = DockStyle.Fill;
        txtPassword.Location = new Point(40, 110);
        txtPassword.Margin = new Padding(0, 2, 0, 4);
        txtPassword.Name = "txtPassword";
        txtPassword.Font = new Font("Segoe UI", 9.5F);
        txtPassword.Properties.AccessibleName = "Password";
        txtPassword.Size = new Size(460, 26);
        txtPassword.TabIndex = 4;
        // 
        // lblPin
        // 
        lblPin.Dock = DockStyle.Fill;
        lblPin.Location = new Point(40, 142);
        lblPin.Margin = new Padding(0, 4, 0, 2);
        lblPin.Name = "lblPin";
        lblPin.Size = new Size(460, 15);
        lblPin.TabIndex = 5;
        lblPin.Text = "P&IN";
        // 
        // txtPin
        // 
        txtPin.Dock = DockStyle.Fill;
        txtPin.Location = new Point(40, 160);
        txtPin.Margin = new Padding(0, 2, 0, 4);
        txtPin.Name = "txtPin";
        txtPin.Font = new Font("Segoe UI", 9.5F);
        txtPin.Properties.AccessibleName = "PIN";
        txtPin.Properties.MaxLength = 8;
        txtPin.Size = new Size(460, 26);
        txtPin.TabIndex = 6;
        // 
        // chkRememberMe
        // 
        chkRememberMe.Dock = DockStyle.Fill;
        chkRememberMe.Location = new Point(40, 192);
        chkRememberMe.Margin = new Padding(0, 2, 0, 4);
        chkRememberMe.Name = "chkRememberMe";
        chkRememberMe.Properties.Caption = "Remember me";
        chkRememberMe.Size = new Size(460, 24);
        chkRememberMe.TabIndex = 7;
        // 
        // lblLanguage
        // 
        lblLanguage.Dock = DockStyle.Fill;
        lblLanguage.Location = new Point(40, 222);
        lblLanguage.Margin = new Padding(0, 4, 0, 2);
        lblLanguage.Name = "lblLanguage";
        lblLanguage.Size = new Size(460, 15);
        lblLanguage.TabIndex = 8;
        lblLanguage.Text = "&Language";
        // 
        // cmbLanguage
        // 
        cmbLanguage.Dock = DockStyle.Fill;
        cmbLanguage.Location = new Point(40, 240);
        cmbLanguage.Margin = new Padding(0, 2, 0, 4);
        cmbLanguage.Name = "cmbLanguage";
        cmbLanguage.Font = new Font("Segoe UI", 9.5F);
        cmbLanguage.Properties.AccessibleName = "Language";
        cmbLanguage.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        cmbLanguage.Size = new Size(460, 26);
        cmbLanguage.TabIndex = 9;
        // 
        // lblTheme
        // 
        lblTheme.Dock = DockStyle.Fill;
        lblTheme.Location = new Point(40, 272);
        lblTheme.Margin = new Padding(0, 4, 0, 2);
        lblTheme.Name = "lblTheme";
        lblTheme.Size = new Size(460, 15);
        lblTheme.TabIndex = 10;
        lblTheme.Text = "&Theme";
        // 
        // cmbTheme
        // 
        cmbTheme.Dock = DockStyle.Fill;
        cmbTheme.Location = new Point(40, 290);
        cmbTheme.Margin = new Padding(0, 2, 0, 4);
        cmbTheme.Name = "cmbTheme";
        cmbTheme.Font = new Font("Segoe UI", 9.5F);
        cmbTheme.Properties.AccessibleName = "Theme";
        cmbTheme.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        cmbTheme.Size = new Size(460, 26);
        cmbTheme.TabIndex = 11;
        cmbTheme.SelectedIndexChanged += CmbTheme_SelectedIndexChanged;
        // 
        // lblError
        // 
        lblError.Appearance.ForeColor = Color.Firebrick;
        lblError.Appearance.Options.UseForeColor = true;
        lblError.Appearance.Options.UseTextOptions = true;
        lblError.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        lblError.Dock = DockStyle.Fill;
        lblError.Location = new Point(40, 322);
        lblError.Margin = new Padding(0, 2, 0, 2);
        lblError.Name = "lblError";
        lblError.Size = new Size(460, 10);
        lblError.TabIndex = 12;
        lblError.Visible = false;
        // 
        // prgLoading
        // 
        prgLoading.Dock = DockStyle.Fill;
        prgLoading.Location = new Point(40, 336);
        prgLoading.Margin = new Padding(0, 2, 0, 2);
        prgLoading.Name = "prgLoading";
        prgLoading.Size = new Size(460, 12);
        prgLoading.TabIndex = 13;
        prgLoading.Visible = false;
        // 
        // lblSelectModule
        // 
        lblSelectModule.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSelectModule.Appearance.ForeColor = Color.Gray;
        lblSelectModule.Appearance.Options.UseFont = true;
        lblSelectModule.Appearance.Options.UseForeColor = true;
        lblSelectModule.Dock = DockStyle.Fill;
        lblSelectModule.Location = new Point(40, 352);
        lblSelectModule.Margin = new Padding(0, 8, 0, 4);
        lblSelectModule.Name = "lblSelectModule";
        lblSelectModule.Padding = new Padding(0, 2, 0, 2);
        lblSelectModule.Size = new Size(460, 15);
        lblSelectModule.TabIndex = 14;
        lblSelectModule.Text = "SELECT MODULE";
        // 
        // tlpModules
        // 
        tlpModules.ColumnCount = 2;
        tlpModules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpModules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpModules.Controls.Add(cardPos, 0, 0);
        tlpModules.Controls.Add(cardBackOffice, 1, 0);
        tlpModules.Dock = DockStyle.Fill;
        tlpModules.Location = new Point(40, 375);
        tlpModules.Margin = new Padding(0, 2, 0, 0);
        tlpModules.Name = "tlpModules";
        tlpModules.RowCount = 1;
        tlpModules.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpModules.Size = new Size(460, 96);
        tlpModules.TabIndex = 15;
        // 
        // cardPos
        // 
        cardPos.Appearance.BackColor = PosAccentColor;
        cardPos.Appearance.Options.UseBackColor = true;
        cardPos.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        cardPos.Controls.Add(tlpCardPos);
        cardPos.Cursor = Cursors.Hand;
        cardPos.Dock = DockStyle.Fill;
        cardPos.Location = new Point(0, 0);
        cardPos.Margin = new Padding(0, 0, 10, 0);
        cardPos.Name = "cardPos";
        cardPos.Size = new Size(220, 96);
        cardPos.TabIndex = 0;
        cardPos.TabStop = true;
        cardPos.KeyDown += Card_KeyDown;
        cardPos.Click += CardPos_Click;
        cardPos.Enter += Card_Enter;
        cardPos.Leave += Card_Leave;
        cardPos.MouseDown += Card_MouseDown;
        cardPos.MouseEnter += Card_MouseEnter;
        cardPos.MouseLeave += Card_MouseLeave;
        cardPos.MouseUp += Card_MouseUp;
        cardPos.Resize += Card_Resize;
        // 
        // tlpCardPos
        // 
        tlpCardPos.BackColor = Color.Transparent;
        tlpCardPos.ColumnCount = 1;
        tlpCardPos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCardPos.Controls.Add(lblPosIcon, 0, 0);
        tlpCardPos.Controls.Add(lblPosTitle, 0, 1);
        tlpCardPos.Controls.Add(lblPosSubtitle, 0, 2);
        tlpCardPos.Dock = DockStyle.Fill;
        tlpCardPos.Location = new Point(0, 0);
        tlpCardPos.Name = "tlpCardPos";
        tlpCardPos.RowCount = 3;
        tlpCardPos.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        tlpCardPos.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        tlpCardPos.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        tlpCardPos.Size = new Size(220, 96);
        tlpCardPos.TabIndex = 0;
        tlpCardPos.Tag = cardPos;
        tlpCardPos.Click += CardPos_Click;
        tlpCardPos.MouseDown += Card_MouseDown;
        tlpCardPos.MouseEnter += Card_MouseEnter;
        tlpCardPos.MouseLeave += Card_MouseLeave;
        tlpCardPos.MouseUp += Card_MouseUp;
        // 
        // lblPosSubtitle
        // 
        lblPosSubtitle.Appearance.Font = new Font("Segoe UI", 7.5F);
        lblPosSubtitle.Appearance.ForeColor = Color.FromArgb(225, 235, 255);
        lblPosSubtitle.Appearance.Options.UseFont = true;
        lblPosSubtitle.Appearance.Options.UseForeColor = true;
        lblPosSubtitle.Appearance.Options.UseTextOptions = true;
        lblPosSubtitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblPosSubtitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblPosSubtitle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        lblPosSubtitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
        lblPosSubtitle.Cursor = Cursors.Hand;
        lblPosSubtitle.Dock = DockStyle.Fill;
        lblPosSubtitle.Location = new Point(3, 60);
        lblPosSubtitle.Margin = new Padding(3);
        lblPosSubtitle.Name = "lblPosSubtitle";
        lblPosSubtitle.Size = new Size(214, 33);
        lblPosSubtitle.TabIndex = 2;
        lblPosSubtitle.Tag = cardPos;
        lblPosSubtitle.Text = "Restaurant Point of Sale";
        lblPosSubtitle.Click += CardPos_Click;
        lblPosSubtitle.MouseDown += Card_MouseDown;
        lblPosSubtitle.MouseEnter += Card_MouseEnter;
        lblPosSubtitle.MouseLeave += Card_MouseLeave;
        lblPosSubtitle.MouseUp += Card_MouseUp;
        // 
        // lblPosTitle
        // 
        lblPosTitle.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblPosTitle.Appearance.ForeColor = Color.White;
        lblPosTitle.Appearance.Options.UseFont = true;
        lblPosTitle.Appearance.Options.UseForeColor = true;
        lblPosTitle.Appearance.Options.UseTextOptions = true;
        lblPosTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblPosTitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblPosTitle.Cursor = Cursors.Hand;
        lblPosTitle.Dock = DockStyle.Fill;
        lblPosTitle.Location = new Point(3, 36);
        lblPosTitle.Margin = new Padding(3);
        lblPosTitle.Name = "lblPosTitle";
        lblPosTitle.Size = new Size(214, 18);
        lblPosTitle.TabIndex = 1;
        lblPosTitle.Tag = cardPos;
        lblPosTitle.Text = "POS";
        lblPosTitle.Click += CardPos_Click;
        lblPosTitle.MouseDown += Card_MouseDown;
        lblPosTitle.MouseEnter += Card_MouseEnter;
        lblPosTitle.MouseLeave += Card_MouseLeave;
        lblPosTitle.MouseUp += Card_MouseUp;
        // 
        // lblPosIcon
        // 
        lblPosIcon.Appearance.Font = new Font("Segoe UI Emoji", 16F);
        lblPosIcon.Appearance.ForeColor = Color.White;
        lblPosIcon.Appearance.Options.UseFont = true;
        lblPosIcon.Appearance.Options.UseForeColor = true;
        lblPosIcon.Appearance.Options.UseTextOptions = true;
        lblPosIcon.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblPosIcon.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblPosIcon.Cursor = Cursors.Hand;
        lblPosIcon.Dock = DockStyle.Fill;
        lblPosIcon.Location = new Point(3, 3);
        lblPosIcon.Margin = new Padding(3);
        lblPosIcon.Name = "lblPosIcon";
        lblPosIcon.Size = new Size(214, 27);
        lblPosIcon.TabIndex = 0;
        lblPosIcon.Tag = cardPos;
        lblPosIcon.Text = "🍴";
        lblPosIcon.Click += CardPos_Click;
        lblPosIcon.MouseDown += Card_MouseDown;
        lblPosIcon.MouseEnter += Card_MouseEnter;
        lblPosIcon.MouseLeave += Card_MouseLeave;
        lblPosIcon.MouseUp += Card_MouseUp;
        // 
        // cardBackOffice
        // 
        cardBackOffice.Appearance.BackColor = BackOfficeAccentColor;
        cardBackOffice.Appearance.Options.UseBackColor = true;
        cardBackOffice.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        cardBackOffice.Controls.Add(tlpCardBackOffice);
        cardBackOffice.Cursor = Cursors.Hand;
        cardBackOffice.Dock = DockStyle.Fill;
        cardBackOffice.Location = new Point(230, 0);
        cardBackOffice.Margin = new Padding(10, 0, 0, 0);
        cardBackOffice.Name = "cardBackOffice";
        cardBackOffice.Size = new Size(230, 96);
        cardBackOffice.TabIndex = 1;
        cardBackOffice.TabStop = true;
        cardBackOffice.KeyDown += Card_KeyDown;
        cardBackOffice.Click += CardBackOffice_Click;
        cardBackOffice.Enter += Card_Enter;
        cardBackOffice.Leave += Card_Leave;
        cardBackOffice.MouseDown += Card_MouseDown;
        cardBackOffice.MouseEnter += Card_MouseEnter;
        cardBackOffice.MouseLeave += Card_MouseLeave;
        cardBackOffice.MouseUp += Card_MouseUp;
        cardBackOffice.Resize += Card_Resize;
        // 
        // tlpCardBackOffice
        // 
        tlpCardBackOffice.BackColor = Color.Transparent;
        tlpCardBackOffice.ColumnCount = 1;
        tlpCardBackOffice.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCardBackOffice.Controls.Add(lblBackOfficeIcon, 0, 0);
        tlpCardBackOffice.Controls.Add(lblBackOfficeTitle, 0, 1);
        tlpCardBackOffice.Controls.Add(lblBackOfficeSubtitle, 0, 2);
        tlpCardBackOffice.Dock = DockStyle.Fill;
        tlpCardBackOffice.Location = new Point(0, 0);
        tlpCardBackOffice.Name = "tlpCardBackOffice";
        tlpCardBackOffice.RowCount = 3;
        tlpCardBackOffice.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        tlpCardBackOffice.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        tlpCardBackOffice.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        tlpCardBackOffice.Size = new Size(230, 96);
        tlpCardBackOffice.TabIndex = 0;
        tlpCardBackOffice.Tag = cardBackOffice;
        tlpCardBackOffice.Click += CardBackOffice_Click;
        tlpCardBackOffice.MouseDown += Card_MouseDown;
        tlpCardBackOffice.MouseEnter += Card_MouseEnter;
        tlpCardBackOffice.MouseLeave += Card_MouseLeave;
        tlpCardBackOffice.MouseUp += Card_MouseUp;
        // 
        // lblBackOfficeSubtitle
        // 
        lblBackOfficeSubtitle.Appearance.Font = new Font("Segoe UI", 7.5F);
        lblBackOfficeSubtitle.Appearance.ForeColor = Color.FromArgb(225, 235, 255);
        lblBackOfficeSubtitle.Appearance.Options.UseFont = true;
        lblBackOfficeSubtitle.Appearance.Options.UseForeColor = true;
        lblBackOfficeSubtitle.Appearance.Options.UseTextOptions = true;
        lblBackOfficeSubtitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblBackOfficeSubtitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblBackOfficeSubtitle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        lblBackOfficeSubtitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
        lblBackOfficeSubtitle.Cursor = Cursors.Hand;
        lblBackOfficeSubtitle.Dock = DockStyle.Fill;
        lblBackOfficeSubtitle.Location = new Point(3, 60);
        lblBackOfficeSubtitle.Margin = new Padding(3);
        lblBackOfficeSubtitle.Name = "lblBackOfficeSubtitle";
        lblBackOfficeSubtitle.Size = new Size(224, 33);
        lblBackOfficeSubtitle.TabIndex = 2;
        lblBackOfficeSubtitle.Tag = cardBackOffice;
        lblBackOfficeSubtitle.Text = "Administration, Inventory, Reports";
        lblBackOfficeSubtitle.Click += CardBackOffice_Click;
        lblBackOfficeSubtitle.MouseDown += Card_MouseDown;
        lblBackOfficeSubtitle.MouseEnter += Card_MouseEnter;
        lblBackOfficeSubtitle.MouseLeave += Card_MouseLeave;
        lblBackOfficeSubtitle.MouseUp += Card_MouseUp;
        // 
        // lblBackOfficeTitle
        // 
        lblBackOfficeTitle.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblBackOfficeTitle.Appearance.ForeColor = Color.White;
        lblBackOfficeTitle.Appearance.Options.UseFont = true;
        lblBackOfficeTitle.Appearance.Options.UseForeColor = true;
        lblBackOfficeTitle.Appearance.Options.UseTextOptions = true;
        lblBackOfficeTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblBackOfficeTitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblBackOfficeTitle.Cursor = Cursors.Hand;
        lblBackOfficeTitle.Dock = DockStyle.Fill;
        lblBackOfficeTitle.Location = new Point(3, 36);
        lblBackOfficeTitle.Margin = new Padding(3);
        lblBackOfficeTitle.Name = "lblBackOfficeTitle";
        lblBackOfficeTitle.Size = new Size(224, 18);
        lblBackOfficeTitle.TabIndex = 1;
        lblBackOfficeTitle.Tag = cardBackOffice;
        lblBackOfficeTitle.Text = "BACK OFFICE";
        lblBackOfficeTitle.Click += CardBackOffice_Click;
        lblBackOfficeTitle.MouseDown += Card_MouseDown;
        lblBackOfficeTitle.MouseEnter += Card_MouseEnter;
        lblBackOfficeTitle.MouseLeave += Card_MouseLeave;
        lblBackOfficeTitle.MouseUp += Card_MouseUp;
        // 
        // lblBackOfficeIcon
        // 
        lblBackOfficeIcon.Appearance.Font = new Font("Segoe UI Emoji", 16F);
        lblBackOfficeIcon.Appearance.ForeColor = Color.White;
        lblBackOfficeIcon.Appearance.Options.UseFont = true;
        lblBackOfficeIcon.Appearance.Options.UseForeColor = true;
        lblBackOfficeIcon.Appearance.Options.UseTextOptions = true;
        lblBackOfficeIcon.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblBackOfficeIcon.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblBackOfficeIcon.Cursor = Cursors.Hand;
        lblBackOfficeIcon.Dock = DockStyle.Fill;
        lblBackOfficeIcon.Location = new Point(3, 3);
        lblBackOfficeIcon.Margin = new Padding(3);
        lblBackOfficeIcon.Name = "lblBackOfficeIcon";
        lblBackOfficeIcon.Size = new Size(224, 27);
        lblBackOfficeIcon.TabIndex = 0;
        lblBackOfficeIcon.Tag = cardBackOffice;
        lblBackOfficeIcon.Text = "🏢";
        lblBackOfficeIcon.Click += CardBackOffice_Click;
        lblBackOfficeIcon.MouseDown += Card_MouseDown;
        lblBackOfficeIcon.MouseEnter += Card_MouseEnter;
        lblBackOfficeIcon.MouseLeave += Card_MouseLeave;
        lblBackOfficeIcon.MouseUp += Card_MouseUp;
        // 
        // btnCancelHidden
        // 
        btnCancelHidden.DialogResult = DialogResult.Cancel;
        btnCancelHidden.Location = new Point(0, 0);
        btnCancelHidden.Margin = new Padding(8, 8, 8, 8);
        btnCancelHidden.Name = "btnCancelHidden";
        btnCancelHidden.Size = new Size(188, 58);
        btnCancelHidden.TabIndex = 16;
        btnCancelHidden.TabStop = false;
        btnCancelHidden.Visible = false;
        btnCancelHidden.Click += BtnCancelHidden_Click;
        // 
        // LoginForm
        // 
        CancelButton = btnCancelHidden;
        ClientSize = new Size(800, 580);
        Controls.Add(pnlForm);
        Controls.Add(pnlBranding);
        Controls.Add(btnCancelHidden);
        KeyPreview = true;
        MinimumSize = new Size(800, 580);
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Sign in - Clovent Business Operating System";
        Load += LoginForm_Load;
        // Enable DPI scaling
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        ((System.ComponentModel.ISupportInitialize)pnlBranding).EndInit();
        pnlBranding.ResumeLayout(false);
        tlpBranding.ResumeLayout(false);
        tlpBranding.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlForm).EndInit();
        pnlForm.ResumeLayout(false);
        tlpForm.ResumeLayout(false);
        tlpForm.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)txtUsername.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)txtPassword.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)txtPin.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)chkRememberMe.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)cmbLanguage.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)cmbTheme.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)prgLoading.Properties).EndInit();
        tlpModules.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)cardPos).EndInit();
        cardPos.ResumeLayout(false);
        cardPos.PerformLayout();
        tlpCardPos.ResumeLayout(false);
        tlpCardPos.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)cardBackOffice).EndInit();
        cardBackOffice.ResumeLayout(false);
        cardBackOffice.PerformLayout();
        tlpCardBackOffice.ResumeLayout(false);
        tlpCardBackOffice.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.PanelControl pnlBranding;
    private TableLayoutPanel tlpBranding;
    private DevExpress.XtraEditors.LabelControl lblLogo;
    private DevExpress.XtraEditors.LabelControl lblTagline;
    private DevExpress.XtraEditors.PanelControl pnlForm;
    private TableLayoutPanel tlpForm;
    private DevExpress.XtraEditors.LabelControl lblTitle;
    private DevExpress.XtraEditors.LabelControl lblUsername;
    private DevExpress.XtraEditors.TextEdit txtUsername;
    private DevExpress.XtraEditors.LabelControl lblPassword;
    private DevExpress.XtraEditors.TextEdit txtPassword;
    private DevExpress.XtraEditors.LabelControl lblPin;
    private DevExpress.XtraEditors.TextEdit txtPin;
    private DevExpress.XtraEditors.CheckEdit chkRememberMe;
    private DevExpress.XtraEditors.LabelControl lblLanguage;
    private DevExpress.XtraEditors.ComboBoxEdit cmbLanguage;
    private DevExpress.XtraEditors.LabelControl lblTheme;
    private DevExpress.XtraEditors.ComboBoxEdit cmbTheme;
    private DevExpress.XtraEditors.LabelControl lblError;
    private DevExpress.XtraEditors.ProgressBarControl prgLoading;
    private DevExpress.XtraEditors.LabelControl lblSelectModule;
    private TableLayoutPanel tlpModules;
    private DevExpress.XtraEditors.PanelControl cardPos;
    private TableLayoutPanel tlpCardPos;
    private DevExpress.XtraEditors.LabelControl lblPosIcon;
    private DevExpress.XtraEditors.LabelControl lblPosTitle;
    private DevExpress.XtraEditors.LabelControl lblPosSubtitle;
    private DevExpress.XtraEditors.PanelControl cardBackOffice;
    private TableLayoutPanel tlpCardBackOffice;
    private DevExpress.XtraEditors.LabelControl lblBackOfficeIcon;
    private DevExpress.XtraEditors.LabelControl lblBackOfficeTitle;
    private DevExpress.XtraEditors.LabelControl lblBackOfficeSubtitle;
    private DevExpress.XtraEditors.SimpleButton btnCancelHidden;
}
