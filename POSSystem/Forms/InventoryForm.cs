using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class InventoryForm : Form
    {
        private readonly User _currentUser;
        private TabControl _tabs = null!;

        public InventoryForm(User user)
        {
            _currentUser = user;
            this.Text = "إدارة المخزون والمستودعات";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Arial", 11) };
            _tabs.TabPages.Add(BuildOverviewTab());
            _tabs.TabPages.Add(BuildMovementsTab());
            _tabs.TabPages.Add(BuildLowStockTab());
            _tabs.TabPages.Add(BuildAdjustTab());
            this.Controls.Add(_tabs);
        }

        // ── OVERVIEW TAB ─────────────────────────────────────────────────
        private TabPage BuildOverviewTab()
        {
            var page = new TabPage("📦  نظرة عامة");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };

            var refreshBtn = new Button { Text = "تحديث", Location = new Point(20, 20), Size = new Size(140, 38),
                BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            refreshBtn.FlatAppearance.BorderSize = 0;
            var cardsPanel = new Panel { Location = new Point(20, 70), Size = new Size(1020, 120), BackColor = Color.Transparent };
            refreshBtn.Click += (s, e) => RefreshOverview(cardsPanel);

            var stockGrid = MakeGrid();
            stockGrid.Location = new Point(20, 210);
            stockGrid.Size = new Size(1020, 400);
            stockGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الرقم", DataPropertyName = "ItemNumber", Width = 70 },
                new DataGridViewTextBoxColumn { HeaderText = "الصنف", DataPropertyName = "Name", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "القسم", DataPropertyName = "CategoryName", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "السعر", DataPropertyName = "Price", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "المخزون الحالي", DataPropertyName = "Stock", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الحد الأدنى", DataPropertyName = "MinStock", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الوحدة", DataPropertyName = "Unit", Width = 80 },
                new DataGridViewCheckBoxColumn { HeaderText = "متاح", DataPropertyName = "IsAvailable", Width = 60 }
            );
            stockGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var gridLbl = new Label { Text = "قائمة الأصناف والمخزون:", Location = new Point(20, 185), Size = new Size(300, 22), Font = new Font("Arial", 11, FontStyle.Bold) };
            panel.Controls.AddRange(new Control[] { refreshBtn, cardsPanel, gridLbl, stockGrid });
            RefreshOverview(cardsPanel);
            stockGrid.DataSource = ItemService.GetItems();
            page.Controls.Add(panel);
            return page;
        }

        private void RefreshOverview(Panel cardsPanel)
        {
            cardsPanel.Controls.Clear();
            var (totalValue, itemCount, lowCount) = StockService.GetInventorySummary();
            var cards = new[]
            {
                ("📦 إجمالي الأصناف", itemCount.ToString(), Color.FromArgb(30, 100, 200)),
                ("⚠ أصناف منخفضة", lowCount.ToString(), Color.FromArgb(200, 80, 0)),
                ("💰 قيمة المخزون", $"{totalValue:N0}", Color.FromArgb(34, 139, 34))
            };
            int x = 0;
            foreach (var (title, val, color) in cards)
            {
                var card = new Panel { Location = new Point(x, 0), Size = new Size(240, 110), BackColor = color };
                card.Controls.Add(new Label { Text = title, ForeColor = Color.White, Font = new Font("Arial", 10), Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter });
                card.Controls.Add(new Label { Text = val, ForeColor = Color.White, Font = new Font("Arial", 22, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
                cardsPanel.Controls.Add(card);
                x += 250;
            }
        }

        // ── MOVEMENTS TAB ─────────────────────────────────────────────────
        private DataGridView _movGrid = null!;
        private DateTimePicker _movFrom = null!, _movTo = null!;
        private ComboBox _movType = null!;
        private TabPage BuildMovementsTab()
        {
            var page = new TabPage("↕  حركات المخزون");
            _movGrid = MakeGrid();
            _movGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الصنف", DataPropertyName = "ItemName", Width = 180 },
                new DataGridViewTextBoxColumn { HeaderText = "النوع", DataPropertyName = "MovementType", Width = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "الكمية", DataPropertyName = "Quantity", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "تكلفة الوحدة", DataPropertyName = "UnitCost", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", DataPropertyName = "TotalCost", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "المرجع", DataPropertyName = "ReferenceType", Width = 90 },
                new DataGridViewTextBoxColumn { HeaderText = "ملاحظات", DataPropertyName = "Notes", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "CreatedAt", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } }
            );

            var filterPanel = new Panel { Height = 50, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8) };
            _movFrom = new DateTimePicker { Value = DateTime.Today.AddDays(-30), Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            _movTo = new DateTimePicker { Value = DateTime.Today, Dock = DockStyle.Right, Width = 150, Font = new Font("Arial", 9) };
            _movType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Right, Width = 120, Font = new Font("Arial", 9) };
            _movType.Items.AddRange(new object[] { "الكل", "in", "out", "adjustment", "return" });
            _movType.SelectedIndex = 0;
            var searchBtn = new Button { Text = "بحث", Dock = DockStyle.Right, Width = 80, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            searchBtn.FlatAppearance.BorderSize = 0; searchBtn.Click += (s, e) => LoadMovements();
            filterPanel.Controls.AddRange(new Control[] { searchBtn, _movTo, new Label { Text = "إلى:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, _movFrom, new Label { Text = "من:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, _movType });

            var toolbar = MakeToolbar(("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadMovements()));
            LoadMovements();
            page.Controls.AddRange(new Control[] { toolbar, filterPanel, _movGrid });
            toolbar.Dock = DockStyle.Top; filterPanel.Dock = DockStyle.Top; _movGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadMovements()
        {
            string? type = _movType.SelectedIndex > 0 ? _movType.SelectedItem?.ToString() : null;
            _movGrid.DataSource = StockService.GetMovements(type: type, from: _movFrom.Value, to: _movTo.Value);
        }

        // ── LOW STOCK TAB ─────────────────────────────────────────────────
        private DataGridView _lowGrid = null!;
        private TabPage BuildLowStockTab()
        {
            var page = new TabPage("⚠  أصناف منخفضة");
            _lowGrid = MakeGrid();
            _lowGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الرقم", DataPropertyName = "ItemNumber", Width = 70 },
                new DataGridViewTextBoxColumn { HeaderText = "الصنف", DataPropertyName = "Name", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "القسم", DataPropertyName = "CategoryName", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "المخزون الحالي", DataPropertyName = "Stock", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", ForeColor = Color.Red } },
                new DataGridViewTextBoxColumn { HeaderText = "الحد الأدنى", DataPropertyName = "MinStock", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } }
            );
            var toolbar = MakeToolbar(("تحديث", Color.FromArgb(200, 80, 0), (s, e) => LoadLowStock()));
            LoadLowStock();
            page.Controls.AddRange(new Control[] { toolbar, _lowGrid });
            toolbar.Dock = DockStyle.Top; _lowGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadLowStock() => _lowGrid.DataSource = StockService.GetLowStockItems();

        // ── ADJUSTMENT TAB ─────────────────────────────────────────────────
        private TabPage BuildAdjustTab()
        {
            var page = new TabPage("✏  تسوية المخزون");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var items = ItemService.GetItems();
            int y = 15;
            var lblTitle = new Label { Text = "إضافة حركة مخزون يدوية:", Location = new Point(0, y), Size = new Size(500, 26), Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 140) }; y += 36;

            var lblItem = new Label { Text = "الصنف:", Location = new Point(0, y), Size = new Size(120, 22), Font = new Font("Arial", 10) }; y += 24;
            var itemCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, y), Size = new Size(360, 28), Font = new Font("Arial", 10) };
            itemCombo.Items.AddRange(items.Select(i => $"({i.ItemNumber}) {i.Name}").ToArray()); y += 38;

            var lblType = new Label { Text = "نوع الحركة:", Location = new Point(0, y), Size = new Size(120, 22), Font = new Font("Arial", 10) }; y += 24;
            var typeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, y), Size = new Size(200, 28), Font = new Font("Arial", 10) };
            typeCombo.Items.AddRange(new object[] { "in - إدخال", "out - إخراج", "adjustment - تسوية" });
            typeCombo.SelectedIndex = 0; y += 38;

            var lblQty = new Label { Text = "الكمية:", Location = new Point(0, y), Size = new Size(120, 22), Font = new Font("Arial", 10) }; y += 24;
            var qtyBox = new NumericUpDown { Location = new Point(0, y), Size = new Size(150, 28), Font = new Font("Arial", 10), Minimum = 0.001m, Maximum = 999999, DecimalPlaces = 3, Value = 1 }; y += 38;

            var lblCost = new Label { Text = "تكلفة الوحدة:", Location = new Point(0, y), Size = new Size(120, 22), Font = new Font("Arial", 10) }; y += 24;
            var costBox = new NumericUpDown { Location = new Point(0, y), Size = new Size(150, 28), Font = new Font("Arial", 10), Maximum = 9999999, ThousandsSeparator = true }; y += 38;

            var lblNotes = new Label { Text = "ملاحظات:", Location = new Point(0, y), Size = new Size(120, 22), Font = new Font("Arial", 10) }; y += 24;
            var notesBox = new TextBox { Location = new Point(0, y), Size = new Size(360, 28), Font = new Font("Arial", 10) }; y += 46;

            var btnSave = new Button { Text = "✔ تسجيل الحركة", Location = new Point(0, y), Size = new Size(200, 42),
                BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                if (itemCombo.SelectedIndex < 0) { MessageBox.Show("اختر صنفاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var item = items[itemCombo.SelectedIndex];
                var typeFull = typeCombo.SelectedItem?.ToString() ?? "in";
                var moveType = typeFull.Split(' ')[0];
                StockService.AddMovement(item.Id, moveType, qtyBox.Value, costBox.Value, notesBox.Text.Trim(), _currentUser.Id);
                MessageBox.Show("تم تسجيل الحركة بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                notesBox.Clear(); qtyBox.Value = 1; costBox.Value = 0;
            };

            panel.Controls.AddRange(new Control[] { lblTitle, lblItem, itemCombo, lblType, typeCombo, lblQty, qtyBox, lblCost, costBox, lblNotes, notesBox, btnSave });
            page.Controls.Add(panel);
            return page;
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
                var btn = new Button { Text = text, Size = new Size(130, 36), BackColor = color, ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 0, 0, 0) };
                btn.FlatAppearance.BorderSize = 0; btn.Click += click; flow.Controls.Add(btn);
            }
            panel.Controls.Add(flow); return panel;
        }
    }
}
