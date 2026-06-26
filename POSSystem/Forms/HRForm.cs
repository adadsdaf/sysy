using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class HRForm : Form
    {
        private readonly User _currentUser;
        private TabControl _tabs = null!;

        public HRForm(User user)
        {
            _currentUser = user;
            this.Text = "الموارد البشرية";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Arial", 11) };
            _tabs.TabPages.Add(BuildEmployeesTab());
            _tabs.TabPages.Add(BuildViolationsTab());
            _tabs.TabPages.Add(BuildSalariesTab());
            _tabs.TabPages.Add(BuildStatisticsTab());
            this.Controls.Add(_tabs);
        }

        // ── EMPLOYEES TAB ─────────────────────────────────────────────────
        private DataGridView _empGrid = null!;
        private TabPage BuildEmployeesTab()
        {
            var page = new TabPage("👥  الموظفون");
            _empGrid = MakeGrid();
            _empGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الاسم", DataPropertyName = "Name", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "المسمى الوظيفي", DataPropertyName = "Position", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "القسم", DataPropertyName = "Department", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "الراتب الأساسي", DataPropertyName = "BaseSalary", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "إجمالي الراتب", DataPropertyName = "TotalSalary", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "تاريخ التعيين", DataPropertyName = "HireDate", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } },
                new DataGridViewTextBoxColumn { HeaderText = "الجوال", DataPropertyName = "Phone", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "الحالة", DataPropertyName = "Status", Width = 80 }
            );

            var toolbar = MakeToolbar(
                ("+ موظف جديد", Color.FromArgb(34, 139, 34), (s, e) => AddEmployee()),
                ("تعديل", Color.FromArgb(30, 100, 200), (s, e) => EditEmployee()),
                ("إيقاف / تفعيل", Color.FromArgb(160, 100, 0), (s, e) => ToggleEmployee()),
                ("حذف", Color.FromArgb(200, 50, 50), (s, e) => DeleteEmployee()),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadEmployees())
            );
            LoadEmployees();
            page.Controls.AddRange(new Control[] { toolbar, _empGrid });
            toolbar.Dock = DockStyle.Top; _empGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadEmployees() => _empGrid.DataSource = EmployeeService.GetAll();

        private void AddEmployee()
        {
            using var dlg = new EmployeeEditDialog(new Employee { HireDate = DateTime.Today, Status = "active" });
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { EmployeeService.Create(dlg.Result); LoadEmployees(); }
        }

        private void EditEmployee()
        {
            if (_empGrid.CurrentRow?.DataBoundItem is not Employee emp) return;
            using var dlg = new EmployeeEditDialog(emp);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { EmployeeService.Update(dlg.Result); LoadEmployees(); }
        }

        private void ToggleEmployee()
        {
            if (_empGrid.CurrentRow?.DataBoundItem is not Employee emp) return;
            emp.Status = emp.Status == "active" ? "suspended" : "active";
            EmployeeService.Update(emp);
            LoadEmployees();
        }

        private void DeleteEmployee()
        {
            if (_empGrid.CurrentRow?.DataBoundItem is not Employee emp) return;
            if (MessageBox.Show($"حذف الموظف '{emp.Name}'؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { EmployeeService.Delete(emp.Id); LoadEmployees(); }
            catch (Exception ex) { MessageBox.Show($"لا يمكن الحذف: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── VIOLATIONS TAB ────────────────────────────────────────────────
        private DataGridView _violGrid = null!;
        private ComboBox _violEmpFilter = null!;
        private TabPage BuildViolationsTab()
        {
            var page = new TabPage("⚠  المخالفات");
            _violGrid = MakeGrid();
            _violGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الموظف", DataPropertyName = "EmployeeName", Width = 140 },
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", DataPropertyName = "ViolationDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } },
                new DataGridViewTextBoxColumn { HeaderText = "نوع المخالفة", DataPropertyName = "ViolationType", Width = 140 },
                new DataGridViewTextBoxColumn { HeaderText = "الوصف", DataPropertyName = "Description", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "الخصم", DataPropertyName = "Deduction", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الحالة", DataPropertyName = "Status", Width = 85 }
            );

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8, 8, 8, 4) };
            _violEmpFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 10), Dock = DockStyle.Right, Width = 200 };
            _violEmpFilter.Items.Add(new Employee { Id = 0, Name = "(جميع الموظفين)" });
            foreach (var e in EmployeeService.GetAll()) _violEmpFilter.Items.Add(e);
            _violEmpFilter.DisplayMember = "Name"; _violEmpFilter.ValueMember = "Id";
            _violEmpFilter.SelectedIndex = 0;
            _violEmpFilter.SelectedIndexChanged += (s, e) => LoadViolations();
            var filterLbl = new Label { Text = "الموظف:", Dock = DockStyle.Right, Width = 65, Font = new Font("Arial", 10), TextAlign = ContentAlignment.MiddleCenter };
            filterPanel.Controls.AddRange(new Control[] { _violEmpFilter, filterLbl });

            var toolbar = MakeToolbar(
                ("+ إضافة مخالفة", Color.FromArgb(200, 80, 0), (s, e) => AddViolation()),
                ("موافقة", Color.FromArgb(34, 120, 34), (s, e) => ApproveViolation()),
                ("رفض", Color.FromArgb(160, 50, 50), (s, e) => DismissViolation()),
                ("حذف", Color.FromArgb(200, 50, 50), (s, e) => DeleteViolation()),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadViolations())
            );
            LoadViolations();
            page.Controls.AddRange(new Control[] { toolbar, filterPanel, _violGrid });
            toolbar.Dock = DockStyle.Top; filterPanel.Dock = DockStyle.Top; _violGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadViolations()
        {
            int? empId = null;
            if (_violEmpFilter?.SelectedItem is Employee e && e.Id > 0) empId = e.Id;
            _violGrid.DataSource = EmployeeService.GetViolations(empId);
        }

        private void AddViolation()
        {
            var employees = EmployeeService.GetAll("active");
            if (!employees.Any()) { MessageBox.Show("لا يوجد موظفون", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using var dlg = new ViolationDialog(employees);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null) { EmployeeService.CreateViolation(dlg.Result); LoadViolations(); }
        }

        private void ApproveViolation()
        {
            if (_violGrid.CurrentRow?.DataBoundItem is not EmployeeViolation v) return;
            EmployeeService.UpdateViolationStatus(v.Id, "approved");
            LoadViolations();
        }

        private void DismissViolation()
        {
            if (_violGrid.CurrentRow?.DataBoundItem is not EmployeeViolation v) return;
            EmployeeService.UpdateViolationStatus(v.Id, "dismissed");
            LoadViolations();
        }

        private void DeleteViolation()
        {
            if (_violGrid.CurrentRow?.DataBoundItem is not EmployeeViolation v) return;
            if (MessageBox.Show("حذف هذه المخالفة؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            EmployeeService.DeleteViolation(v.Id);
            LoadViolations();
        }

        // ── SALARIES TAB ─────────────────────────────────────────────────
        private DataGridView _salGrid = null!;
        private NumericUpDown _salMonth = null!, _salYear = null!;
        private TabPage BuildSalariesTab()
        {
            var page = new TabPage("💰  كشف الرواتب");
            _salGrid = MakeGrid();
            _salGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الموظف", DataPropertyName = "EmployeeName", Width = 150 },
                new DataGridViewTextBoxColumn { HeaderText = "الراتب الأساسي", DataPropertyName = "BaseSalary", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "البدلات", DataPropertyName = "Allowances", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "مكافآت", DataPropertyName = "Bonuses", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "استقطاعات", DataPropertyName = "Deductions", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "خصم مخالفات", DataPropertyName = "ViolationDeductions", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الصافي", DataPropertyName = "NetSalary", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الحالة", DataPropertyName = "Status", Width = 80 }
            );

            var ctrlPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8, 8, 8, 4) };
            _salMonth = new NumericUpDown { Value = DateTime.Now.Month, Minimum = 1, Maximum = 12, Font = new Font("Arial", 10), Dock = DockStyle.Right, Width = 70 };
            _salYear = new NumericUpDown { Value = DateTime.Now.Year, Minimum = 2020, Maximum = 2099, Font = new Font("Arial", 10), Dock = DockStyle.Right, Width = 90 };
            _salMonth.ValueChanged += (s, e) => LoadSalaries();
            _salYear.ValueChanged += (s, e) => LoadSalaries();

            var btnGenerate = new Button { Text = "توليد كشف", Dock = DockStyle.Right, Width = 120, BackColor = Color.FromArgb(0, 120, 60), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += (s, e) => {
                EmployeeService.GenerateSalaries((int)_salMonth.Value, (int)_salYear.Value);
                LoadSalaries();
                MessageBox.Show("تم توليد كشف الرواتب بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var m1 = new Label { Text = "الشهر:", Dock = DockStyle.Right, Width = 55, Font = new Font("Arial", 10), TextAlign = ContentAlignment.MiddleCenter };
            var m2 = new Label { Text = "السنة:", Dock = DockStyle.Right, Width = 55, Font = new Font("Arial", 10), TextAlign = ContentAlignment.MiddleCenter };
            ctrlPanel.Controls.AddRange(new Control[] { btnGenerate, _salMonth, m1, _salYear, m2 });

            var toolbar = MakeToolbar(
                ("دفع الراتب", Color.FromArgb(34, 139, 34), (s, e) => PaySalary()),
                ("تعديل", Color.FromArgb(30, 100, 200), (s, e) => EditSalary()),
                ("طباعة الكشف", Color.FromArgb(80, 80, 160), (s, e) => PrintSalarySheet()),
                ("تحديث", Color.FromArgb(80, 100, 140), (s, e) => LoadSalaries())
            );
            LoadSalaries();
            page.Controls.AddRange(new Control[] { toolbar, ctrlPanel, _salGrid });
            toolbar.Dock = DockStyle.Top; ctrlPanel.Dock = DockStyle.Top; _salGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadSalaries() => _salGrid.DataSource = EmployeeService.GetSalaries((int)_salYear.Value, (int)_salMonth.Value);

        private void PaySalary()
        {
            if (_salGrid.CurrentRow?.DataBoundItem is not EmployeeSalary sal) return;
            if (sal.Status == "paid") { MessageBox.Show("تم دفع هذا الراتب مسبقاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show($"تأكيد دفع راتب {sal.EmployeeName} بمبلغ {sal.NetSalary:N0}؟", "تأكيد الدفع", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            EmployeeService.PaySalary(sal.Id);
            LoadSalaries();
            MessageBox.Show("تم تسجيل الدفع بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditSalary()
        {
            if (_salGrid.CurrentRow?.DataBoundItem is not EmployeeSalary sal) return;
            if (sal.Status == "paid") { MessageBox.Show("لا يمكن تعديل راتب مدفوع", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using var dlg = new SalaryEditDialog(sal);
            if (dlg.ShowDialog() == DialogResult.OK) { EmployeeService.UpdateSalary(dlg.Result); LoadSalaries(); }
        }

        private void PrintSalarySheet()
        {
            var salaries = EmployeeService.GetSalaries((int)_salYear.Value, (int)_salMonth.Value);
            if (!salaries.Any()) { MessageBox.Show("لا يوجد كشف رواتب لهذا الشهر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var sb = new System.Text.StringBuilder();
            int w = 80;
            sb.AppendLine(Center("كشف مرتبات الموظفين", w));
            sb.AppendLine(Center($"شهر {_salMonth.Value}/{_salYear.Value}", w));
            sb.AppendLine(new string('═', w));
            sb.AppendLine($"{"الموظف",-22} {"الأساسي",10} {"البدلات",10} {"مكافآت",8} {"خصومات",10} {"الصافي",12} {"الحالة",8}");
            sb.AppendLine(new string('─', w));
            decimal totalNet = 0;
            foreach (var s in salaries)
            {
                sb.AppendLine($"{s.EmployeeName,-22} {s.BaseSalary,10:N0} {s.Allowances,10:N0} {s.Bonuses,8:N0} {s.Deductions+s.ViolationDeductions,10:N0} {s.NetSalary,12:N0} {s.Status,8}");
                totalNet += s.NetSalary;
            }
            sb.AppendLine(new string('═', w));
            sb.AppendLine($"{"إجمالي الرواتب:",-50} {totalNet,12:N0}");
            ShowPrintPreview(sb.ToString(), "كشف الرواتب");
        }

        // ── STATISTICS TAB ─────────────────────────────────────────────────
        private TabPage BuildStatisticsTab()
        {
            var page = new TabPage("📊  إحصائيات");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };

            var refreshBtn = new Button { Text = "تحديث الإحصائيات", Location = new Point(20, 20), Size = new Size(180, 38),
                BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            refreshBtn.FlatAppearance.BorderSize = 0;
            var statsPanel = new Panel { Location = new Point(20, 70), Size = new Size(900, 400), BackColor = Color.Transparent };
            refreshBtn.Click += (s, e) => RefreshHRStats(statsPanel);
            panel.Controls.AddRange(new Control[] { refreshBtn, statsPanel });
            RefreshHRStats(statsPanel);
            page.Controls.Add(panel);
            return page;
        }

        private void RefreshHRStats(Panel statsPanel)
        {
            statsPanel.Controls.Clear();
            var employees = EmployeeService.GetAll();
            var active = employees.Count(e => e.Status == "active");
            var violations = EmployeeService.GetViolations();
            var pendingViol = violations.Count(v => v.Status == "pending");

            var cards = new[]
            {
                ("👥 إجمالي الموظفين", employees.Count.ToString(), Color.FromArgb(30, 100, 200)),
                ("✅ موظفون نشطون", active.ToString(), Color.FromArgb(34, 139, 34)),
                ("⚠ مخالفات معلقة", pendingViol.ToString(), Color.FromArgb(200, 120, 0)),
                ("💰 إجمالي الرواتب", $"{employees.Where(e=>e.Status=="active").Sum(e=>e.TotalSalary):N0}", Color.FromArgb(100, 50, 160))
            };

            int x = 0;
            foreach (var (title, val, color) in cards)
            {
                var card = new Panel { Location = new Point(x, 0), Size = new Size(200, 110), BackColor = color };
                card.Controls.Add(new Label { Text = title, ForeColor = Color.White, Font = new Font("Arial", 10), Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter });
                card.Controls.Add(new Label { Text = val, ForeColor = Color.White, Font = new Font("Arial", 24, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
                statsPanel.Controls.Add(card);
                x += 210;
            }
        }

        // ── HELPERS ─────────────────────────────────────────────────────
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
                btn.FlatAppearance.BorderSize = 0; btn.Click += click;
                flow.Controls.Add(btn);
            }
            panel.Controls.Add(flow); return panel;
        }

        private static string Center(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || text.Length >= width) return text;
            int pad = (width - text.Length) / 2;
            return new string(' ', pad) + text;
        }

        private static void ShowPrintPreview(string content, string title)
        {
            var dlg = new Form { Text = title, Size = new Size(700, 600), StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes };
            var rtb = new RichTextBox { Dock = DockStyle.Fill, Text = content, Font = new Font("Courier New", 9), ReadOnly = true };
            var btnPrint = new Button { Text = "طباعة", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) => {
                var pd = new System.Drawing.Printing.PrintDocument { DocumentName = title };
                pd.PrintPage += (ps, pe) => pe.Graphics!.DrawString(content, new Font("Courier New", 8), Brushes.Black, new System.Drawing.PointF(10, 10));
                var prd = new PrintDialog { Document = pd };
                if (prd.ShowDialog() == DialogResult.OK) pd.Print();
            };
            dlg.Controls.AddRange(new Control[] { rtb, btnPrint });
            dlg.ShowDialog();
        }
    }

    // ── EMPLOYEE EDIT DIALOG ──────────────────────────────────────────────
    public class EmployeeEditDialog : Form
    {
        public Employee? Result { get; private set; }
        private readonly Employee _orig;
        private TextBox _nameBox = null!, _nationalIdBox = null!, _phoneBox = null!, _posBox = null!, _deptBox = null!, _notesBox = null!;
        private NumericUpDown _baseBox = null!, _houBox = null!, _traBox = null!, _othBox = null!;
        private DateTimePicker _hireDtp = null!;
        private ComboBox _statusCombo = null!;

        public EmployeeEditDialog(Employee emp)
        {
            _orig = emp;
            this.Text = emp.Id == 0 ? "إضافة موظف" : "تعديل بيانات الموظف";
            this.Size = new Size(480, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };
            int y = 10;
            Label Lbl(string t) { var l = new Label { Text = t, Location = new Point(16, y), Size = new Size(430, 22), Font = new Font("Arial", 10) }; panel.Controls.Add(l); return l; }
            TextBox Txt(string v) { y += 24; var tb = new TextBox { Text = v, Location = new Point(16, y), Size = new Size(430, 28), Font = new Font("Arial", 10) }; panel.Controls.Add(tb); y += 34; return tb; }
            NumericUpDown Num(decimal v) { y += 24; var nb = new NumericUpDown { Value = v, Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 10), Maximum = 9999999, ThousandsSeparator = true }; panel.Controls.Add(nb); y += 34; return nb; }

            Lbl("الاسم الكامل: *"); _nameBox = Txt(emp.Name);
            Lbl("رقم الهوية الوطنية:"); _nationalIdBox = Txt(emp.NationalId ?? "");
            Lbl("الجوال:"); _phoneBox = Txt(emp.Phone ?? "");
            Lbl("المسمى الوظيفي:"); _posBox = Txt(emp.Position ?? "");
            Lbl("القسم:"); _deptBox = Txt(emp.Department ?? "");
            Lbl("الراتب الأساسي:"); _baseBox = Num(emp.BaseSalary);
            Lbl("بدل السكن:"); _houBox = Num(emp.HousingAllowance);
            Lbl("بدل المواصلات:"); _traBox = Num(emp.TransportAllowance);
            Lbl("بدلات أخرى:"); _othBox = Num(emp.OtherAllowances);

            Lbl("تاريخ التعيين:"); y += 24;
            _hireDtp = new DateTimePicker { Value = emp.HireDate == default ? DateTime.Today : emp.HireDate, Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 10) };
            panel.Controls.Add(_hireDtp); y += 34;

            Lbl("الحالة:"); y += 24;
            _statusCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 10) };
            _statusCombo.Items.AddRange(new object[] { "active", "suspended", "terminated" });
            _statusCombo.SelectedItem = emp.Status;
            panel.Controls.Add(_statusCombo); y += 34;

            Lbl("ملاحظات:"); _notesBox = Txt(emp.Notes ?? "");

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(16, 8, 16, 8) };
            var btnOk = new Button { Text = "حفظ", Dock = DockStyle.Right, Width = 130, BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => {
                if (string.IsNullOrEmpty(_nameBox.Text.Trim())) { MessageBox.Show("أدخل اسم الموظف", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Result = new Employee {
                    Id = _orig.Id, Name = _nameBox.Text.Trim(), NationalId = _nationalIdBox.Text.Trim().NullIfEmpty(),
                    Phone = _phoneBox.Text.Trim().NullIfEmpty(), Position = _posBox.Text.Trim().NullIfEmpty(),
                    Department = _deptBox.Text.Trim().NullIfEmpty(), BaseSalary = _baseBox.Value,
                    HousingAllowance = _houBox.Value, TransportAllowance = _traBox.Value, OtherAllowances = _othBox.Value,
                    HireDate = _hireDtp.Value.Date, Status = _statusCombo.SelectedItem?.ToString() ?? "active",
                    Notes = _notesBox.Text.Trim().NullIfEmpty()
                };
                DialogResult = DialogResult.OK; Close();
            };
            var btnCancel = new Button { Text = "إلغاء", Dock = DockStyle.Left, Width = 100, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.Controls.AddRange(new Control[] { panel, btnPanel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }

    // ── VIOLATION DIALOG ─────────────────────────────────────────────────
    public class ViolationDialog : Form
    {
        public EmployeeViolation? Result { get; private set; }
        public ViolationDialog(List<Employee> employees)
        {
            this.Text = "إضافة مخالفة";
            this.Size = new Size(440, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            int y = 15;
            Label Lbl(string t) { var l = new Label { Text = t, Location = new Point(16, y), Size = new Size(400, 22), Font = new Font("Arial", 10) }; this.Controls.Add(l); y += 24; return l; }

            Lbl("الموظف: *");
            var empCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, y), Size = new Size(400, 28), Font = new Font("Arial", 10) };
            empCombo.Items.AddRange(employees.ToArray()); empCombo.DisplayMember = "Name"; empCombo.ValueMember = "Id";
            if (employees.Any()) empCombo.SelectedIndex = 0;
            this.Controls.Add(empCombo); y += 38;

            Lbl("نوع المخالفة: *");
            var typeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, y), Size = new Size(400, 28), Font = new Font("Arial", 10) };
            typeBox.Items.AddRange(new object[] { "تأخر", "غياب بدون إذن", "إهمال في العمل", "سلوك غير لائق", "إتلاف ممتلكات", "أخرى" });
            typeBox.SelectedIndex = 0; this.Controls.Add(typeBox); y += 38;

            Lbl("الوصف:");
            var descBox = new TextBox { Location = new Point(16, y), Size = new Size(400, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(descBox); y += 38;

            Lbl("مبلغ الخصم:");
            var dedBox = new NumericUpDown { Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 10), Maximum = 999999, ThousandsSeparator = true };
            this.Controls.Add(dedBox); y += 38;

            Lbl("التاريخ:");
            var datePicker = new DateTimePicker { Value = DateTime.Today, Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(datePicker); y += 50;

            var btnOk = new Button { Text = "إضافة", Location = new Point(16, y), Size = new Size(130, 38), BackColor = Color.FromArgb(200, 80, 0), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => {
                if (empCombo.SelectedItem is not Employee emp) { MessageBox.Show("اختر موظفاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Result = new EmployeeViolation { EmployeeId = emp.Id, ViolationType = typeBox.Text, Description = descBox.Text, Deduction = dedBox.Value, ViolationDate = datePicker.Value.Date, Status = "pending" };
                DialogResult = DialogResult.OK; Close();
            };
            var btnCancel = new Button { Text = "إلغاء", Location = new Point(160, y), Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }

    // ── SALARY EDIT DIALOG ────────────────────────────────────────────────
    public class SalaryEditDialog : Form
    {
        public EmployeeSalary Result { get; private set; }
        public SalaryEditDialog(EmployeeSalary sal)
        {
            Result = sal;
            this.Text = $"تعديل راتب: {sal.EmployeeName}";
            this.Size = new Size(400, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;

            int y = 15;
            NumericUpDown Fld(string label, decimal val) {
                this.Controls.Add(new Label { Text = label, Location = new Point(16, y), Size = new Size(360, 22), Font = new Font("Arial", 10) }); y += 24;
                var nb = new NumericUpDown { Value = val, Location = new Point(16, y), Size = new Size(200, 28), Font = new Font("Arial", 10), Maximum = 9999999, ThousandsSeparator = true };
                this.Controls.Add(nb); y += 38; return nb;
            }
            var bsBox = Fld("الراتب الأساسي:", sal.BaseSalary);
            var alBox = Fld("البدلات:", sal.Allowances);
            var boBox = Fld("مكافآت:", sal.Bonuses);
            var deBox = Fld("استقطاعات:", sal.Deductions);
            var vdBox = Fld("خصم مخالفات:", sal.ViolationDeductions);

            var notesBox = new TextBox { PlaceholderText = "ملاحظات...", Location = new Point(16, y), Size = new Size(360, 28), Font = new Font("Arial", 10) };
            this.Controls.Add(notesBox); y += 50;

            var btnOk = new Button { Text = "حفظ", Location = new Point(16, y), Size = new Size(130, 38), BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => {
                Result.BaseSalary = bsBox.Value; Result.Allowances = alBox.Value; Result.Bonuses = boBox.Value;
                Result.Deductions = deBox.Value; Result.ViolationDeductions = vdBox.Value;
                Result.Notes = notesBox.Text.Trim();
                DialogResult = DialogResult.OK; Close();
            };
            var btnCancel = new Button { Text = "إلغاء", Location = new Point(160, y), Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }

}
