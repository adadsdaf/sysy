using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class MainForm : Form
    {
        private User _currentUser;
        private Invoice _currentInvoice;
        private AppSettings _settings;
        private List<Item> _allItems = new();
        private List<Category> _categories = new();
        private int? _selectedCategoryId = null;

        // Controls
        private Panel _itemsPanel = null!;
        private DataGridView _invoiceGrid = null!;
        private Label _subtotalLbl = null!, _taxLbl = null!, _discountLbl = null!, _totalLbl = null!;
        private Label _invoiceNumLbl = null!, _cashierLbl = null!, _statusBar = null!;
        private TextBox _searchBox = null!;
        private Panel _categoriesPanel = null!;
        private NumericUpDown _discountInput = null!;
        private ComboBox _orderTypeCombo = null!, _tableCombo = null!, _customerCombo = null!;
        private TextBox _notesBox = null!;

        public MainForm(User user)
        {
            _currentUser = user;
            _settings = SettingsService.Get();
            _currentInvoice = InvoiceService.CreateNew(user.Id);
            InitializeComponent();
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            LoadData();
            UpdateInvoiceDisplay();
        }

        private void InitializeComponent()
        {
            this.Text = $"نقطة المبيعات - {_settings.StoreName}";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(235, 238, 245);
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            BuildTopBar();
            BuildRightPanel();   // Right = invoice panel (RTL: appears on left visually)
            BuildCenterPanel();  // Center = items grid
            BuildBottomBar();
        }

        // ──────────────── TOP BAR ────────────────────────────────────────
        private void BuildTopBar()
        {
            var topBar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(22, 28, 55) };

            var titleLbl = new Label
            {
                Text = _settings.StoreName, Font = new Font("Arial", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 210, 70), TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(topBar.Width - 260, 0), Size = new Size(250, 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _invoiceNumLbl = new Label
            {
                Font = new Font("Arial", 10), ForeColor = Color.FromArgb(160, 190, 240),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(350, 0), Size = new Size(320, 54)
            };

            _cashierLbl = new Label
            {
                Text = $"الكاشير: {_currentUser.Name}", Font = new Font("Arial", 10),
                ForeColor = Color.FromArgb(140, 220, 160), TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(10, 0), Size = new Size(210, 54)
            };

            var timeLbl = new Label
            {
                Font = new Font("Arial", 10), ForeColor = Color.FromArgb(190, 200, 225),
                TextAlign = ContentAlignment.MiddleLeft, Location = new Point(220, 0), Size = new Size(200, 54)
            };
            var clock = new System.Windows.Forms.Timer { Interval = 1000 };
            clock.Tick += (s, e) => timeLbl.Text = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy");
            clock.Start();

            var btns = new List<Control>();

            if (_currentUser.IsAdmin || _currentUser.Permissions.CanAccessManagement || _currentUser.Permissions.CanManageItems)
            {
                var btnManage = MakeTopBtn("الإدارة", Color.FromArgb(65, 75, 140));
                btnManage.Location = new Point(topBar.Width - 540, 9);
                btnManage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                btnManage.Click += (s, e) => { using var f = new ManagementForm(_currentUser); f.ShowDialog(); LoadData(); _settings = SettingsService.Get(); };
                btns.Add(btnManage);
            }

            var btnLogout = MakeTopBtn("خروج", Color.FromArgb(140, 50, 50));
            btnLogout.Location = new Point(topBar.Width - 420, 9);
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.Click += (s, e) => this.Close();

            var btnNewInv = MakeTopBtn("فاتورة جديدة (F5)", Color.FromArgb(0, 110, 90));
            btnNewInv.Size = new Size(150, 36);
            btnNewInv.Location = new Point(topBar.Width - 280, 9);
            btnNewInv.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNewInv.Click += (s, e) => NewInvoice();

            btns.AddRange(new Control[] { titleLbl, _invoiceNumLbl, _cashierLbl, timeLbl, btnLogout, btnNewInv });
            topBar.Controls.AddRange(btns.ToArray());
            this.Controls.Add(topBar);
        }

        private Button MakeTopBtn(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text, Size = new Size(110, 36), BackColor = bg,
                ForeColor = Color.White, Font = new Font("Arial", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ──────────────── RIGHT PANEL (Invoice / Cart) ────────────────────
        private void BuildRightPanel()
        {
            var rightPanel = new Panel
            {
                Dock = DockStyle.Right, Width = 400,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(40, 55, 100) };
            var hdrLbl = new Label
            {
                Text = "قائمة الطلب", Font = new Font("Arial", 13, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter
            };
            header.Controls.Add(hdrLbl);

            // Order type + table (compact row)
            var orderRow = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(245, 247, 252), Padding = new Padding(4, 4, 4, 4) };
            _orderTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9),
                Dock = DockStyle.Right, Width = 110
            };
            _orderTypeCombo.Items.AddRange(new object[] { "محلي", "سفري", "توصيل" });
            _orderTypeCombo.SelectedIndex = 0;

            _tableCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9),
                Dock = DockStyle.Right, Width = 100
            };
            _tableCombo.Items.Add("(بدون طاولة)");
            for (int i = 1; i <= 30; i++) _tableCombo.Items.Add($"طاولة {i}");
            _tableCombo.SelectedIndex = 0;

            var custRow = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.FromArgb(245, 247, 252), Padding = new Padding(4, 3, 4, 3) };
            _customerCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9),
                Dock = DockStyle.Fill
            };
            _customerCombo.Items.Add(new Customer { Id = 0, Name = "(بدون عميل)" });
            _customerCombo.DisplayMember = "Name"; _customerCombo.ValueMember = "Id";
            _customerCombo.SelectedIndex = 0;
            var custLbl = new Label { Text = "العميل:", Dock = DockStyle.Right, Width = 55, Font = new Font("Arial", 9), TextAlign = ContentAlignment.MiddleCenter };
            custRow.Controls.AddRange(new Control[] { _customerCombo, custLbl });

            orderRow.Controls.AddRange(new Control[] { _orderTypeCombo, _tableCombo });

            // Invoice grid
            _invoiceGrid = new DataGridView
            {
                Dock = DockStyle.Fill, AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, Font = new Font("Arial", 10),
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(220, 225, 235),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 70, 140),
                    ForeColor = Color.White,
                    Font = new Font("Arial", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 32 },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 248, 255) }
            };
            _invoiceGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الصنف", DataPropertyName = "Name", Width = 145, ReadOnly = true });
            _invoiceGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الكمية", DataPropertyName = "Quantity", Width = 58 });
            _invoiceGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "السعر", DataPropertyName = "Total", Width = 80, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
            _invoiceGrid.CellValueChanged += InvoiceGrid_CellValueChanged;
            _invoiceGrid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) RemoveSelectedItem(); };

            // Discount row
            var discRow = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.FromArgb(245, 247, 252), Padding = new Padding(4, 3, 4, 3) };
            var discLbl = new Label { Text = "خصم:", Dock = DockStyle.Right, Width = 50, Font = new Font("Arial", 9), TextAlign = ContentAlignment.MiddleCenter };
            _discountInput = new NumericUpDown
            {
                Dock = DockStyle.Right, Width = 110, Font = new Font("Arial", 9),
                DecimalPlaces = 0, Maximum = 9999999, ThousandsSeparator = true
            };
            if (_currentUser.IsAdmin || _currentUser.Permissions.CanApplyDiscount)
                _discountInput.ValueChanged += (s, e) => { _currentInvoice.DiscountAmount = _discountInput.Value; UpdateTotals(); };
            else
                _discountInput.Enabled = false;

            discRow.Controls.AddRange(new Control[] { _discountInput, discLbl });

            // Notes row
            var notesRow = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.FromArgb(245, 247, 252), Padding = new Padding(4, 3, 4, 3) };
            _notesBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Arial", 9), PlaceholderText = "ملاحظات الطلب..." };
            var notesLbl = new Label { Text = "ملاحظات:", Dock = DockStyle.Right, Width = 65, Font = new Font("Arial", 9), TextAlign = ContentAlignment.MiddleCenter };
            notesRow.Controls.AddRange(new Control[] { _notesBox, notesLbl });

            // Totals
            var totalsPanel = new Panel { Dock = DockStyle.Bottom, Height = 110, BackColor = Color.FromArgb(240, 243, 252), Padding = new Padding(8) };
            _subtotalLbl = MakeTotalLabel("المجموع:", "", totalsPanel, 5);
            _taxLbl = MakeTotalLabel("الضريبة:", "", totalsPanel, 33);
            _discountLbl = MakeTotalLabel("الخصم:", "", totalsPanel, 61);
            _totalLbl = new Label
            {
                Font = new Font("Arial", 15, FontStyle.Bold), ForeColor = Color.FromArgb(210, 40, 40),
                TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Bottom, Height = 42,
                BackColor = Color.FromArgb(228, 232, 250), Padding = new Padding(8, 0, 0, 0)
            };
            totalsPanel.Controls.Add(_totalLbl);

            // Action buttons
            var actPanel = new Panel { Dock = DockStyle.Bottom, Height = 96, BackColor = Color.FromArgb(235, 238, 248) };

            var btnPay = MakeActionBtn("دفع (F12)", Color.FromArgb(22, 130, 60), new Point(6, 6), new Size(186, 50));
            btnPay.Click += BtnPay_Click;

            var btnPrint = MakeActionBtn("طباعة الفاتورة", Color.FromArgb(50, 90, 170), new Point(200, 6), new Size(186, 50));
            btnPrint.Click += (s, e) => PrintInvoice();

            var btnCancel = MakeActionBtn("إلغاء الفاتورة", Color.FromArgb(180, 45, 45), new Point(6, 60), new Size(186, 30));
            if (!(_currentUser.IsAdmin || _currentUser.Permissions.CanCancelInvoice)) btnCancel.Enabled = false;
            btnCancel.Click += (s, e) => CancelInvoice();

            var btnRemove = MakeActionBtn("حذف صنف (Del)", Color.FromArgb(120, 60, 0), new Point(200, 60), new Size(186, 30));
            btnRemove.Click += (s, e) => RemoveSelectedItem();

            actPanel.Controls.AddRange(new Control[] { btnPay, btnPrint, btnCancel, btnRemove });

            rightPanel.Controls.AddRange(new Control[]
            {
                actPanel, totalsPanel, discRow, notesRow, _invoiceGrid, custRow, orderRow, header
            });
            this.Controls.Add(rightPanel);
        }

        private Label MakeTotalLabel(string caption, string value, Panel parent, int y)
        {
            parent.Controls.Add(new Label { Text = caption, Font = new Font("Arial", 10), Location = new Point(6, y), Size = new Size(75, 24), TextAlign = ContentAlignment.MiddleRight });
            var lbl = new Label { Text = value, Font = new Font("Arial", 10, FontStyle.Bold), Location = new Point(85, y), Size = new Size(280, 24), TextAlign = ContentAlignment.MiddleLeft };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private Button MakeActionBtn(string text, Color bg, Point loc, Size size)
        {
            var btn = new Button
            {
                Text = text, Location = loc, Size = size, BackColor = bg,
                ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ──────────────── CENTER PANEL (Categories + Items Tiles) ─────────
        private void BuildCenterPanel()
        {
            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };

            // Search bar
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(2) };
            var searchLbl = new Label { Text = "بحث:", Dock = DockStyle.Right, Width = 55, Font = new Font("Arial", 11), TextAlign = ContentAlignment.MiddleCenter };
            _searchBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Arial", 12), PlaceholderText = "ابحث بالاسم أو رقم الصنف..." };
            _searchBox.TextChanged += (s, e) => LoadItems();
            searchPanel.Controls.AddRange(new Control[] { _searchBox, searchLbl });

            // Categories bar - styled like the reference image
            _categoriesPanel = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                AutoScroll = true, BackColor = Color.FromArgb(220, 225, 240)
            };

            // Items grid
            _itemsPanel = new Panel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = Color.FromArgb(235, 238, 245), Padding = new Padding(4)
            };

            centerPanel.Controls.AddRange(new Control[] { _itemsPanel, _categoriesPanel, searchPanel });
            this.Controls.Add(centerPanel);
        }

        // ──────────────── STATUS BAR ──────────────────────────────────────
        private void BuildBottomBar()
        {
            _statusBar = new Label
            {
                Dock = DockStyle.Bottom, Height = 26,
                BackColor = Color.FromArgb(40, 50, 80),
                ForeColor = Color.FromArgb(190, 210, 240),
                Font = new Font("Arial", 9), TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(8, 0, 8, 0), Text = "جاهز  |  F12: دفع  |  F5: فاتورة جديدة  |  Del: حذف صنف"
            };
            this.Controls.Add(_statusBar);
        }

        // ──────────────── DATA LOADING ────────────────────────────────────
        private void LoadData()
        {
            _settings = SettingsService.Get();
            _categories = CategoryService.GetAll();
            _allItems = ItemService.GetItems();
            LoadCustomersCombo();
            BuildCategoryButtons();
            LoadItems();
        }

        private void LoadCustomersCombo()
        {
            _customerCombo.Items.Clear();
            _customerCombo.Items.Add(new Customer { Id = 0, Name = "(بدون عميل)" });
            foreach (var c in CustomerService.GetAll()) _customerCombo.Items.Add(c);
            _customerCombo.SelectedIndex = 0;
        }

        private void BuildCategoryButtons()
        {
            _categoriesPanel.Controls.Clear();
            int x = 4;

            // "All" button
            var allBtn = new Button
            {
                Text = "الكل", Size = new Size(80, 44), Location = new Point(x, 6),
                BackColor = _selectedCategoryId == null ? Color.FromArgb(40, 70, 180) : Color.FromArgb(90, 100, 140),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            allBtn.FlatAppearance.BorderSize = 0;
            allBtn.Click += (s, e) => { _selectedCategoryId = null; LoadItems(); BuildCategoryButtons(); };
            _categoriesPanel.Controls.Add(allBtn);
            x += 88;

            // Favorites button
            var favBtn = new Button
            {
                Text = "⭐ مفضل", Size = new Size(95, 44), Location = new Point(x, 6),
                BackColor = Color.FromArgb(195, 140, 0), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold)
            };
            favBtn.FlatAppearance.BorderSize = 0;
            favBtn.Click += (s, e) => { _selectedCategoryId = -1; LoadItems(); };
            _categoriesPanel.Controls.Add(favBtn);
            x += 103;

            foreach (var cat in _categories)
            {
                var catLocal = cat;
                var isSelected = _selectedCategoryId == cat.Id;
                var catColor = ParseColor(cat.Color, Color.FromArgb(60, 120, 60));
                var selectedColor = AdjustBrightness(catColor, 0.7f);
                var btn = new Button
                {
                    Text = cat.Name, Size = new Size(110, 44), Location = new Point(x, 6),
                    BackColor = isSelected ? selectedColor : catColor,
                    ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 10, FontStyle.Bold), Tag = catLocal
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.BorderColor = Color.White;
                if (isSelected) btn.FlatAppearance.BorderSize = 2;
                btn.Click += (s, e) => { _selectedCategoryId = catLocal.Id; LoadItems(); BuildCategoryButtons(); };
                _categoriesPanel.Controls.Add(btn);
                x += 116;
            }

            _categoriesPanel.AutoScrollMinSize = new Size(x + 10, 0);
        }

        private void LoadItems()
        {
            _itemsPanel.Controls.Clear();
            var search = _searchBox?.Text.Trim() ?? "";
            List<Item> items;

            if (_selectedCategoryId == -1) items = ItemService.GetItems(favorites: true);
            else items = ItemService.GetItems(_selectedCategoryId, string.IsNullOrEmpty(search) ? null : search);

            int col = 0, row = 0;
            // Tile size: wider for 3 columns like the reference image
            int btnW = 145, btnH = 80, gap = 5, startX = 5, startY = 5;
            int panelWidth = _itemsPanel.Width > 10 ? _itemsPanel.Width : 600;
            int perRow = Math.Max(1, (panelWidth - startX * 2) / (btnW + gap));

            foreach (var item in items)
            {
                var itemLocal = item;
                var catColor = ParseColor(item.CategoryColor ?? "#E88020", Color.FromArgb(220, 130, 30));

                var btn = new Button
                {
                    Size = new Size(btnW, btnH),
                    Location = new Point(startX + col * (btnW + gap), startY + row * (btnH + gap)),
                    BackColor = catColor, ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Tag = itemLocal,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = AdjustBrightness(catColor, 1.15f);

                // Number + name + price — matching the reference image style
                var numPart = item.ItemNumber != null ? $"({item.ItemNumber})" : "";
                btn.Text = $"{numPart}\r\n{item.Name}\r\n{item.Price:N0}";
                btn.Font = new Font("Arial", 9, FontStyle.Bold);
                btn.Click += (s, e) => AddItemToInvoice(itemLocal);

                // Keyboard shortcut via ItemNumber
                _itemsPanel.Controls.Add(btn);

                col++;
                if (col >= perRow) { col = 0; row++; }
            }

            if (items.Count == 0)
            {
                var emptyLbl = new Label
                {
                    Text = "لا توجد أصناف في هذه الفئة",
                    Font = new Font("Arial", 13),
                    ForeColor = Color.FromArgb(150, 160, 180),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(startX, startY),
                    Size = new Size(panelWidth - 20, 80)
                };
                _itemsPanel.Controls.Add(emptyLbl);
            }

            _statusBar.Text = $"عدد الأصناف: {items.Count}  |  F12: دفع  |  F5: فاتورة جديدة  |  Del: حذف صنف";
        }

        // ──────────────── INVOICE OPERATIONS ─────────────────────────────
        private void AddItemToInvoice(Item item)
        {
            var existing = _currentInvoice.Items.FirstOrDefault(i => i.ItemId == item.Id);
            if (existing != null)
                existing.Quantity++;
            else
                _currentInvoice.Items.Add(new InvoiceItem
                {
                    ItemId = item.Id, Name = item.Name,
                    ItemNumber = item.ItemNumber,
                    CategoryName = item.CategoryName,
                    Price = item.Price,
                    Quantity = 1, TaxRate = item.TaxRate, DiscountRate = item.DiscountRate
                });

            UpdateInvoiceDisplay();
            _statusBar.Text = $"تم إضافة: {item.Name}  ({item.Price:N0} {_settings.CurrencySymbol})";
        }

        private void RemoveSelectedItem()
        {
            if (_invoiceGrid.CurrentRow == null || _invoiceGrid.CurrentRow.Index < 0) return;
            int idx = _invoiceGrid.CurrentRow.Index;
            if (idx < _currentInvoice.Items.Count)
            {
                _currentInvoice.Items.RemoveAt(idx);
                UpdateInvoiceDisplay();
            }
        }

        private void InvoiceGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentInvoice.Items.Count) return;
            if (_invoiceGrid.Columns[e.ColumnIndex].DataPropertyName == "Quantity")
            {
                if (decimal.TryParse(_invoiceGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString(), out var qty) && qty > 0)
                {
                    _currentInvoice.Items[e.RowIndex].Quantity = qty;
                    UpdateTotals();
                }
            }
        }

        private void UpdateInvoiceDisplay()
        {
            _invoiceGrid.DataSource = null;
            _invoiceGrid.DataSource = _currentInvoice.Items;
            _invoiceNumLbl.Text = $"فاتورة: {_currentInvoice.InvoiceNumber}  |  {_currentInvoice.CreatedAt:dd/MM/yyyy HH:mm}";
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            var sym = _settings.CurrencySymbol;
            _subtotalLbl.Text = $"{_currentInvoice.Subtotal:N0} {sym}";
            _taxLbl.Text = $"{_currentInvoice.TaxAmount:N0} {sym}";
            _discountLbl.Text = $"{_currentInvoice.DiscountAmount:N0} {sym}";
            _totalLbl.Text = $"  الإجمالي: {_currentInvoice.Total:N0} {sym}";
        }

        // ──────────────── PAYMENT ─────────────────────────────────────────
        private void BtnPay_Click(object? sender, EventArgs e)
        {
            if (!_currentInvoice.Items.Any()) { MessageBox.Show("الفاتورة فارغة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            InvoiceService.SaveItems(_currentInvoice.Id, _currentInvoice.Items);
            UpdateInvoiceHeader();

            using var payForm = new PaymentForm(_currentInvoice, _settings.CurrencySymbol);
            if (payForm.ShowDialog() == DialogResult.OK)
            {
                InvoiceService.ProcessPayment(_currentInvoice.Id, payForm.SelectedMethod, payForm.AmountPaid);
                var change = payForm.AmountPaid - _currentInvoice.Total;

                // Print automatically after payment
                PrintInvoice(autoprint: true);

                MessageBox.Show(
                    $"✅ تم الدفع بنجاح!\nالمبلغ المدفوع: {payForm.AmountPaid:N0} {_settings.CurrencySymbol}\nالباقي: {Math.Max(0, change):N0} {_settings.CurrencySymbol}",
                    "تم الدفع", MessageBoxButtons.OK, MessageBoxIcon.Information);
                NewInvoice();
            }
        }

        private void UpdateInvoiceHeader()
        {
            _currentInvoice.DiscountAmount = _discountInput.Value;
            _currentInvoice.Notes = _notesBox.Text;
            var custItem = _customerCombo.SelectedItem as Customer;
            _currentInvoice.CustomerId = custItem?.Id == 0 ? null : custItem?.Id;
            var types = new[] { "dine_in", "takeaway", "delivery" };
            _currentInvoice.OrderType = types[_orderTypeCombo.SelectedIndex];
            _currentInvoice.TableNumber = _tableCombo.SelectedIndex > 0 ? $"{_tableCombo.SelectedIndex}" : null;
            InvoiceService.UpdateHeader(_currentInvoice);
        }

        private void NewInvoice()
        {
            if (_currentInvoice.Items.Any() && _currentInvoice.Status == "open")
                InvoiceService.SaveItems(_currentInvoice.Id, _currentInvoice.Items);
            _currentInvoice = InvoiceService.CreateNew(_currentUser.Id);
            _discountInput.Value = 0;
            _customerCombo.SelectedIndex = 0;
            _orderTypeCombo.SelectedIndex = 0;
            _tableCombo.SelectedIndex = 0;
            _notesBox.Text = "";
            UpdateInvoiceDisplay();
            _statusBar.Text = "فاتورة جديدة  |  F12: دفع  |  Del: حذف صنف";
        }

        private void CancelInvoice()
        {
            if (MessageBox.Show("هل تريد إلغاء الفاتورة الحالية؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            InvoiceService.Cancel(_currentInvoice.Id);
            _currentInvoice = InvoiceService.CreateNew(_currentUser.Id);
            UpdateInvoiceDisplay();
            _statusBar.Text = "تم إلغاء الفاتورة";
        }

        // ──────────────── PRINTING ────────────────────────────────────────
        private void PrintInvoice(bool autoprint = false)
        {
            if (!_currentInvoice.Items.Any()) { if (!autoprint) MessageBox.Show("الفاتورة فارغة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            _settings = SettingsService.Get();
            var orderTypeText = _orderTypeCombo.SelectedIndex switch { 1 => "سفري", 2 => "توصيل", _ => "محلي" };
            var tableText = _tableCombo.SelectedIndex > 0 ? $"طاولة {_tableCombo.SelectedIndex}" : "";

            // Build customer receipt
            var customerReceipt = BuildCustomerReceipt(orderTypeText, tableText);
            // Build kitchen slips per category
            var kitchenSlips = BuildKitchenSlips(orderTypeText, tableText);

            var dlg = new Form
            {
                Text = "معاينة الطباعة", Size = new Size(780, 680),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(240, 242, 248),
                RightToLeft = RightToLeft.Yes, RightToLeftLayout = true
            };

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Arial", 10) };

            // Customer receipt tab
            var custTab = new TabPage("فاتورة العميل");
            var custRtb = new RichTextBox
            {
                Dock = DockStyle.Fill, Text = customerReceipt,
                Font = new Font("Courier New", 11), ReadOnly = true,
                BackColor = Color.White, RightToLeft = RightToLeft.Yes
            };
            custTab.Controls.Add(custRtb);
            tabs.TabPages.Add(custTab);

            // Kitchen slips tab
            if (kitchenSlips.Count > 0)
            {
                var kitTab = new TabPage("قصاصات الأقسام");
                var kitRtb = new RichTextBox
                {
                    Dock = DockStyle.Fill, Text = string.Join("\r\n\r\n" + new string('─', 32) + "\r\n\r\n", kitchenSlips),
                    Font = new Font("Courier New", 11), ReadOnly = true,
                    BackColor = Color.White, RightToLeft = RightToLeft.Yes
                };
                kitTab.Controls.Add(kitRtb);
                tabs.TabPages.Add(kitTab);
            }

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Color.FromArgb(235, 238, 248), Padding = new Padding(8) };

            var btnPrintCust = new Button
            {
                Text = "طباعة فاتورة العميل", Dock = DockStyle.Right, Width = 180,
                BackColor = Color.FromArgb(22, 130, 60), ForeColor = Color.White,
                Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat
            };
            btnPrintCust.FlatAppearance.BorderSize = 0;
            btnPrintCust.Click += (s, e) => DoPrint(customerReceipt, "فاتورة العميل");

            var btnPrintKit = new Button
            {
                Text = "طباعة قصاصات الأقسام", Dock = DockStyle.Right, Width = 200,
                BackColor = Color.FromArgb(50, 90, 170), ForeColor = Color.White,
                Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat
            };
            btnPrintKit.FlatAppearance.BorderSize = 0;
            btnPrintKit.Click += (s, e) =>
            {
                foreach (var slip in kitchenSlips) DoPrint(slip, "قصاصة قسم");
            };

            var btnClose = new Button
            {
                Text = "إغلاق", Dock = DockStyle.Left, Width = 100,
                BackColor = Color.FromArgb(120, 60, 60), ForeColor = Color.White,
                Font = new Font("Arial", 11), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderSize = 0;

            btnPanel.Controls.AddRange(new Control[] { btnPrintCust, btnPrintKit, btnClose });
            dlg.Controls.AddRange(new Control[] { tabs, btnPanel });
            dlg.ShowDialog(this);
        }

        private string BuildCustomerReceipt(string orderType, string tableText)
        {
            var sb = new System.Text.StringBuilder();
            var sym = _settings.CurrencySymbol;
            int w = 42;

            sb.AppendLine(Center(_settings.StoreName, w));
            sb.AppendLine(Center(_settings.StoreNameAr ?? "", w));
            sb.AppendLine();
            sb.AppendLine(Center("فاتورة خاصة بالزبون", w));
            sb.AppendLine(Center(new string('═', 20), w));
            sb.AppendLine();
            sb.AppendLine($"الوقت: {_currentInvoice.CreatedAt:dd/MM/yyyy  HH:mm}");
            sb.AppendLine($"رقم الفاتورة: {_currentInvoice.InvoiceNumber}");
            sb.AppendLine($"نوع الطلب: {orderType}");
            if (!string.IsNullOrEmpty(tableText)) sb.AppendLine($"الطاولة: {tableText}");
            sb.AppendLine($"الكاشير: {_currentUser.Name}");
            sb.AppendLine(new string('─', w));
            sb.AppendLine($"{"الصنف",-22} {"الكمية",5} {"السعر",10}");
            sb.AppendLine(new string('─', w));
            foreach (var item in _currentInvoice.Items)
                sb.AppendLine($"{item.Name,-22} {item.Quantity,5:N0} {item.Total,10:N0}");
            sb.AppendLine(new string('═', w));
            sb.AppendLine($"{"المجموع:",-26} {_currentInvoice.Subtotal,12:N0} {sym}");
            if (_currentInvoice.TaxAmount > 0) sb.AppendLine($"{"الضريبة:",-26} {_currentInvoice.TaxAmount,12:N0} {sym}");
            if (_currentInvoice.DiscountAmount > 0) sb.AppendLine($"{"الخصم:",-26} {_currentInvoice.DiscountAmount,12:N0} {sym}");
            sb.AppendLine($"{"الإجمالي:",-26} {_currentInvoice.Total,12:N0} {sym}");
            sb.AppendLine(new string('═', w));
            sb.AppendLine(Center(_currentUser.Name, w));
            sb.AppendLine();
            if (!string.IsNullOrEmpty(_currentInvoice.Notes)) { sb.AppendLine($"ملاحظات: {_currentInvoice.Notes}"); sb.AppendLine(); }
            if (!string.IsNullOrEmpty(_settings.ReceiptNotes)) sb.AppendLine(Center(_settings.ReceiptNotes, w));
            if (!string.IsNullOrEmpty(_settings.ReceiptFooter)) sb.AppendLine(Center(_settings.ReceiptFooter, w));
            if (!string.IsNullOrEmpty(_settings.Phone)) sb.AppendLine(Center(_settings.Phone, w));
            sb.AppendLine(Center(DateTime.Now.ToString("HH:mm  dd/MM/yyyy"), w));
            return sb.ToString();
        }

        private List<string> BuildKitchenSlips(string orderType, string tableText)
        {
            // Group items by category
            var groups = _currentInvoice.Items
                .GroupBy(i => i.CategoryName ?? "عام")
                .ToList();

            var slips = new List<string>();
            int w = 32;

            foreach (var grp in groups)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(Center($"=== {grp.Key} ===", w));
                sb.AppendLine(Center(DateTime.Now.ToString("HH:mm  dd/MM/yyyy"), w));
                sb.AppendLine($"فاتورة: {_currentInvoice.InvoiceNumber}");
                sb.AppendLine($"نوع: {orderType}");
                if (!string.IsNullOrEmpty(tableText)) sb.AppendLine($"الطاولة: {tableText}");
                sb.AppendLine($"الكاشير: {_currentUser.Name}");
                sb.AppendLine(new string('─', w));
                foreach (var item in grp)
                    sb.AppendLine($"{item.Quantity,3:N0}x  {item.Name}");
                sb.AppendLine(new string('─', w));
                if (!string.IsNullOrEmpty(_currentInvoice.Notes)) sb.AppendLine($"ملاحظة: {_currentInvoice.Notes}");
                slips.Add(sb.ToString());
            }
            return slips;
        }

        private void DoPrint(string content, string docName)
        {
            var pd = new System.Drawing.Printing.PrintDocument();
            pd.DocumentName = docName;
            pd.PrintPage += (ps, pe) =>
            {
                pe.Graphics!.DrawString(content,
                    new Font("Courier New", 9),
                    Brushes.Black,
                    new System.Drawing.PointF(20, 20));
            };
            var prd = new PrintDialog { Document = pd };
            if (prd.ShowDialog() == DialogResult.OK) pd.Print();
        }

        // ──────────────── KEYBOARD SHORTCUTS ─────────────────────────────
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) { _searchBox.Focus(); _searchBox.SelectAll(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { NewInvoice(); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { BtnPay_Click(sender, e); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete && !_invoiceGrid.IsCurrentCellInEditMode) { RemoveSelectedItem(); e.Handled = true; }
            else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !_searchBox.Focused)
            {
                // Quick add by item number typed on keyboard
                _searchBox.Text += (char)('0' + (e.KeyCode - Keys.D0));
                _searchBox.Focus();
                _searchBox.SelectionStart = _searchBox.Text.Length;
            }
        }

        // ──────────────── HELPERS ─────────────────────────────────────────
        private static string Center(string text, int width = 42)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length >= width) return text;
            int totalPad = width - text.Length;
            int left = totalPad / 2;
            int right = totalPad - left;
            return new string(' ', left) + text + new string(' ', right);
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            try { return ColorTranslator.FromHtml(hex); } catch { return fallback; }
        }

        private static Color AdjustBrightness(Color color, float factor)
        {
            return Color.FromArgb(color.A,
                (int)Math.Min(255, color.R * factor),
                (int)Math.Min(255, color.G * factor),
                (int)Math.Min(255, color.B * factor));
        }
    }
}
