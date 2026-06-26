using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class CashierDashboardForm : Form
    {
        private readonly User _user;
        private readonly AppSettings _settings;
        public bool GoToPOS { get; private set; } = false;

        public CashierDashboardForm(User user)
        {
            _user = user;
            _settings = SettingsService.Get();
            this.Text = $"نقطة المبيعات — {_settings.StoreName}";
            this.Size = new Size(1060, 680);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(238, 241, 250);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            BuildTopBar();
            BuildSidebar();
            BuildMainArea();
        }

        // ── TOP BAR ──────────────────────────────────────────────────────────
        private void BuildTopBar()
        {
            var topBar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(22, 28, 52) };

            var storeLbl = new Label
            {
                Text = _settings.StoreName,
                Font = new Font("Arial", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var timeLbl = new Label
            {
                Font = new Font("Arial", 10),
                ForeColor = Color.FromArgb(180, 200, 255),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left,
                Width = 200
            };
            var timer = new System.Windows.Forms.Timer { Interval = 1000, Enabled = true };
            timer.Tick += (s, e) => timeLbl.Text = $"  {DateTime.Now:HH:mm:ss}  |  {DateTime.Now:dddd dd/MM/yyyy}";
            timer.Tick += null;
            timeLbl.Text = $"  {DateTime.Now:HH:mm:ss}  |  {DateTime.Now:dddd dd/MM/yyyy}";
            timer.Tick += (s, e2) => timeLbl.Text = $"  {DateTime.Now:HH:mm:ss}  |  {DateTime.Now:dddd dd/MM/yyyy}";

            var userLbl = new Label
            {
                Text = $"👤 {_user.Name}  ({(_user.IsAdmin ? "مدير" : "كاشير")})  ",
                Font = new Font("Arial", 10),
                ForeColor = Color.FromArgb(200, 230, 255),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                Width = 250
            };

            topBar.Controls.AddRange(new Control[] { storeLbl, timeLbl, userLbl });
            this.Controls.Add(topBar);
        }

        // ── SIDEBAR ───────────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 230,
                BackColor = Color.FromArgb(28, 35, 65),
                Padding = new Padding(0, 10, 0, 10)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10, 8, 10, 8),
                AutoSize = false,
                WrapContents = false
            };

            // ── Navigation items ──────────────────────────────────────────
            var navItems = BuildNavItems();
            foreach (var (icon, label, color, action, visible) in navItems)
            {
                if (!visible) continue;
                var btn = MakeSidebarButton(icon, label, color);
                btn.Click += (s, e) => action();
                flow.Controls.Add(btn);
            }

            // ── Separator ────────────────────────────────────────────────
            flow.Controls.Add(new Label { Size = new Size(210, 1), BackColor = Color.FromArgb(70, 80, 130), Margin = new Padding(0, 8, 0, 8) });

            // ── PIN change button ─────────────────────────────────────────
            var btnPin = MakeSidebarButton("🔑", "تغيير كلمة السر", Color.FromArgb(80, 80, 110));
            btnPin.Click += (s, e) => ChangePin();
            flow.Controls.Add(btnPin);

            // ── Logout ────────────────────────────────────────────────────
            var btnLogout = MakeSidebarButton("🚪", "تسجيل الخروج", Color.FromArgb(140, 40, 40));
            btnLogout.Click += (s, e) => { this.Close(); };
            flow.Controls.Add(btnLogout);

            sidebar.Controls.Add(flow);
            this.Controls.Add(sidebar);
        }

        private IEnumerable<(string icon, string label, Color color, Action action, bool visible)> BuildNavItems()
        {
            yield return ("🛒", "نقطة المبيعات", Color.FromArgb(34, 120, 34), OpenPOS, _user.Permissions.CanAccessPOS);
            yield return ("↩", "المرتجعات", Color.FromArgb(160, 50, 50), OpenReturns, _user.Permissions.CanAccessPOS);
            yield return ("📊", "التقارير", Color.FromArgb(30, 100, 200), OpenReports, _user.Permissions.CanViewReports || _user.IsAdmin);
            yield return ("👥", "الموارد البشرية", Color.FromArgb(80, 60, 160), OpenHR, _user.IsAdmin);
            yield return ("📦", "المخزون والمستودع", Color.FromArgb(0, 120, 130), OpenInventory, _user.IsAdmin || _user.Permissions.CanManageItems);
            yield return ("🏭", "الموردون والمشتريات", Color.FromArgb(130, 90, 0), OpenSuppliers, _user.IsAdmin);
            yield return ("💰", "الإدارة المالية", Color.FromArgb(34, 100, 34), OpenFinance, _user.IsAdmin);
            yield return ("⚙", "الإعدادات والإدارة", Color.FromArgb(60, 60, 100), OpenManagement, _user.Permissions.CanAccessManagement || _user.IsAdmin);
        }

        private static Button MakeSidebarButton(string icon, string label, Color color)
        {
            var btn = new Button
            {
                Text = $" {icon}  {label}",
                Size = new Size(210, 42),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 3, 0, 3),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.3f);
            return btn;
        }

        // ── MAIN AREA ─────────────────────────────────────────────────────────
        private void BuildMainArea()
        {
            var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28) };

            // Welcome header
            var welcomeLbl = new Label
            {
                Text = $"مرحباً، {_user.Name}",
                Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 40, 110),
                Dock = DockStyle.Top, Height = 52,
                TextAlign = ContentAlignment.MiddleRight
            };

            var subLbl = new Label
            {
                Text = $"اليوم: {DateTime.Now:dddd  dd MMMM yyyy}  —  {_settings.StoreName}",
                Font = new Font("Arial", 11),
                ForeColor = Color.FromArgb(100, 110, 150),
                Dock = DockStyle.Top, Height = 30,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Quick-stats row
            var statsPanel = BuildQuickStats();
            statsPanel.Dock = DockStyle.Top;
            statsPanel.Height = 110;

            // Quick-actions grid
            var actionsLbl = new Label
            {
                Text = "الوصول السريع:",
                Font = new Font("Arial", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 140),
                Dock = DockStyle.Top, Height = 36,
                TextAlign = ContentAlignment.MiddleRight
            };

            var actionsPanel = BuildQuickActions();
            actionsPanel.Dock = DockStyle.Fill;

            main.Controls.AddRange(new Control[] { actionsPanel, actionsLbl, statsPanel, subLbl, welcomeLbl });
            this.Controls.Add(main);
        }

        private Panel BuildQuickStats()
        {
            var panel = new Panel { Padding = new Padding(0, 10, 0, 10) };
            try
            {
                var todaySales = Database.DatabaseHelper.ExecuteScalar(@"
SELECT ISNULL(SUM(ii.Quantity*ii.Price),0)
FROM InvoiceItems ii JOIN Invoices inv ON ii.InvoiceId=inv.Id
WHERE inv.Status='paid' AND CAST(inv.PaidAt AS DATE)=CAST(GETDATE() AS DATE)");
                var todayCount = Database.DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Invoices WHERE Status='paid' AND CAST(PaidAt AS DATE)=CAST(GETDATE() AS DATE)");
                var totalItems = Database.DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Items WHERE IsAvailable=1");
                var totalEmployees = Database.DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Employees WHERE Status='active'");

                var cards = new[]
                {
                    ("💰 مبيعات اليوم", $"{Convert.ToDecimal(todaySales):N0} ر.س", Color.FromArgb(34, 120, 34)),
                    ("🧾 فواتير اليوم", Convert.ToString(todayCount) ?? "0", Color.FromArgb(30, 100, 200)),
                    ("🛒 الأصناف النشطة", Convert.ToString(totalItems) ?? "0", Color.FromArgb(100, 60, 170)),
                    ("👥 الموظفون", Convert.ToString(totalEmployees) ?? "0", Color.FromArgb(0, 130, 140))
                };

                int x = 0;
                foreach (var (title, val, color) in cards)
                {
                    var card = new Panel { Location = new Point(x, 0), Size = new Size(175, 90), BackColor = color, Cursor = Cursors.Default };
                    var roundLabel = new Label { Text = title, ForeColor = Color.White, Font = new Font("Arial", 9), Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter };
                    var valLabel = new Label { Text = val, ForeColor = Color.White, Font = new Font("Arial", 18, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                    card.Controls.AddRange(new Control[] { valLabel, roundLabel });
                    panel.Controls.Add(card);
                    x += 185;
                }
            }
            catch { /* DB not connected yet */ }
            return panel;
        }

        private Panel BuildQuickActions()
        {
            var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 10, 0, 0) };

            var actions = new (string icon, string label, Color color, Action action)[]
            {
                ("🛒", "نقطة المبيعات", Color.FromArgb(34, 120, 34), OpenPOS),
                ("↩", "المرتجعات", Color.FromArgb(160, 50, 50), OpenReturns),
                ("📊", "التقارير", Color.FromArgb(30, 100, 200), OpenReports),
                ("👥", "الموارد البشرية", Color.FromArgb(80, 60, 160), OpenHR),
                ("📦", "المخزون", Color.FromArgb(0, 120, 130), OpenInventory),
                ("🏭", "الموردون", Color.FromArgb(130, 90, 0), OpenSuppliers),
                ("💰", "الإدارة المالية", Color.FromArgb(34, 100, 34), OpenFinance),
                ("⚙", "الإعدادات", Color.FromArgb(60, 60, 100), OpenManagement),
            };

            foreach (var (icon, label, color, action) in actions)
            {
                var btn = new Button
                {
                    Text = $"{icon}\n{label}",
                    Size = new Size(130, 110),
                    BackColor = color,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 11, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(6, 6, 0, 0),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.3f);
                btn.Click += (s, e) => action();
                panel.Controls.Add(btn);
            }
            return panel;
        }

        // ── NAVIGATION ACTIONS ────────────────────────────────────────────────
        private void OpenPOS()
        {
            if (!_user.Permissions.CanAccessPOS && !_user.IsAdmin)
            { MessageBox.Show("ليس لديك صلاحية الوصول إلى نقطة المبيعات", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            GoToPOS = true;
            this.Close();
        }

        private void OpenReports()
        {
            if (!_user.Permissions.CanViewReports && !_user.IsAdmin)
            { MessageBox.Show("ليس لديك صلاحية عرض التقارير", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new ReportsForm(_user).Show();
        }

        private void OpenManagement()
        {
            if (!_user.Permissions.CanAccessManagement && !_user.IsAdmin)
            { MessageBox.Show("ليس لديك صلاحية الوصول إلى الإدارة", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new ManagementForm(_user).ShowDialog();
        }

        private void OpenHR()
        {
            if (!_user.IsAdmin)
            { MessageBox.Show("هذه الميزة للمدير فقط", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new HRForm(_user).Show();
        }

        private void OpenInventory()
        {
            if (!_user.IsAdmin && !_user.Permissions.CanManageItems)
            { MessageBox.Show("ليس لديك صلاحية الوصول إلى المخزون", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new InventoryForm(_user).Show();
        }

        private void OpenSuppliers()
        {
            if (!_user.IsAdmin)
            { MessageBox.Show("هذه الميزة للمدير فقط", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new SuppliersForm(_user).Show();
        }

        private void OpenFinance()
        {
            if (!_user.IsAdmin)
            { MessageBox.Show("هذه الميزة للمدير فقط", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new FinanceForm(_user).Show();
        }

        private void OpenReturns()
        {
            if (!_user.Permissions.CanAccessPOS && !_user.IsAdmin)
            { MessageBox.Show("ليس لديك صلاحية تسجيل المرتجعات", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new ReturnsForm(_user).Show();
        }

        // ── PIN CHANGE ────────────────────────────────────────────────────────
        private void ChangePin()
        {
            using var dlg = new ChangePinDialog(_user.Id, _user.Pin);
            dlg.ShowDialog();
        }

        // ── PIN DIALOG ────────────────────────────────────────────────────────
        private class ChangePinDialog : Form
        {
            private readonly int _userId;
            private readonly string _currentPin;

            public ChangePinDialog(int userId, string currentPin)
            {
                _userId = userId;
                _currentPin = currentPin;

                this.Text = "تغيير كلمة السر";
                this.Size = new Size(370, 270);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;
                this.MaximizeBox = false;
                this.BackColor = Color.FromArgb(245, 247, 252);
                this.RightToLeft = RightToLeft.Yes;
                this.RightToLeftLayout = true;

                int y = 20;
                void AddLbl(string t) { this.Controls.Add(new Label { Text = t, Location = new Point(20, y), Size = new Size(320, 22), Font = new Font("Arial", 10) }); y += 24; }
                TextBox AddTxt() { var tb = new TextBox { Location = new Point(20, y), Size = new Size(320, 28), Font = new Font("Arial", 11), UseSystemPasswordChar = true }; this.Controls.Add(tb); y += 38; return tb; }

                AddLbl("كلمة السر الحالية:");
                var oldBox = AddTxt();
                AddLbl("كلمة السر الجديدة:");
                var newBox = AddTxt();
                AddLbl("تأكيد كلمة السر الجديدة:");
                var confBox = AddTxt(); y += 4;

                var btnSave = new Button { Text = "تغيير", Location = new Point(20, y), Size = new Size(130, 38), BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
                btnSave.FlatAppearance.BorderSize = 0;
                btnSave.Click += (s, e) => {
                    if (oldBox.Text != _currentPin) { MessageBox.Show("كلمة السر الحالية غير صحيحة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    if (newBox.Text.Length < 4) { MessageBox.Show("يجب أن تكون كلمة السر 4 أرقام على الأقل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (newBox.Text != confBox.Text) { MessageBox.Show("كلمات السر غير متطابقة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    UserService.ChangePin(_userId, oldBox.Text, newBox.Text);
                    MessageBox.Show("تم تغيير كلمة السر بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                };
                var btnCancel = new Button { Text = "إلغاء", Location = new Point(165, y), Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
                this.Controls.AddRange(new Control[] { btnSave, btnCancel });
                this.AcceptButton = btnSave; this.CancelButton = btnCancel;
            }
        }
    }
}
