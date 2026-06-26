using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class FinanceForm : Form
    {
        private readonly User _currentUser;
        private TabControl _tabs = null!;

        public FinanceForm(User user)
        {
            _currentUser = user;
            this.Text = "الإدارة المالية — سندات الصرف والقبض";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Arial", 11) };
            _tabs.TabPages.Add(BuildDashboardTab());
            _tabs.TabPages.Add(BuildPaymentVouchersTab());
            _tabs.TabPages.Add(BuildReceiptVouchersTab());
            _tabs.TabPages.Add(BuildCashBoxesTab());
            _tabs.TabPages.Add(BuildStatementTab());
            this.Controls.Add(_tabs);
        }

        // ── DASHBOARD TAB ──────────────────────────────────────────────────
        private TabPage BuildDashboardTab()
        {
            var page = new TabPage("📊  لوحة التحكم");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24) };

            var refreshBtn = new Button { Text = "تحديث", Location = new Point(24, 24), Size = new Size(140, 40),
                BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            refreshBtn.FlatAppearance.BorderSize = 0;
            var cardsPanel = new Panel { Location = new Point(24, 80), Size = new Size(1000, 130), BackColor = Color.Transparent };
            refreshBtn.Click += (s, e) => RefreshFinanceDashboard(cardsPanel);

            var cashBoxPanel = new Panel { Location = new Point(24, 230), Size = new Size(1000, 350), BackColor = Color.Transparent };
            var cbLbl = new Label { Text = "أرصدة الصناديق:", Location = new Point(24, 210), Size = new Size(300, 24), Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 140) };
            refreshBtn.Click += (s2, e2) => RefreshCashBoxCards(cashBoxPanel);
            panel.Controls.AddRange(new Control[] { refreshBtn, cardsPanel, cbLbl, cashBoxPanel });
            RefreshFinanceDashboard(cardsPanel);
            RefreshCashBoxCards(cashBoxPanel);
            page.Controls.Add(panel);
            return page;
        }

        private void RefreshFinanceDashboard(Panel cardsPanel)
        {
            cardsPanel.Controls.Clear();
            var (totalIn, totalOut, net) = FinanceService.GetTodaySummary();
            var cards = new[]
            {
                ("📥 إجمالي القبض اليوم", $"{totalIn:N0}", Color.FromArgb(34, 139, 34)),
                ("📤 إجمالي الصرف اليوم", $"{totalOut:N0}", Color.FromArgb(200, 60, 60)),
                ("⚖ الصافي اليوم", $"{net:N0}", net >= 0 ? Color.FromArgb(30, 100, 200) : Color.FromArgb(160, 40, 40))
            };
            int x = 0;
            foreach (var (title, val, color) in cards)
            {
                var card = new Panel { Location = new Point(x, 0), Size = new Size(240, 120), BackColor = color };
                card.Controls.Add(new Label { Text = title, ForeColor = Color.White, Font = new Font("Arial", 10), Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter });
                card.Controls.Add(new Label { Text = val, ForeColor = Color.White, Font = new Font("Arial", 22, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
                cardsPanel.Controls.Add(card); x += 255;
            }
        }

        private void RefreshCashBoxCards(Panel cashBoxPanel)
        {
            cashBoxPanel.Controls.Clear();
            var boxes = FinanceService.GetCashBoxes();
            int x = 0, y2 = 0, col = 0;
            foreach (var cb in boxes)
            {
                var card = new Panel { Location = new Point(x, y2), Size = new Size(230, 100), BackColor = Color.FromArgb(40, 65, 130) };
                if (cb.IsDefault) card.BackColor = Color.FromArgb(0, 110, 60);
                card.Controls.Add(new Label { Text = cb.IsDefault ? $"⭐ {cb.Name}" : cb.Name, ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), Dock = DockStyle.Top, Height = 35, TextAlign = ContentAlignment.MiddleCenter });
                card.Controls.Add(new Label { Text = $"{cb.Balance:N0} ر.س", ForeColor = Color.White, Font = new Font("Arial", 18, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
                cashBoxPanel.Controls.Add(card);
                col++; x += 240;
                if (col >= 4) { col = 0; x = 0; y2 += 110; }
            }
        }

        // ── PAYMENT VOUCHERS (صرف) ─────────────────────────────────────────
        private DataGridView _payGrid = null!;
        private DateTimePicker _payFrom = null!, _payTo = null!;
        private TabPage BuildPaymentVouchersTab()
        {
            var page = new TabPage("📤  سندات الصرف");
            _payGrid = MakeGrid();
            _payGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "رقم السند", DataPropertyName = "VoucherNumber", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "VoucherDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } },
                new DataGridViewTextBoxColumn { HeaderText = "الجهة المستفيدة", DataPropertyName = "PayeeName", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "البيان", DataPropertyName = "Description", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "المبلغ", DataPropertyName = "Amount", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الصندوق", DataPropertyName = "CashBoxName", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "المرجع", DataPropertyName = "ReferenceNumber", Width = 110 }
            );

            var filterPanel = BuildDateFilterPanel(ref _payFrom, ref _payTo, () => LoadPaymentVouchers());
            var toolbar = MakeToolbar(
                ("+ سند صرف جديد", Color.FromArgb(200, 60, 60), (s, e) => AddVoucher("payment")),
                ("حذف السند", Color.FromArgb(140, 30, 30), (s, e) => DeleteVoucher(_payGrid)),
                ("طباعة", Color.FromArgb(80, 80, 160), (s, e) => PrintVoucher(_payGrid)),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadPaymentVouchers())
            );
            LoadPaymentVouchers();
            page.Controls.AddRange(new Control[] { toolbar, filterPanel, _payGrid });
            toolbar.Dock = DockStyle.Top; filterPanel.Dock = DockStyle.Top; _payGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadPaymentVouchers() => _payGrid.DataSource = FinanceService.GetVouchers("payment", _payFrom.Value, _payTo.Value);

        // ── RECEIPT VOUCHERS (قبض) ─────────────────────────────────────────
        private DataGridView _recGrid = null!;
        private DateTimePicker _recFrom = null!, _recTo = null!;
        private TabPage BuildReceiptVouchersTab()
        {
            var page = new TabPage("📥  سندات القبض");
            _recGrid = MakeGrid();
            _recGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "رقم السند", DataPropertyName = "VoucherNumber", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "VoucherDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } },
                new DataGridViewTextBoxColumn { HeaderText = "المستلم منه", DataPropertyName = "PayeeName", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "البيان", DataPropertyName = "Description", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "المبلغ", DataPropertyName = "Amount", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الصندوق", DataPropertyName = "CashBoxName", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "المرجع", DataPropertyName = "ReferenceNumber", Width = 110 }
            );
            var filterPanel = BuildDateFilterPanel(ref _recFrom, ref _recTo, () => LoadReceiptVouchers());
            var toolbar = MakeToolbar(
                ("+ سند قبض جديد", Color.FromArgb(34, 139, 34), (s, e) => AddVoucher("receipt")),
                ("حذف السند", Color.FromArgb(200, 50, 50), (s, e) => DeleteVoucher(_recGrid)),
                ("طباعة", Color.FromArgb(80, 80, 160), (s, e) => PrintVoucher(_recGrid)),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadReceiptVouchers())
            );
            LoadReceiptVouchers();
            page.Controls.AddRange(new Control[] { toolbar, filterPanel, _recGrid });
            toolbar.Dock = DockStyle.Top; filterPanel.Dock = DockStyle.Top; _recGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadReceiptVouchers() => _recGrid.DataSource = FinanceService.GetVouchers("receipt", _recFrom.Value, _recTo.Value);

        private void AddVoucher(string type)
        {
            var cashBoxes = FinanceService.GetCashBoxes();
            if (!cashBoxes.Any()) { MessageBox.Show("لا يوجد صناديق نقدية", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using var dlg = new VoucherDialog(type, cashBoxes);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null)
            {
                FinanceService.CreateVoucher(dlg.Result, _currentUser.Id);
                LoadPaymentVouchers(); LoadReceiptVouchers();
                RefreshCashBoxCards_Refresh();
                MessageBox.Show($"تم إصدار سند {(type == "payment" ? "الصرف" : "القبض")} بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RefreshCashBoxCards_Refresh() { /* Will refresh on next tab switch */ }

        private void DeleteVoucher(DataGridView grid)
        {
            if (grid.CurrentRow?.DataBoundItem is not CashVoucher v) return;
            if (MessageBox.Show($"حذف السند {v.VoucherNumber}؟ سيتم عكس تأثيره على الصندوق.", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            FinanceService.DeleteVoucher(v.Id);
            LoadPaymentVouchers(); LoadReceiptVouchers();
        }

        private void PrintVoucher(DataGridView grid)
        {
            if (grid.CurrentRow?.DataBoundItem is not CashVoucher v) return;
            var typeName = v.VoucherType == "payment" ? "سند صرف" : "سند قبض";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"           {typeName}");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"رقم السند:    {v.VoucherNumber}");
            sb.AppendLine($"التاريخ:     {v.VoucherDate:dd/MM/yyyy}");
            sb.AppendLine($"الجهة:       {v.PayeeName}");
            sb.AppendLine($"المبلغ:      {v.Amount:N0} ر.س");
            sb.AppendLine($"البيان:      {v.Description}");
            sb.AppendLine($"الصندوق:     {v.CashBoxName}");
            if (!string.IsNullOrEmpty(v.ReferenceNumber)) sb.AppendLine($"المرجع:      {v.ReferenceNumber}");
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"المبلغ فقط: {AmountToWords(v.Amount)}");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"توقيع المستلم:              توقيع المصرف:");
            sb.AppendLine();
            sb.AppendLine("_______________             _______________");
            var dlg = new Form { Text = typeName, Size = new Size(500, 430), StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes };
            var rtb = new RichTextBox { Dock = DockStyle.Fill, Text = sb.ToString(), Font = new Font("Courier New", 10), ReadOnly = true };
            var btnPrint = new Button { Text = "🖨 طباعة", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) => {
                var pd = new System.Drawing.Printing.PrintDocument { DocumentName = typeName };
                pd.PrintPage += (ps, pe) => pe.Graphics!.DrawString(sb.ToString(), new Font("Courier New", 9), Brushes.Black, new System.Drawing.PointF(10, 10));
                var prd = new PrintDialog { Document = pd };
                if (prd.ShowDialog() == DialogResult.OK) pd.Print();
            };
            dlg.Controls.AddRange(new Control[] { rtb, btnPrint }); dlg.ShowDialog();
        }

        private static string AmountToWords(decimal amount) => $"{amount:N0} ريال سعودي فقط لا غير";

        // ── CASH BOXES TAB ─────────────────────────────────────────────────
        private DataGridView _cbGrid = null!;
        private TabPage BuildCashBoxesTab()
        {
            var page = new TabPage("🏦  الصناديق");
            _cbGrid = MakeGrid();
            _cbGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "اسم الصندوق", DataPropertyName = "Name", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "الوصف", DataPropertyName = "Description", Width = 250 },
                new DataGridViewTextBoxColumn { HeaderText = "الرصيد الحالي", DataPropertyName = "Balance", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewCheckBoxColumn { HeaderText = "افتراضي", DataPropertyName = "IsDefault", Width = 80 }
            );
            var toolbar = MakeToolbar(
                ("+ صندوق جديد", Color.FromArgb(34, 139, 34), (s, e) => AddCashBox()),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadCashBoxes())
            );
            LoadCashBoxes();
            page.Controls.AddRange(new Control[] { toolbar, _cbGrid });
            toolbar.Dock = DockStyle.Top; _cbGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadCashBoxes() => _cbGrid.DataSource = FinanceService.GetCashBoxes();

        private void AddCashBox()
        {
            using var dlg = new CashBoxDialog();
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { FinanceService.CreateCashBox(dlg.Result); LoadCashBoxes(); }
        }

        // ── CASHBOX STATEMENT TAB ──────────────────────────────────────────
        private DataGridView _cbStGrid = null!;
        private ComboBox _cbStCombo = null!;
        private DateTimePicker _cbStFrom = null!, _cbStTo = null!;
        private Label _cbStTotalLbl = null!;
        private TabPage BuildStatementTab()
        {
            var page = new TabPage("📋  كشف الصندوق");
            _cbStGrid = MakeGrid();
            _cbStGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "Date", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } },
                new DataGridViewTextBoxColumn { HeaderText = "البيان", DataPropertyName = "Description", Width = 250 },
                new DataGridViewTextBoxColumn { HeaderText = "صرف", DataPropertyName = "Debit", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "قبض", DataPropertyName = "Credit", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الرصيد", DataPropertyName = "Balance", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "مرجع", DataPropertyName = "Reference", Width = 130 }
            );

            var ctrlPanel = new Panel { Height = 56, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8) };
            _cbStCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Right, Width = 200, Font = new Font("Arial", 9) };
            foreach (var cb in FinanceService.GetCashBoxes()) _cbStCombo.Items.Add(cb);
            _cbStCombo.DisplayMember = "Name"; if (_cbStCombo.Items.Count > 0) _cbStCombo.SelectedIndex = 0;
            _cbStFrom = new DateTimePicker { Value = DateTime.Today.AddDays(-30), Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            _cbStTo = new DateTimePicker { Value = DateTime.Today, Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            var btnShow = new Button { Text = "عرض", Dock = DockStyle.Right, Width = 80, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnShow.FlatAppearance.BorderSize = 0; btnShow.Click += (s, e) => LoadCashBoxStatement();
            _cbStTotalLbl = new Label { Text = "", Dock = DockStyle.Left, Width = 260, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.FromArgb(30, 100, 200), TextAlign = ContentAlignment.MiddleLeft };
            ctrlPanel.Controls.AddRange(new Control[] { _cbStTotalLbl, btnShow, _cbStTo, new Label { Text = "إلى:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, _cbStFrom, new Label { Text = "من:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, _cbStCombo });
            page.Controls.AddRange(new Control[] { ctrlPanel, _cbStGrid });
            ctrlPanel.Dock = DockStyle.Top; _cbStGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadCashBoxStatement()
        {
            if (_cbStCombo.SelectedItem is not CashBox cb) return;
            var rows = FinanceService.GetCashBoxStatement(cb.Id, _cbStFrom.Value, _cbStTo.Value);
            _cbStGrid.DataSource = rows;
            var balance = rows.LastOrDefault()?.Balance ?? 0;
            _cbStTotalLbl.Text = $"الرصيد: {balance:N0} ر.س";
        }

        private Panel BuildDateFilterPanel(ref DateTimePicker from, ref DateTimePicker to, Action refresh)
        {
            var filterPanel = new Panel { Height = 46, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8) };
            from = new DateTimePicker { Value = DateTime.Today.AddDays(-30), Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            to = new DateTimePicker { Value = DateTime.Today, Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            var fromRef = from; var toRef = to;
            var btnSearch = new Button { Text = "بحث", Dock = DockStyle.Right, Width = 80, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSearch.FlatAppearance.BorderSize = 0; btnSearch.Click += (s, e) => refresh();
            filterPanel.Controls.AddRange(new Control[] { btnSearch, toRef, new Label { Text = "إلى:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, fromRef, new Label { Text = "من:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter } });
            return filterPanel;
        }

        private static DataGridView MakeGrid() => new DataGridView
        {
            AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            Font = new Font("Arial", 10), RowHeadersVisible = false,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 248, 255) },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(45, 75, 150), ForeColor = Color.White },
            EnableHeadersVisualStyles = false
        };

        private static Panel MakeToolbar(params (string text, Color color, EventHandler click)[] buttons)
        {
            var panel = new Panel { Height = 50, BackColor = Color.FromArgb(228, 233, 248), Padding = new Padding(8, 7, 8, 7) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            foreach (var (text, color, click) in buttons)
            {
                var btn = new Button { Text = text, Size = new Size(text.Length > 6 ? 155 : 110, 36), BackColor = color, ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 0, 0, 0) };
                btn.FlatAppearance.BorderSize = 0; btn.Click += click; flow.Controls.Add(btn);
            }
            panel.Controls.Add(flow); return panel;
        }
    }

    // ── VOUCHER DIALOG ─────────────────────────────────────────────────────
    public class VoucherDialog : Form
    {
        public CashVoucher? Result { get; private set; }
        private readonly string _type;
        private readonly List<CashBox> _cashBoxes;

        public VoucherDialog(string type, List<CashBox> cashBoxes)
        {
            _type = type; _cashBoxes = cashBoxes;
            this.Text = type == "payment" ? "سند صرف جديد" : "سند قبض جديد";
            this.Size = new Size(460, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            int y = 15;
            Control Lbl(string t) { var l = new Label { Text = t, Location = new Point(16, y), Size = new Size(420, 22), Font = new Font("Arial", 10) }; this.Controls.Add(l); y += 24; return l; }

            Lbl("الصندوق: *");
            var cbCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, y), Size = new Size(420, 28), Font = new Font("Arial", 10) };
            cbCombo.Items.AddRange(cashBoxes.ToArray()); cbCombo.DisplayMember = "Name";
            var def = cashBoxes.FirstOrDefault(c => c.IsDefault) ?? cashBoxes[0];
            cbCombo.SelectedItem = def; this.Controls.Add(cbCombo); y += 38;

            Lbl("التاريخ:");
            var dtPicker = new DateTimePicker { Value = DateTime.Today, Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(dtPicker); y += 38;

            Lbl(type == "payment" ? "اسم المستفيد: *" : "اسم الدافع: *");
            var payeeBox = new TextBox { Location = new Point(16, y), Size = new Size(420, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(payeeBox); y += 38;

            Lbl("البيان: *");
            var descBox = new TextBox { Location = new Point(16, y), Size = new Size(420, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(descBox); y += 38;

            Lbl("المبلغ: *");
            var amtBox = new NumericUpDown { Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 11), Maximum = 9999999, Minimum = 0.01m, DecimalPlaces = 2, ThousandsSeparator = true };
            this.Controls.Add(amtBox); y += 38;

            Lbl("رقم المرجع (اختياري):");
            var refBox = new TextBox { Location = new Point(16, y), Size = new Size(250, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(refBox); y += 46;

            var btnOk = new Button
            {
                Text = type == "payment" ? "✔ إصدار سند الصرف" : "✔ إصدار سند القبض",
                Location = new Point(16, y), Size = new Size(220, 42),
                BackColor = type == "payment" ? Color.FromArgb(200, 60, 60) : Color.FromArgb(34, 139, 34),
                ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(payeeBox.Text)) { MessageBox.Show("أدخل اسم الجهة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (string.IsNullOrWhiteSpace(descBox.Text)) { MessageBox.Show("أدخل البيان", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (amtBox.Value <= 0) { MessageBox.Show("أدخل مبلغاً صحيحاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (cbCombo.SelectedItem is not CashBox cb) { MessageBox.Show("اختر صندوقاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Result = new CashVoucher
                {
                    VoucherType = _type, CashBoxId = cb.Id, VoucherDate = dtPicker.Value.Date,
                    PayeeName = payeeBox.Text.Trim(), Description = descBox.Text.Trim(),
                    Amount = amtBox.Value, ReferenceNumber = refBox.Text.Trim().NullIfEmpty()
                };
                DialogResult = DialogResult.OK; Close();
            };
            var btnCancel = new Button { Text = "إلغاء", Location = new Point(250, y), Size = new Size(100, 42), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }

    // ── CASH BOX DIALOG ───────────────────────────────────────────────────
    public class CashBoxDialog : Form
    {
        public CashBox? Result { get; private set; }
        public CashBoxDialog()
        {
            this.Text = "صندوق جديد";
            this.Size = new Size(400, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            this.Controls.Add(new Label { Text = "اسم الصندوق: *", Location = new Point(16, 20), Size = new Size(360, 22), Font = new Font("Arial", 10) });
            var nameBox = new TextBox { Location = new Point(16, 44), Size = new Size(360, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(nameBox);
            this.Controls.Add(new Label { Text = "الوصف:", Location = new Point(16, 82), Size = new Size(360, 22), Font = new Font("Arial", 10) });
            var descBox = new TextBox { Location = new Point(16, 106), Size = new Size(360, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(descBox);
            this.Controls.Add(new Label { Text = "الرصيد الافتتاحي:", Location = new Point(16, 144), Size = new Size(200, 22), Font = new Font("Arial", 10) });
            var balBox = new NumericUpDown { Location = new Point(16, 166), Size = new Size(180, 28), Font = new Font("Arial", 10), Maximum = 9999999, ThousandsSeparator = true };
            this.Controls.Add(balBox);

            var btnOk = new Button { Text = "إضافة", Location = new Point(16, 206), Size = new Size(130, 38), BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("أدخل اسم الصندوق", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Result = new CashBox { Name = nameBox.Text.Trim(), Description = descBox.Text.Trim().NullIfEmpty(), Balance = balBox.Value };
                DialogResult = DialogResult.OK; Close();
            };
            var btnCancel = new Button { Text = "إلغاء", Location = new Point(160, 206), Size = new Size(90, 38), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }
}
