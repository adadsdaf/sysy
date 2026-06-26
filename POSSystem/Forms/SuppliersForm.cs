using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class SuppliersForm : Form
    {
        private readonly User _currentUser;
        private TabControl _tabs = null!;

        public SuppliersForm(User user)
        {
            _currentUser = user;
            this.Text = "إدارة الموردين والمشتريات";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Arial", 11) };
            _tabs.TabPages.Add(BuildSuppliersTab());
            _tabs.TabPages.Add(BuildOrdersTab());
            _tabs.TabPages.Add(BuildStatementTab());
            this.Controls.Add(_tabs);
        }

        // ── SUPPLIERS TAB ─────────────────────────────────────────────────
        private DataGridView _supGrid = null!;
        private TabPage BuildSuppliersTab()
        {
            var page = new TabPage("🏭  الموردون");
            _supGrid = MakeGrid();
            _supGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الاسم", DataPropertyName = "Name", Width = 180 },
                new DataGridViewTextBoxColumn { HeaderText = "الجوال", DataPropertyName = "Phone", Width = 120 },
                new DataGridViewTextBoxColumn { HeaderText = "مسؤول التواصل", DataPropertyName = "ContactPerson", Width = 140 },
                new DataGridViewTextBoxColumn { HeaderText = "الرقم الضريبي", DataPropertyName = "TaxNumber", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "الرصيد المستحق", DataPropertyName = "Balance", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "تاريخ التسجيل", DataPropertyName = "CreatedAt", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } }
            );
            var toolbar = MakeToolbar(
                ("+ مورد جديد", Color.FromArgb(34, 139, 34), (s, e) => AddSupplier()),
                ("تعديل", Color.FromArgb(30, 100, 200), (s, e) => EditSupplier()),
                ("حذف", Color.FromArgb(200, 50, 50), (s, e) => DeleteSupplier()),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadSuppliers())
            );
            LoadSuppliers();
            page.Controls.AddRange(new Control[] { toolbar, _supGrid });
            toolbar.Dock = DockStyle.Top; _supGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadSuppliers() => _supGrid.DataSource = SupplierService.GetAll();

        private void AddSupplier()
        {
            using var dlg = new SupplierEditDialog(new Supplier());
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { SupplierService.Create(dlg.Result); LoadSuppliers(); }
        }

        private void EditSupplier()
        {
            if (_supGrid.CurrentRow?.DataBoundItem is not Supplier sup) return;
            using var dlg = new SupplierEditDialog(sup);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { SupplierService.Update(dlg.Result); LoadSuppliers(); }
        }

        private void DeleteSupplier()
        {
            if (_supGrid.CurrentRow?.DataBoundItem is not Supplier sup) return;
            if (MessageBox.Show($"حذف المورد '{sup.Name}'؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { SupplierService.Delete(sup.Id); LoadSuppliers(); }
            catch { MessageBox.Show("لا يمكن حذف هذا المورد لوجود طلبات مرتبطة به.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── PURCHASE ORDERS TAB ───────────────────────────────────────────
        private DataGridView _ordGrid = null!;
        private ComboBox _ordSupFilter = null!;
        private TabPage BuildOrdersTab()
        {
            var page = new TabPage("🛒  طلبات الشراء");
            _ordGrid = MakeGrid();
            _ordGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "رقم الطلب", DataPropertyName = "OrderNumber", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "المورد", DataPropertyName = "SupplierName", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "OrderDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } },
                new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", DataPropertyName = "Total", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "المدفوع", DataPropertyName = "AmountPaid", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "المتبقي", DataPropertyName = "Balance", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الحالة", DataPropertyName = "Status", Width = 90 },
                new DataGridViewTextBoxColumn { HeaderText = "تاريخ الاستلام", DataPropertyName = "ReceiveDate", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } }
            );

            var filterPanel = new Panel { Height = 46, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8) };
            _ordSupFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Right, Width = 200, Font = new Font("Arial", 9) };
            _ordSupFilter.Items.Add(new Supplier { Id = 0, Name = "(جميع الموردين)" });
            foreach (var s in SupplierService.GetAll()) _ordSupFilter.Items.Add(s);
            _ordSupFilter.DisplayMember = "Name"; _ordSupFilter.SelectedIndex = 0;
            _ordSupFilter.SelectedIndexChanged += (s, e) => LoadOrders();
            filterPanel.Controls.AddRange(new Control[] { _ordSupFilter, new Label { Text = "المورد:", Dock = DockStyle.Right, Width = 55, Font = new Font("Arial", 9), TextAlign = ContentAlignment.MiddleCenter } });

            var toolbar = MakeToolbar(
                ("+ طلب شراء جديد", Color.FromArgb(34, 139, 34), (s, e) => AddOrder()),
                ("استلام الطلب", Color.FromArgb(30, 100, 200), (s, e) => ReceiveOrder()),
                ("تسجيل دفع", Color.FromArgb(160, 100, 0), (s, e) => PayOrder()),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadOrders())
            );
            LoadOrders();
            page.Controls.AddRange(new Control[] { toolbar, filterPanel, _ordGrid });
            toolbar.Dock = DockStyle.Top; filterPanel.Dock = DockStyle.Top; _ordGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadOrders()
        {
            int? supId = null;
            if (_ordSupFilter?.SelectedItem is Supplier s && s.Id > 0) supId = s.Id;
            _ordGrid.DataSource = SupplierService.GetOrders(supId);
        }

        private void AddOrder()
        {
            var suppliers = SupplierService.GetAll();
            if (!suppliers.Any()) { MessageBox.Show("أضف مورداً أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using var dlg = new PurchaseOrderDialog(suppliers);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { SupplierService.CreateOrder(dlg.Result); LoadOrders(); LoadSuppliers(); }
        }

        private void ReceiveOrder()
        {
            if (_ordGrid.CurrentRow?.DataBoundItem is not PurchaseOrder ord) return;
            if (ord.Status == "received") { MessageBox.Show("تم استلام هذا الطلب مسبقاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show($"تأكيد استلام طلب {ord.OrderNumber}؟ سيتم إضافة الكميات للمخزون.", "تأكيد الاستلام", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            SupplierService.ReceiveOrder(ord.Id);
            LoadOrders();
            MessageBox.Show("تم استلام الطلب وتحديث المخزون بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PayOrder()
        {
            if (_ordGrid.CurrentRow?.DataBoundItem is not PurchaseOrder ord) return;
            using var dlg = new PayDialog($"تسجيل دفع لـ {ord.OrderNumber}", ord.Balance);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Amount > 0) { SupplierService.PayOrderPartial(ord.Id, dlg.Amount); LoadOrders(); LoadSuppliers(); }
        }

        // ── SUPPLIER STATEMENT TAB ────────────────────────────────────────
        private DataGridView _stGrid = null!;
        private ComboBox _stSupCombo = null!;
        private DateTimePicker _stFrom = null!, _stTo = null!;
        private Label _stTotalLbl = null!;
        private TabPage BuildStatementTab()
        {
            var page = new TabPage("📋  كشف حساب المورد");
            _stGrid = MakeGrid();
            _stGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "Date", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } },
                new DataGridViewTextBoxColumn { HeaderText = "البيان", DataPropertyName = "Description", Width = 250 },
                new DataGridViewTextBoxColumn { HeaderText = "مديونية", DataPropertyName = "Debit", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "دائنية", DataPropertyName = "Credit", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الرصيد", DataPropertyName = "Balance", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "مرجع", DataPropertyName = "Reference", Width = 120 }
            );

            var ctrlPanel = new Panel { Height = 56, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8) };
            _stSupCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Right, Width = 200, Font = new Font("Arial", 9) };
            foreach (var s in SupplierService.GetAll()) _stSupCombo.Items.Add(s);
            _stSupCombo.DisplayMember = "Name"; if (_stSupCombo.Items.Count > 0) _stSupCombo.SelectedIndex = 0;
            _stFrom = new DateTimePicker { Value = DateTime.Today.AddDays(-90), Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            _stTo = new DateTimePicker { Value = DateTime.Today, Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            var btnShow = new Button { Text = "عرض", Dock = DockStyle.Right, Width = 80, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnShow.FlatAppearance.BorderSize = 0; btnShow.Click += (s, e) => LoadStatement();
            _stTotalLbl = new Label { Text = "", Dock = DockStyle.Left, Width = 250, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.FromArgb(30, 100, 200), TextAlign = ContentAlignment.MiddleLeft };
            ctrlPanel.Controls.AddRange(new Control[] { _stTotalLbl, btnShow, _stTo, new Label { Text = "إلى:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, _stFrom, new Label { Text = "من:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, _stSupCombo });

            var printBtn = new Button { Text = "🖨 طباعة", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(80, 80, 160), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            printBtn.FlatAppearance.BorderSize = 0; printBtn.Click += (s, e) => PrintStatement();
            page.Controls.AddRange(new Control[] { ctrlPanel, _stGrid, printBtn });
            ctrlPanel.Dock = DockStyle.Top; _stGrid.Dock = DockStyle.Fill; printBtn.Dock = DockStyle.Bottom;
            return page;
        }

        private void LoadStatement()
        {
            if (_stSupCombo.SelectedItem is not Supplier sup) return;
            var rows = SupplierService.GetStatement(sup.Id, _stFrom.Value, _stTo.Value);
            _stGrid.DataSource = rows;
            var balance = rows.LastOrDefault()?.Balance ?? 0;
            _stTotalLbl.Text = $"الرصيد: {balance:N0} ر.س";
        }

        private void PrintStatement()
        {
            if (_stSupCombo.SelectedItem is not Supplier sup) return;
            var rows = SupplierService.GetStatement(sup.Id, _stFrom.Value, _stTo.Value);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════");
            sb.AppendLine($"            كشف حساب المورد: {sup.Name}");
            sb.AppendLine($"      من {_stFrom.Value:dd/MM/yyyy}  إلى  {_stTo.Value:dd/MM/yyyy}");
            sb.AppendLine("═══════════════════════════════════════════════════");
            sb.AppendLine($"{"التاريخ",-12} {"البيان",-28} {"مديونية",10} {"دائنية",10} {"الرصيد",12}");
            sb.AppendLine(new string('─', 75));
            foreach (var r in rows)
                sb.AppendLine($"{r.Date:dd/MM/yyyy,-12} {r.Description,-28} {r.Debit,10:N0} {r.Credit,10:N0} {r.Balance,12:N0}");
            sb.AppendLine(new string('═', 75));
            var dlg = new Form { Text = "كشف حساب المورد", Size = new Size(700, 550), StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes };
            var rtb = new RichTextBox { Dock = DockStyle.Fill, Text = sb.ToString(), Font = new Font("Courier New", 9), ReadOnly = true };
            dlg.Controls.Add(rtb); dlg.ShowDialog();
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
                var btn = new Button { Text = text, Size = new Size(text.Length > 6 ? 150 : 110, 36), BackColor = color, ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 0, 0, 0) };
                btn.FlatAppearance.BorderSize = 0; btn.Click += click; flow.Controls.Add(btn);
            }
            panel.Controls.Add(flow); return panel;
        }
    }

    // ── SUPPLIER EDIT DIALOG ──────────────────────────────────────────────
    public class SupplierEditDialog : Form
    {
        public Supplier? Result { get; private set; }
        private readonly Supplier _orig;
        public SupplierEditDialog(Supplier sup)
        {
            _orig = sup;
            this.Text = sup.Id == 0 ? "إضافة مورد" : "تعديل بيانات المورد";
            this.Size = new Size(440, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            int y = 15;
            TextBox Txt(string label, string val) {
                this.Controls.Add(new Label { Text = label, Location = new Point(16, y), Size = new Size(400, 22), Font = new Font("Arial", 10) }); y += 24;
                var tb = new TextBox { Text = val, Location = new Point(16, y), Size = new Size(400, 28), Font = new Font("Arial", 10) };
                this.Controls.Add(tb); y += 36; return tb;
            }
            var nameBox = Txt("الاسم: *", sup.Name);
            var phoneBox = Txt("الجوال:", sup.Phone ?? "");
            var emailBox = Txt("البريد الإلكتروني:", sup.Email ?? "");
            var addressBox = Txt("العنوان:", sup.Address ?? "");
            var taxBox = Txt("الرقم الضريبي:", sup.TaxNumber ?? "");
            var contactBox = Txt("مسؤول التواصل:", sup.ContactPerson ?? "");
            var notesBox = Txt("ملاحظات:", sup.Notes ?? "");

            var btnOk = new Button { Text = "حفظ", Location = new Point(16, y), Size = new Size(130, 38), BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => {
                if (string.IsNullOrEmpty(nameBox.Text.Trim())) { MessageBox.Show("أدخل اسم المورد", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Result = new Supplier { Id = _orig.Id, Name = nameBox.Text.Trim(), Phone = phoneBox.Text.Trim().NullIfEmpty(), Email = emailBox.Text.Trim().NullIfEmpty(), Address = addressBox.Text.Trim().NullIfEmpty(), TaxNumber = taxBox.Text.Trim().NullIfEmpty(), ContactPerson = contactBox.Text.Trim().NullIfEmpty(), Notes = notesBox.Text.Trim().NullIfEmpty(), Balance = _orig.Balance };
                DialogResult = DialogResult.OK; Close();
            };
            var btnCancel = new Button { Text = "إلغاء", Location = new Point(160, y), Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }

    // ── PURCHASE ORDER DIALOG ─────────────────────────────────────────────
    public class PurchaseOrderDialog : Form
    {
        public PurchaseOrder? Result { get; private set; }
        private readonly List<Supplier> _suppliers;
        private DataGridView _itemsGrid = null!;
        private List<PurchaseOrderItem> _items = new();
        private ComboBox _supCombo = null!;
        private DateTimePicker _datePicker = null!;
        private NumericUpDown _paidBox = null!;
        private Label _totalLbl = null!;

        public PurchaseOrderDialog(List<Supplier> suppliers)
        {
            _suppliers = suppliers;
            this.Text = "طلب شراء جديد";
            this.Size = new Size(780, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            var header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(12) };
            _supCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 12), Size = new Size(260, 30), Font = new Font("Arial", 10) };
            _supCombo.Items.AddRange(suppliers.ToArray()); _supCombo.DisplayMember = "Name"; _supCombo.SelectedIndex = 0;
            header.Controls.Add(new Label { Text = "المورد:", Location = new Point(12, -2), Size = new Size(80, 22), Font = new Font("Arial", 10) });
            header.Controls.Add(_supCombo);

            _datePicker = new DateTimePicker { Value = DateTime.Today, Location = new Point(300, 12), Size = new Size(160, 30), Font = new Font("Arial", 10) };
            header.Controls.Add(new Label { Text = "تاريخ الطلب:", Location = new Point(300, -2), Size = new Size(110, 22), Font = new Font("Arial", 10) });
            header.Controls.Add(_datePicker);

            header.Controls.Add(new Label { Text = "المبلغ المدفوع:", Location = new Point(12, 54), Size = new Size(110, 22), Font = new Font("Arial", 10) });
            _paidBox = new NumericUpDown { Location = new Point(12, 72), Size = new Size(160, 30), Font = new Font("Arial", 10), Maximum = 9999999, ThousandsSeparator = true };
            _totalLbl = new Label { Text = "الإجمالي: 0", Location = new Point(200, 65), Size = new Size(220, 28), Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.FromArgb(30, 100, 200) };
            header.Controls.AddRange(new Control[] { _paidBox, _totalLbl });

            _itemsGrid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, Font = new Font("Arial", 10), RowHeadersVisible = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            _itemsGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الصنف", Name = "ItemName", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "الوحدة", Name = "Unit", Width = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "الكمية", Name = "Quantity", Width = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "سعر الوحدة", Name = "UnitPrice", Width = 100 },
                new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", Name = "Total", Width = 100, ReadOnly = true }
            );
            _itemsGrid.CellEndEdit += (s, e) => RecalcTotal();

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(12, 8, 12, 8) };
            var btnAddRow = new Button { Text = "+ سطر", Dock = DockStyle.Right, Width = 90, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddRow.FlatAppearance.BorderSize = 0; btnAddRow.Click += (s, e) => AddItemRow();
            var btnDelRow = new Button { Text = "حذف سطر", Dock = DockStyle.Right, Width = 100, BackColor = Color.FromArgb(180, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDelRow.FlatAppearance.BorderSize = 0; btnDelRow.Click += (s, e) => { if (_itemsGrid.CurrentRow != null) { _itemsGrid.Rows.Remove(_itemsGrid.CurrentRow); RecalcTotal(); } };
            var btnSave = new Button { Text = "✔ حفظ الطلب", Dock = DockStyle.Left, Width = 140, BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSave.FlatAppearance.BorderSize = 0; btnSave.Click += SaveOrder;
            var btnCancel = new Button { Text = "إلغاء", Dock = DockStyle.Left, Width = 90, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnPanel.Controls.AddRange(new Control[] { btnAddRow, btnDelRow, btnCancel, btnSave });

            AddItemRow(); AddItemRow();
            this.Controls.AddRange(new Control[] { header, _itemsGrid, btnPanel });
            this.CancelButton = btnCancel;
        }

        private void AddItemRow()
        {
            var i = _itemsGrid.Rows.Add();
            _itemsGrid.Rows[i].SetValues("", "قطعة", "1", "0", "0");
        }

        private void RecalcTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in _itemsGrid.Rows)
            {
                if (!decimal.TryParse(row.Cells["Quantity"].Value?.ToString(), out decimal qty)) qty = 0;
                if (!decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal price)) price = 0;
                var rowTotal = qty * price;
                row.Cells["Total"].Value = rowTotal.ToString("N0");
                total += rowTotal;
            }
            _totalLbl.Text = $"الإجمالي: {total:N0}";
        }

        private void SaveOrder(object? s, EventArgs e)
        {
            if (_supCombo.SelectedItem is not Supplier sup) { MessageBox.Show("اختر مورداً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var items = new List<PurchaseOrderItem>();
            decimal total = 0;
            foreach (DataGridViewRow row in _itemsGrid.Rows)
            {
                var name = row.Cells["ItemName"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(name)) continue;
                decimal.TryParse(row.Cells["Quantity"].Value?.ToString(), out decimal qty);
                decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal price);
                items.Add(new PurchaseOrderItem { ItemId = 0, ItemName = name, Unit = row.Cells["Unit"].Value?.ToString(), Quantity = qty, UnitPrice = price });
                total += qty * price;
            }
            if (!items.Any()) { MessageBox.Show("أضف أصنافاً للطلب", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            Result = new PurchaseOrder { SupplierId = sup.Id, OrderDate = _datePicker.Value.Date, Status = "pending", Total = total, AmountPaid = _paidBox.Value, Items = items };
            DialogResult = DialogResult.OK; Close();
        }
    }

    // ── PAY DIALOG ────────────────────────────────────────────────────────
    public class PayDialog : Form
    {
        public decimal Amount { get; private set; }
        public PayDialog(string title, decimal maxAmount)
        {
            this.Text = title;
            this.Size = new Size(360, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;
            this.Controls.Add(new Label { Text = $"المبلغ المتبقي: {maxAmount:N0}", Location = new Point(16, 18), Size = new Size(320, 24), Font = new Font("Arial", 11) });
            this.Controls.Add(new Label { Text = "المبلغ المدفوع:", Location = new Point(16, 54), Size = new Size(160, 24), Font = new Font("Arial", 10) });
            var amtBox = new NumericUpDown { Location = new Point(16, 76), Size = new Size(200, 30), Font = new Font("Arial", 11), Maximum = maxAmount, ThousandsSeparator = true };
            amtBox.Value = maxAmount; this.Controls.Add(amtBox);
            var btnOk = new Button { Text = "تأكيد", Location = new Point(16, 118), Size = new Size(120, 38), BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => { Amount = amtBox.Value; DialogResult = DialogResult.OK; Close(); };
            var btnCancel = new Button { Text = "إلغاء", Location = new Point(150, 118), Size = new Size(90, 38), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }
}
