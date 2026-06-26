using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class ManagementForm : Form
    {
        private readonly User _currentUser;
        private TabControl _tabs = null!;

        public ManagementForm(User currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة النظام";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Arial", 11) };

            _tabs.TabPages.Add(BuildItemsTab());
            _tabs.TabPages.Add(BuildCategoriesTab());

            if (_currentUser.IsAdmin || _currentUser.Permissions.CanManageCustomers)
                _tabs.TabPages.Add(BuildCustomersTab());

            if (_currentUser.IsAdmin)
            {
                _tabs.TabPages.Add(BuildUsersTab());
                _tabs.TabPages.Add(BuildSettingsTab());
            }

            if (_currentUser.IsAdmin || _currentUser.Permissions.CanViewReports)
                _tabs.TabPages.Add(BuildReportsTab());

            this.Controls.Add(_tabs);
        }

        // ─── ITEMS TAB ─────────────────────────────────────────────────────
        private DataGridView _itemsGrid = null!;
        private TabPage BuildItemsTab()
        {
            var page = new TabPage("الأصناف");
            _itemsGrid = MakeGrid();
            _itemsGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الرقم", DataPropertyName = "ItemNumber", Width = 70 },
                new DataGridViewTextBoxColumn { HeaderText = "الاسم", DataPropertyName = "Name", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "السعر", DataPropertyName = "Price", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الفئة", DataPropertyName = "CategoryName", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "الباركود", DataPropertyName = "Barcode", Width = 110 },
                new DataGridViewCheckBoxColumn { HeaderText = "مفضل", DataPropertyName = "IsFavorite", Width = 65 },
                new DataGridViewCheckBoxColumn { HeaderText = "متاح", DataPropertyName = "IsAvailable", Width = 60 }
            );

            bool canEdit = _currentUser.IsAdmin || _currentUser.Permissions.CanManageItems;
            var toolbarBtns = new List<(string, Color, EventHandler)>();
            if (canEdit)
            {
                toolbarBtns.Add(("+ إضافة", Color.FromArgb(34, 139, 34), (s, e) => { using var f = new ItemEditForm(); if (f.ShowDialog() == DialogResult.OK && f.ResultItem != null) { ItemService.Create(f.ResultItem); LoadItems(); } }));
                toolbarBtns.Add(("تعديل", Color.FromArgb(30, 100, 200), (s, e) => EditSelectedItem()));
                toolbarBtns.Add(("حذف", Color.FromArgb(200, 50, 50), (s, e) => DeleteSelectedItem(_itemsGrid, id => { ItemService.Delete(id); LoadItems(); })));
            }
            toolbarBtns.Add(("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadItems()));

            var toolbar = MakeToolbar(toolbarBtns.ToArray());
            LoadItems();
            page.Controls.AddRange(new Control[] { toolbar, _itemsGrid });
            toolbar.Dock = DockStyle.Top; _itemsGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadItems() => _itemsGrid.DataSource = ItemService.GetItems();

        private void EditSelectedItem()
        {
            if (_itemsGrid.CurrentRow?.DataBoundItem is not Item item) return;
            using var f = new ItemEditForm(item);
            if (f.ShowDialog() == DialogResult.OK && f.ResultItem != null) { ItemService.Update(f.ResultItem); LoadItems(); }
        }

        // ─── CATEGORIES TAB ───────────────────────────────────────────────
        private DataGridView _catGrid = null!;
        private TabPage BuildCategoriesTab()
        {
            var page = new TabPage("الأقسام");
            _catGrid = MakeGrid();
            _catGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الاسم", DataPropertyName = "Name", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "اللون (HEX)", DataPropertyName = "Color", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "الترتيب", DataPropertyName = "SortOrder", Width = 80 }
            );

            bool canEdit = _currentUser.IsAdmin || _currentUser.Permissions.CanManageItems;
            var toolbarBtns = new List<(string, Color, EventHandler)>();
            if (canEdit)
            {
                toolbarBtns.Add(("+ إضافة", Color.FromArgb(34, 139, 34), (s, e) => AddCategory()));
                toolbarBtns.Add(("تعديل", Color.FromArgb(30, 100, 200), (s, e) => EditCategory()));
                toolbarBtns.Add(("حذف", Color.FromArgb(200, 50, 50), (s, e) => DeleteSelectedItem(_catGrid, id => { CategoryService.Delete(id); LoadCategories(); })));
            }
            toolbarBtns.Add(("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadCategories()));

            var toolbar = MakeToolbar(toolbarBtns.ToArray());
            LoadCategories();
            page.Controls.AddRange(new Control[] { toolbar, _catGrid });
            toolbar.Dock = DockStyle.Top; _catGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadCategories() => _catGrid.DataSource = CategoryService.GetAll();

        private void AddCategory()
        {
            using var dlg = new CategoryEditDialog(new Category { Color = "#E88020", SortOrder = 0 });
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { CategoryService.Create(dlg.Result); LoadCategories(); }
        }

        private void EditCategory()
        {
            if (_catGrid.CurrentRow?.DataBoundItem is not Category cat) return;
            using var dlg = new CategoryEditDialog(cat);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { CategoryService.Update(dlg.Result); LoadCategories(); }
        }

        // ─── CUSTOMERS TAB ────────────────────────────────────────────────
        private DataGridView _custGrid = null!;
        private TabPage BuildCustomersTab()
        {
            var page = new TabPage("العملاء");
            _custGrid = MakeGrid();
            _custGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الاسم", DataPropertyName = "Name", Width = 180 },
                new DataGridViewTextBoxColumn { HeaderText = "الجوال", DataPropertyName = "Phone", Width = 120 },
                new DataGridViewTextBoxColumn { HeaderText = "البريد", DataPropertyName = "Email", Width = 180 },
                new DataGridViewTextBoxColumn { HeaderText = "إجمالي المشتريات", DataPropertyName = "TotalPurchases", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } }
            );
            var toolbar = MakeToolbar(
                ("+ إضافة", Color.FromArgb(34, 139, 34), (s, e) => AddCustomer()),
                ("حذف", Color.FromArgb(200, 50, 50), (s, e) => DeleteSelectedItem(_custGrid, id => { CustomerService.Delete(id); LoadCustomers(); }))
            );
            LoadCustomers();
            page.Controls.AddRange(new Control[] { toolbar, _custGrid });
            toolbar.Dock = DockStyle.Top; _custGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadCustomers() => _custGrid.DataSource = CustomerService.GetAll();

        private void AddCustomer()
        {
            var name = InputDialog("اسم العميل:");
            if (string.IsNullOrEmpty(name)) return;
            var phone = InputDialog("رقم الجوال:");
            CustomerService.Create(new Customer { Name = name, Phone = phone });
            LoadCustomers();
        }

        // ─── USERS TAB (Admin only) ───────────────────────────────────────
        private DataGridView _usersGrid = null!;
        private TabPage BuildUsersTab()
        {
            var page = new TabPage("المستخدمون والصلاحيات");
            _usersGrid = MakeGrid();
            _usersGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الاسم", DataPropertyName = "Name", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "الدور", DataPropertyName = "Role", Width = 90 },
                new DataGridViewTextBoxColumn { HeaderText = "PIN", DataPropertyName = "Pin", Width = 70 },
                new DataGridViewCheckBoxColumn { HeaderText = "نشط", DataPropertyName = "IsActive", Width = 55 }
            );

            var toolbar = MakeToolbar(
                ("+ إضافة", Color.FromArgb(34, 139, 34), (s, e) => AddUser()),
                ("تعديل الصلاحيات", Color.FromArgb(30, 100, 200), (s, e) => EditUserPermissions()),
                ("تعديل PIN", Color.FromArgb(100, 70, 160), (s, e) => EditUserPin()),
                ("حذف", Color.FromArgb(200, 50, 50), (s, e) => DeleteSelectedItem(_usersGrid, id => { UserService.Delete(id); LoadUsers(); }))
            );

            // Permissions legend
            var legendPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(235, 240, 255), Padding = new Padding(10, 5, 10, 5) };
            var legendLbl = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 9),
                ForeColor = Color.FromArgb(60, 80, 140),
                Text = "انقر على 'تعديل الصلاحيات' لتحديد ما يمكن للكاشير فعله: الدخول لـ POS | إدارة الأصناف | عرض التقارير | تطبيق الخصم | إلغاء الفاتورة | إدارة العملاء",
                TextAlign = ContentAlignment.MiddleRight
            };
            legendPanel.Controls.Add(legendLbl);

            LoadUsers();
            page.Controls.AddRange(new Control[] { toolbar, legendPanel, _usersGrid });
            toolbar.Dock = DockStyle.Top; _usersGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadUsers() => _usersGrid.DataSource = UserService.GetAll();

        private void AddUser()
        {
            var name = InputDialog("اسم المستخدم:");
            if (string.IsNullOrEmpty(name)) return;
            var pin = InputDialog("رمز PIN (على الأقل 4 أرقام):");
            if (string.IsNullOrEmpty(pin) || pin.Length < 4) { MessageBox.Show("PIN يجب أن يكون 4 أرقام على الأقل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var roleResult = MessageBox.Show("هل تريد منح صلاحية مدير؟ (لا = كاشير)", "نوع المستخدم", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            var role = roleResult == DialogResult.Yes ? "admin" : "cashier";

            var newUser = new User { Name = name, Pin = pin, Role = role, IsActive = true };
            if (role == "admin") newUser.Permissions = new UserPermissions { CanAccessPOS = true, CanManageItems = true, CanViewReports = true, CanApplyDiscount = true, CanCancelInvoice = true, CanAccessManagement = true, CanManageCustomers = true };
            UserService.Create(newUser);
            LoadUsers();

            if (role == "cashier")
            {
                MessageBox.Show("تم إنشاء الحساب. استخدم 'تعديل الصلاحيات' لتحديد صلاحيات الكاشير.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                EditUserPermissions();
            }
        }

        private void EditUserPermissions()
        {
            if (_usersGrid.CurrentRow?.DataBoundItem is not User user) return;
            if (user.IsAdmin) { MessageBox.Show("المدير لديه جميع الصلاحيات تلقائياً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            using var dlg = new UserPermissionsDialog(user);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                UserService.Update(dlg.UpdatedUser);
                LoadUsers();
                MessageBox.Show("تم تحديث الصلاحيات بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void EditUserPin()
        {
            if (_usersGrid.CurrentRow?.DataBoundItem is not User user) return;
            var newPin = InputDialog("أدخل PIN الجديد:", "");
            if (string.IsNullOrEmpty(newPin) || newPin.Length < 4) { MessageBox.Show("PIN يجب أن يكون 4 أرقام على الأقل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            user.Pin = newPin;
            UserService.Update(user);
            LoadUsers();
            MessageBox.Show("تم تحديث PIN بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ─── SETTINGS TAB (Admin only) ────────────────────────────────────
        private TabPage BuildSettingsTab()
        {
            var page = new TabPage("الإعدادات والطباعة");
            var s = SettingsService.Get();
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };

            var y = 20;
            var storeNameBox = AddSetting(panel, ref y, "اسم المطعم/المحل:", s.StoreName);
            var storeNameArBox = AddSetting(panel, ref y, "الاسم بالعربي:", s.StoreNameAr ?? "");
            var addressBox = AddSetting(panel, ref y, "العنوان:", s.Address ?? "");
            var phoneBox = AddSetting(panel, ref y, "أرقام التواصل:", s.Phone ?? "");
            var taxBox = AddSetting(panel, ref y, "نسبة الضريبة %:", s.TaxRate.ToString());
            var svcBox = AddSetting(panel, ref y, "نسبة الخدمة %:", s.ServiceRate.ToString());

            y += 10;
            panel.Controls.Add(new Label { Text = "─── إعدادات الطباعة ───", Location = new Point(20, y), Size = new Size(500, 24), Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.FromArgb(50, 80, 150) });
            y += 32;

            var footerBox = AddSetting(panel, ref y, "تذييل الفاتورة:", s.ReceiptFooter ?? "");
            var notesBox = AddSetting(panel, ref y, "ملاحظات الفاتورة:", s.ReceiptNotes ?? "الطلب لا يمكن استرجاعه أو الغاؤه");

            var printCustChk = new CheckBox { Text = "طباعة نسخة للعميل", Checked = s.PrintCustomerCopy, Location = new Point(20, y), Size = new Size(220, 28), Font = new Font("Arial", 11) };
            panel.Controls.Add(printCustChk); y += 36;

            var printKitChk = new CheckBox { Text = "طباعة قصاصات الأقسام", Checked = s.PrintKitchenSlips, Location = new Point(20, y), Size = new Size(220, 28), Font = new Font("Arial", 11) };
            panel.Controls.Add(printKitChk); y += 42;

            var btnSave = new Button
            {
                Text = "حفظ الإعدادات", Location = new Point(20, y + 10), Size = new Size(200, 44),
                BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (sender, e) =>
            {
                s.StoreName = storeNameBox.Text.Trim();
                s.StoreNameAr = storeNameArBox.Text.Trim();
                s.Address = addressBox.Text.Trim();
                s.Phone = phoneBox.Text.Trim();
                if (decimal.TryParse(taxBox.Text, out var tax)) s.TaxRate = tax;
                if (decimal.TryParse(svcBox.Text, out var svc)) s.ServiceRate = svc;
                s.ReceiptFooter = footerBox.Text.Trim();
                s.ReceiptNotes = notesBox.Text.Trim();
                s.PrintCustomerCopy = printCustChk.Checked;
                s.PrintKitchenSlips = printKitChk.Checked;
                SettingsService.Save(s);
                SettingsService.ClearCache();
                MessageBox.Show("تم حفظ الإعدادات بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            panel.Controls.Add(btnSave);
            page.Controls.Add(panel);
            return page;
        }

        private TextBox AddSetting(Panel panel, ref int y, string label, string value)
        {
            panel.Controls.Add(new Label { Text = label, Location = new Point(20, y), Size = new Size(190, 26), Font = new Font("Arial", 11), TextAlign = ContentAlignment.MiddleRight });
            var box = new TextBox { Text = value, Location = new Point(220, y), Size = new Size(380, 28), Font = new Font("Arial", 11) };
            panel.Controls.Add(box); y += 42; return box;
        }

        // ─── REPORTS TAB ─────────────────────────────────────────────────
        private TabPage BuildReportsTab()
        {
            var page = new TabPage("التقارير");
            var grid = MakeGrid();
            grid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "رقم الفاتورة", DataPropertyName = "InvoiceNumber", Width = 150 },
                new DataGridViewTextBoxColumn { HeaderText = "الحالة", DataPropertyName = "Status", Width = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "النوع", DataPropertyName = "OrderType", Width = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "الطاولة", DataPropertyName = "TableNumber", Width = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "الكاشير", DataPropertyName = "CashierName", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", DataPropertyName = "Total", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "طريقة الدفع", DataPropertyName = "PaymentMethod", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "CreatedAt", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } }
            );
            var toolbar = MakeToolbar(("تحديث", Color.FromArgb(30, 100, 200), (s, e) => grid.DataSource = InvoiceService.GetAll()));
            grid.DataSource = InvoiceService.GetAll();
            page.Controls.AddRange(new Control[] { toolbar, grid });
            toolbar.Dock = DockStyle.Top; grid.Dock = DockStyle.Fill;
            return page;
        }

        // ─── HELPERS ─────────────────────────────────────────────────────
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
                var btn = new Button
                {
                    Text = text, Size = new Size(text.Length > 8 ? 160 : 120, 36),
                    BackColor = color, ForeColor = Color.White,
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 0, 0, 0)
                };
                btn.FlatAppearance.BorderSize = 0; btn.Click += click;
                flow.Controls.Add(btn);
            }
            panel.Controls.Add(flow);
            return panel;
        }

        private static void DeleteSelectedItem(DataGridView grid, Action<int> deleteAction)
        {
            if (grid.CurrentRow == null) return;
            int id = 0;
            // Try to get Id from bound object via reflection
            var item = grid.CurrentRow.DataBoundItem;
            if (item != null)
            {
                var prop = item.GetType().GetProperty("Id");
                if (prop != null) id = (int)(prop.GetValue(item) ?? 0);
            }
            if (id <= 0) return;
            if (MessageBox.Show("هل تريد حذف هذا العنصر؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { deleteAction(id); }
            catch (Exception ex) { MessageBox.Show($"لا يمكن الحذف: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static string? InputDialog(string prompt, string defaultValue = "")
        {
            var form = new Form { Text = prompt, Size = new Size(400, 165), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
            var lbl = new Label { Text = prompt, Location = new Point(10, 15), Size = new Size(370, 25), Font = new Font("Arial", 11) };
            var box = new TextBox { Text = defaultValue, Location = new Point(10, 45), Size = new Size(370, 28), Font = new Font("Arial", 11) };
            var ok = new Button { Text = "موافق", DialogResult = DialogResult.OK, Location = new Point(210, 90), Size = new Size(85, 30), BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Location = new Point(305, 90), Size = new Size(75, 30), FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0;
            form.Controls.AddRange(new Control[] { lbl, box, ok, cancel });
            form.AcceptButton = ok; form.CancelButton = cancel;
            return form.ShowDialog() == DialogResult.OK ? box.Text.Trim() : null;
        }
    }

    // ─── CATEGORY EDIT DIALOG ─────────────────────────────────────────────
    public class CategoryEditDialog : Form
    {
        public Category? Result { get; private set; }
        private TextBox _nameBox = null!;
        private TextBox _colorBox = null!;
        private NumericUpDown _sortBox = null!;
        private Panel _colorPreview = null!;
        private readonly Category _original;

        public CategoryEditDialog(Category cat)
        {
            _original = cat;
            this.Text = cat.Id == 0 ? "إضافة قسم جديد" : "تعديل القسم";
            this.Size = new Size(430, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            int y = 20;
            Label Lbl(string t) { var l = new Label { Text = t, Location = new Point(20, y), Size = new Size(380, 24), Font = new Font("Arial", 11) }; return l; }
            TextBox Txt(string v) { var tb = new TextBox { Text = v, Location = new Point(20, y + 26), Size = new Size(380, 28), Font = new Font("Arial", 11) }; return tb; }

            var lbl1 = Lbl("اسم القسم:"); _nameBox = Txt(cat.Name); y += 64;
            var lbl2 = Lbl("لون القسم (مثال: #FF9800):"); _colorBox = Txt(cat.Color); y += 26;

            // Color preview
            _colorPreview = new Panel { Location = new Point(20, y + 30), Size = new Size(60, 30), BackColor = TryParseColor(cat.Color), BorderStyle = BorderStyle.FixedSingle };
            _colorBox.TextChanged += (s, e) => { try { _colorPreview.BackColor = TryParseColor(_colorBox.Text); } catch { } };

            // Color swatches
            var colors = new[] { "#E53935", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5", "#2196F3", "#03A9F4", "#009688", "#4CAF50", "#8BC34A", "#FF9800", "#FF5722", "#795548", "#607D8B", "#E88020" };
            var swatchFlow = new FlowLayoutPanel { Location = new Point(20, y + 66), Size = new Size(380, 42), FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
            foreach (var c in colors)
            {
                var colorLocal = c;
                var sw = new Panel { Size = new Size(24, 24), BackColor = TryParseColor(c), Cursor = Cursors.Hand, Margin = new Padding(2) };
                sw.Click += (s, e) => { _colorBox.Text = colorLocal; _colorPreview.BackColor = TryParseColor(colorLocal); };
                swatchFlow.Controls.Add(sw);
            }
            y += 118;

            var lbl3 = new Label { Text = "الترتيب:", Location = new Point(20, y), Size = new Size(200, 24), Font = new Font("Arial", 11) };
            _sortBox = new NumericUpDown { Value = cat.SortOrder, Location = new Point(20, y + 26), Size = new Size(120, 28), Font = new Font("Arial", 11), Minimum = 0, Maximum = 999 };
            y += 66;

            var btnOk = new Button { Text = "حفظ", Location = new Point(20, y), Size = new Size(130, 38), BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.None };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_nameBox.Text.Trim())) { MessageBox.Show("أدخل اسم القسم", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Result = new Category { Id = _original.Id, Name = _nameBox.Text.Trim(), Color = _colorBox.Text.Trim(), SortOrder = (int)_sortBox.Value };
                this.DialogResult = DialogResult.OK; Close();
            };

            var btnCancel = new Button { Text = "إلغاء", Location = new Point(165, y), Size = new Size(120, 38), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };

            this.Size = new Size(430, y + 100);
            this.Controls.AddRange(new Control[] { lbl1, _nameBox, lbl2, _colorBox, _colorPreview, swatchFlow, lbl3, _sortBox, btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }

        private static Color TryParseColor(string hex) { try { return ColorTranslator.FromHtml(hex); } catch { return Color.Gray; } }
    }

    // ─── USER PERMISSIONS DIALOG ──────────────────────────────────────────
    public class UserPermissionsDialog : Form
    {
        public User UpdatedUser { get; private set; }
        private CheckBox _chkPOS = null!, _chkItems = null!, _chkReports = null!,
                         _chkDiscount = null!, _chkCancel = null!, _chkCustomers = null!;

        public UserPermissionsDialog(User user)
        {
            UpdatedUser = user;
            this.Text = $"صلاحيات: {user.Name}";
            this.Size = new Size(460, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var headerLbl = new Label
            {
                Text = $"تحديد صلاحيات الكاشير: {user.Name}",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 130),
                Location = new Point(20, 15), Size = new Size(400, 30)
            };

            var subLbl = new Label
            {
                Text = "ضع علامة على الصلاحيات التي تريد منحها لهذا الكاشير:",
                Font = new Font("Arial", 10),
                ForeColor = Color.FromArgb(100, 120, 160),
                Location = new Point(20, 48), Size = new Size(400, 22)
            };

            CheckBox MakeChk(string text, bool val, int y, string desc)
            {
                var chk = new CheckBox
                {
                    Text = text, Checked = val, Location = new Point(30, y),
                    Size = new Size(380, 26), Font = new Font("Arial", 11, FontStyle.Bold)
                };
                var descLbl = new Label { Text = desc, Location = new Point(55, y + 24), Size = new Size(360, 18), Font = new Font("Arial", 8.5f), ForeColor = Color.FromArgb(120, 140, 160) };
                this.Controls.AddRange(new Control[] { chk, descLbl });
                return chk;
            }

            _chkPOS = MakeChk("الدخول إلى نقطة المبيعات", user.Permissions.CanAccessPOS, 80, "السماح بالدخول لواجهة البيع وإنشاء الفواتير");
            _chkItems = MakeChk("إدارة الأصناف والأقسام", user.Permissions.CanManageItems, 130, "إضافة وتعديل وحذف الأصناف والفئات");
            _chkReports = MakeChk("عرض التقارير", user.Permissions.CanViewReports, 180, "الاطلاع على تقارير المبيعات والفواتير");
            _chkDiscount = MakeChk("تطبيق الخصومات", user.Permissions.CanApplyDiscount, 230, "السماح بإضافة خصم على الفواتير");
            _chkCancel = MakeChk("إلغاء الفواتير", user.Permissions.CanCancelInvoice, 280, "السماح بإلغاء الفاتورة الحالية");
            _chkCustomers = MakeChk("إدارة العملاء", user.Permissions.CanManageCustomers, 330, "إضافة وتعديل بيانات العملاء");

            var btnSave = new Button
            {
                Text = "حفظ الصلاحيات", Location = new Point(30, 375), Size = new Size(180, 42),
                BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White,
                Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.None
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                UpdatedUser.Permissions.CanAccessPOS = _chkPOS.Checked;
                UpdatedUser.Permissions.CanManageItems = _chkItems.Checked;
                UpdatedUser.Permissions.CanViewReports = _chkReports.Checked;
                UpdatedUser.Permissions.CanApplyDiscount = _chkDiscount.Checked;
                UpdatedUser.Permissions.CanCancelInvoice = _chkCancel.Checked;
                UpdatedUser.Permissions.CanManageCustomers = _chkCustomers.Checked;
                this.DialogResult = DialogResult.OK; Close();
            };

            var btnCancel = new Button
            {
                Text = "إلغاء", Location = new Point(225, 375), Size = new Size(120, 42),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { headerLbl, subLbl, btnSave, btnCancel });
            this.AcceptButton = btnSave; this.CancelButton = btnCancel;
        }
    }
}
