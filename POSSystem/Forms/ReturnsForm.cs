using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class ReturnsForm : Form
    {
        private readonly User _currentUser;
        private DataGridView _grid = null!;
        private DateTimePicker _fromDate = null!, _toDate = null!;
        private Label _totalLbl = null!;

        public ReturnsForm(User user)
        {
            _currentUser = user;
            this.Text = "إدارة المرتجعات";
            this.Size = new Size(1050, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            _grid = new DataGridView
            {
                AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                Font = new Font("Arial", 10), RowHeadersVisible = false, Dock = DockStyle.Fill,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 248, 255) },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(160, 40, 40), ForeColor = Color.White },
                EnableHeadersVisualStyles = false
            };
            _grid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "رقم المرتجع", DataPropertyName = "ReturnNumber", Width = 140 },
                new DataGridViewTextBoxColumn { HeaderText = "رقم الفاتورة الأصلية", DataPropertyName = "OriginalInvoiceNumber", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "العميل", DataPropertyName = "CustomerName", Width = 150 },
                new DataGridViewTextBoxColumn { HeaderText = "الكاشير", DataPropertyName = "CashierName", Width = 120 },
                new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", DataPropertyName = "Total", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "السبب", DataPropertyName = "Reason", Width = 180 },
                new DataGridViewTextBoxColumn { HeaderText = "طريقة الاسترداد", DataPropertyName = "RefundMethod", Width = 120 },
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "CreatedAt", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } }
            );

            // Filter Panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8) };
            _fromDate = new DateTimePicker { Value = DateTime.Today.AddDays(-30), Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            _toDate = new DateTimePicker { Value = DateTime.Today, Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            var searchBtn = new Button { Text = "بحث", Dock = DockStyle.Right, Width = 80, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            searchBtn.FlatAppearance.BorderSize = 0; searchBtn.Click += (s, e) => LoadReturns();
            _totalLbl = new Label { Text = "", Dock = DockStyle.Left, Width = 280, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.FromArgb(160, 40, 40), TextAlign = ContentAlignment.MiddleLeft };
            filterPanel.Controls.AddRange(new Control[] { _totalLbl, searchBtn, _toDate, new Label { Text = "إلى:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, _fromDate, new Label { Text = "من:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter } });

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(228, 233, 248), Padding = new Padding(8, 7, 8, 7) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var btnAdd = new Button { Text = "+ مرتجع جديد", Size = new Size(150, 36), BackColor = Color.FromArgb(180, 40, 40), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnAdd.FlatAppearance.BorderSize = 0; btnAdd.Click += (s, e) => AddReturn();
            var btnRefresh = new Button { Text = "تحديث", Size = new Size(100, 36), BackColor = Color.FromArgb(80, 100, 140), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnRefresh.FlatAppearance.BorderSize = 0; btnRefresh.Click += (s, e) => LoadReturns();
            var btnDetail = new Button { Text = "عرض التفاصيل", Size = new Size(140, 36), BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnDetail.FlatAppearance.BorderSize = 0; btnDetail.Click += (s, e) => ViewReturnDetails();
            flow.Controls.AddRange(new Control[] { btnAdd, btnDetail, btnRefresh });
            toolbar.Controls.Add(flow);

            LoadReturns();
            this.Controls.AddRange(new Control[] { _grid, filterPanel, toolbar });
        }

        private void LoadReturns()
        {
            var returns = ReturnService.GetAll(_fromDate.Value, _toDate.Value);
            _grid.DataSource = returns;
            _totalLbl.Text = $"إجمالي المرتجعات: {returns.Sum(r => r.Total):N0} ر.س  ({returns.Count} سند)";
        }

        private void AddReturn()
        {
            using var dlg = new ReturnDialog(ItemService.GetItems(), _currentUser);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null)
            {
                ReturnService.Create(dlg.Result, _currentUser.Id);
                LoadReturns();
                MessageBox.Show("تم تسجيل المرتجع وتحديث المخزون بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ViewReturnDetails()
        {
            if (_grid.CurrentRow?.DataBoundItem is not ReturnInvoice ret) return;
            var dt = Database.DatabaseHelper.ExecuteQuery(
                "SELECT * FROM ReturnInvoiceItems WHERE ReturnInvoiceId=@Id",
                new Dictionary<string, object?> { ["@Id"] = ret.Id });

            var dlg = new Form { Text = $"تفاصيل المرتجع {ret.ReturnNumber}", Size = new Size(600, 500), StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
            var grid2 = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true, AllowUserToAddRows = false, Font = new Font("Arial", 10) };
            grid2.DataSource = dt;
            var info = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };
            info.Controls.Add(new Label { Text = $"المرتجع: {ret.ReturnNumber}   |   الفاتورة الأصلية: {ret.OriginalInvoiceNumber ?? "—"}   |   الإجمالي: {ret.Total:N0} ر.س\nالسبب: {ret.Reason}   |   طريقة الاسترداد: {ret.RefundMethod}", Dock = DockStyle.Fill, Font = new Font("Arial", 10) });
            dlg.Controls.AddRange(new Control[] { grid2, info }); dlg.ShowDialog();
        }
    }

    // ── RETURN DIALOG ─────────────────────────────────────────────────────
    public class ReturnDialog : Form
    {
        public ReturnInvoice? Result { get; private set; }
        private readonly List<Item> _allItems;
        private readonly User _user;
        private DataGridView _itemsGrid = null!;
        private TextBox _invoiceNumBox = null!, _reasonBox = null!;
        private ComboBox _refundCombo = null!;
        private Label _totalLbl = null!;

        public ReturnDialog(List<Item> items, User user)
        {
            _allItems = items; _user = user;
            this.Text = "مرتجع جديد";
            this.Size = new Size(800, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            var header = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(12) };
            header.Controls.Add(new Label { Text = "رقم الفاتورة الأصلية (اختياري):", Location = new Point(12, 8), Size = new Size(280, 22), Font = new Font("Arial", 10) });
            _invoiceNumBox = new TextBox { PlaceholderText = "مثال: INV-20240101-0001", Location = new Point(12, 30), Size = new Size(280, 28), Font = new Font("Arial", 10) };
            header.Controls.Add(_invoiceNumBox);
            header.Controls.Add(new Label { Text = "سبب الإرجاع: *", Location = new Point(12, 64), Size = new Size(220, 22), Font = new Font("Arial", 10) });
            _reasonBox = new TextBox { Location = new Point(12, 84), Size = new Size(300, 28), Font = new Font("Arial", 10) };
            header.Controls.Add(_reasonBox);
            header.Controls.Add(new Label { Text = "طريقة الاسترداد:", Location = new Point(330, 64), Size = new Size(140, 22), Font = new Font("Arial", 10) });
            _refundCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(330, 84), Size = new Size(160, 28), Font = new Font("Arial", 10) };
            _refundCombo.Items.AddRange(new object[] { "cash - نقداً", "store_credit - رصيد" });
            _refundCombo.SelectedIndex = 0;
            header.Controls.Add(_refundCombo);

            _itemsGrid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, Font = new Font("Arial", 10), RowHeadersVisible = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            var itemNames = items.Select(i => $"({i.ItemNumber}) {i.Name}").ToArray();
            var itemNameCol = new DataGridViewComboBoxColumn { HeaderText = "الصنف", Name = "ItemName", Width = 240, DataSource = itemNames };
            _itemsGrid.Columns.AddRange(itemNameCol, new DataGridViewTextBoxColumn { HeaderText = "الكمية", Name = "Qty", Width = 80 }, new DataGridViewTextBoxColumn { HeaderText = "السعر", Name = "Price", Width = 90 }, new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", Name = "Total", Width = 100, ReadOnly = true });
            _itemsGrid.CellEndEdit += (s, e) => RecalcTotal();

            _totalLbl = new Label { Dock = DockStyle.Bottom, Height = 40, Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.FromArgb(160, 30, 30), TextAlign = ContentAlignment.MiddleRight, Text = "الإجمالي: 0.00 ر.س" };

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(12, 8, 12, 8) };
            var btnAdd = new Button { Text = "+ إضافة سطر", Dock = DockStyle.Right, Width = 130, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdd.FlatAppearance.BorderSize = 0; btnAdd.Click += (s, e) => { _itemsGrid.Rows.Add(itemNames.FirstOrDefault() ?? "", "1", "0", "0"); };
            var btnDel = new Button { Text = "حذف سطر", Dock = DockStyle.Right, Width = 110, BackColor = Color.FromArgb(180, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDel.FlatAppearance.BorderSize = 0; btnDel.Click += (s, e) => { if (_itemsGrid.CurrentRow != null) { _itemsGrid.Rows.Remove(_itemsGrid.CurrentRow); RecalcTotal(); } };
            var btnSave = new Button { Text = "✔ تسجيل المرتجع", Dock = DockStyle.Left, Width = 165, BackColor = Color.FromArgb(180, 40, 40), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSave.FlatAppearance.BorderSize = 0; btnSave.Click += SaveReturn;
            var btnCancel = new Button { Text = "إلغاء", Dock = DockStyle.Left, Width = 90, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnPanel.Controls.AddRange(new Control[] { btnAdd, btnDel, btnCancel, btnSave });

            _itemsGrid.Rows.Add(itemNames.FirstOrDefault() ?? "", "1", "0", "0");
            this.Controls.AddRange(new Control[] { header, _itemsGrid, _totalLbl, btnPanel });
            this.CancelButton = btnCancel;
        }

        private void RecalcTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in _itemsGrid.Rows)
            {
                decimal.TryParse(row.Cells["Qty"].Value?.ToString(), out decimal qty);
                decimal.TryParse(row.Cells["Price"].Value?.ToString(), out decimal price);
                var t = qty * price; row.Cells["Total"].Value = t.ToString("N0"); total += t;
            }
            _totalLbl.Text = $"إجمالي المرتجع: {total:N0} ر.س";
        }

        private void SaveReturn(object? s, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_reasonBox.Text)) { MessageBox.Show("أدخل سبب الإرجاع", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var returnItems = new List<ReturnInvoiceItem>();
            decimal total = 0;
            foreach (DataGridViewRow row in _itemsGrid.Rows)
            {
                var nameVal = row.Cells["ItemName"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(nameVal)) continue;
                decimal.TryParse(row.Cells["Qty"].Value?.ToString(), out decimal qty);
                decimal.TryParse(row.Cells["Price"].Value?.ToString(), out decimal price);
                if (qty <= 0) continue;
                // Try match item
                var matchedItem = _allItems.FirstOrDefault(i => nameVal.Contains(i.Name) || nameVal.Contains(i.ItemNumber ?? "???"));
                returnItems.Add(new ReturnInvoiceItem { ItemId = matchedItem?.Id ?? 0, ItemName = matchedItem?.Name ?? nameVal, Quantity = qty, UnitPrice = price });
                total += qty * price;
            }
            if (!returnItems.Any()) { MessageBox.Show("أضف أصنافاً للمرتجع", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var refundMethod = (_refundCombo.SelectedItem?.ToString() ?? "cash").Split(' ')[0];
            Result = new ReturnInvoice
            {
                OriginalInvoiceNumber = _invoiceNumBox.Text.Trim().NullIfEmpty(),
                Reason = _reasonBox.Text.Trim(), RefundMethod = refundMethod,
                Total = total, Items = returnItems
            };
            DialogResult = DialogResult.OK; Close();
        }
    }
}
