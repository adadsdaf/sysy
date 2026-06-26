using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Forms
{
    public class ReportsForm : Form
    {
        private readonly User _currentUser;
        private TabControl _tabs = null!;

        public ReportsForm(User user)
        {
            _currentUser = user;
            this.Text = "التقارير والإحصائيات";
            this.Size = new Size(1150, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 252);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Arial", 11) };
            _tabs.TabPages.Add(BuildDashboardTab());
            _tabs.TabPages.Add(BuildSalesTab());
            _tabs.TabPages.Add(BuildItemsTab());
            _tabs.TabPages.Add(BuildCashierTab());
            _tabs.TabPages.Add(BuildDailyTab());
            this.Controls.Add(_tabs);
        }

        // ── DASHBOARD ──────────────────────────────────────────────────────
        private Panel _dashCards = null!;
        private TabPage BuildDashboardTab()
        {
            var page = new TabPage("📊  لوحة الإحصائيات");
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            var refreshBtn = new Button { Text = "🔄 تحديث", Location = new Point(20, 20), Size = new Size(150, 40),
                BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            refreshBtn.FlatAppearance.BorderSize = 0;
            _dashCards = new Panel { Location = new Point(20, 76), Size = new Size(1080, 400), BackColor = Color.Transparent };
            refreshBtn.Click += (s, e) => RefreshDashboard();
            panel.Controls.AddRange(new Control[] { refreshBtn, _dashCards });
            RefreshDashboard();
            page.Controls.Add(panel);
            return page;
        }

        private void RefreshDashboard()
        {
            _dashCards.Controls.Clear();

            var today = DateTime.Today;
            var todaySales = GetSalesTotal(today, today);
            var monthSales = GetSalesTotal(new DateTime(today.Year, today.Month, 1), today);
            var yearSales = GetSalesTotal(new DateTime(today.Year, 1, 1), today);
            var totalInvoices = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Invoices WHERE Status='paid'"));
            var totalCustomers = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Customers"));
            var totalItems = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Items WHERE IsAvailable=1"));

            var bigCards = new (string title, string val, Color color)[]
            {
                ("💰 مبيعات اليوم", $"{todaySales:N0}", Color.FromArgb(34, 120, 34)),
                ("📅 مبيعات الشهر", $"{monthSales:N0}", Color.FromArgb(30, 100, 200)),
                ("📆 مبيعات السنة", $"{yearSales:N0}", Color.FromArgb(100, 50, 160)),
            };
            int x = 0;
            foreach (var (t, v, c) in bigCards)
            {
                var card = MakeCard(t, v, c, 330, 130);
                card.Location = new Point(x, 0);
                _dashCards.Controls.Add(card);
                x += 342;
            }

            var smallCards = new (string title, string val, Color color)[]
            {
                ("🧾 إجمالي الفواتير", totalInvoices.ToString(), Color.FromArgb(0, 140, 150)),
                ("👤 العملاء", totalCustomers.ToString(), Color.FromArgb(60, 100, 160)),
                ("🛒 الأصناف النشطة", totalItems.ToString(), Color.FromArgb(150, 80, 0)),
            };
            x = 0;
            foreach (var (t, v, c) in smallCards)
            {
                var card = MakeCard(t, v, c, 200, 100);
                card.Location = new Point(x, 146);
                _dashCards.Controls.Add(card);
                x += 214;
            }

            // Top 5 items today
            var topDt = DatabaseHelper.ExecuteQuery(@"
SELECT TOP 5 ii.Name, SUM(ii.Quantity) AS TotalQty, SUM(ii.Quantity*ii.Price) AS TotalRev
FROM InvoiceItems ii JOIN Invoices inv ON ii.InvoiceId=inv.Id
WHERE inv.Status='paid' AND CAST(inv.PaidAt AS DATE)=CAST(GETDATE() AS DATE)
GROUP BY ii.Name ORDER BY TotalRev DESC");
            if (topDt.Rows.Count > 0)
            {
                var topLbl = new Label { Text = "🏆 أفضل الأصناف مبيعاً اليوم:", Location = new Point(0, 264), Size = new Size(500, 24), Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 140) };
                _dashCards.Controls.Add(topLbl);
                int ty = 294;
                foreach (DataRow r in topDt.Rows)
                {
                    var lbl = new Label { Text = $"  ● {r["Name"]}  —  {Convert.ToDecimal(r["TotalQty"]):N0} وحدة  —  {Convert.ToDecimal(r["TotalRev"]):N0} ر.س", Location = new Point(0, ty), Size = new Size(600, 22), Font = new Font("Arial", 10) };
                    _dashCards.Controls.Add(lbl); ty += 24;
                }
            }
        }

        private static Panel MakeCard(string title, string val, Color color, int w, int h)
        {
            var card = new Panel { Size = new Size(w, h), BackColor = color };
            card.Controls.Add(new Label { Text = title, ForeColor = Color.White, Font = new Font("Arial", 10), Dock = DockStyle.Top, Height = h / 3, TextAlign = ContentAlignment.MiddleCenter });
            card.Controls.Add(new Label { Text = val, ForeColor = Color.White, Font = new Font("Arial", (int)(h * 0.22f), FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            return card;
        }

        private static decimal GetSalesTotal(DateTime from, DateTime to)
        {
            var dt = DatabaseHelper.ExecuteQuery(@"
SELECT ISNULL(SUM(ii.Quantity*ii.Price), 0) AS Total
FROM InvoiceItems ii JOIN Invoices inv ON ii.InvoiceId=inv.Id
WHERE inv.Status='paid' AND inv.PaidAt>=@Fr AND inv.PaidAt<=@To",
                new Dictionary<string, object?> { ["@Fr"] = from.Date, ["@To"] = to.Date.AddDays(1) });
            return (decimal)dt.Rows[0]["Total"];
        }

        // ── SALES REPORT ──────────────────────────────────────────────────
        private DataGridView _salesGrid = null!;
        private DateTimePicker _salesFrom = null!, _salesTo = null!;
        private Label _salesTotalLbl = null!;
        private TabPage BuildSalesTab()
        {
            var page = new TabPage("📈  تقرير المبيعات");
            _salesGrid = MakeGrid();
            _salesGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "التاريخ", Name = "Date", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "عدد الفواتير", Name = "Count", Width = 100 },
                new DataGridViewTextBoxColumn { HeaderText = "إجمالي المبيعات", Name = "Gross", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الضريبة", Name = "Tax", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "الخصومات", Name = "Disc", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "صافي المبيعات", Name = "Net", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } }
            );

            var ctrlPanel = BuildFilterPanel(ref _salesFrom, ref _salesTo, LoadSalesReport);
            _salesTotalLbl = new Label { Dock = DockStyle.Bottom, Height = 40, Font = new Font("Arial", 13, FontStyle.Bold), ForeColor = Color.FromArgb(30, 100, 200), TextAlign = ContentAlignment.MiddleRight, Text = "" };
            var printBtn = new Button { Text = "🖨 طباعة", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(80, 80, 160), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            printBtn.FlatAppearance.BorderSize = 0; printBtn.Click += (s, e) => PrintSalesReport();
            LoadSalesReport();
            page.Controls.AddRange(new Control[] { ctrlPanel, _salesGrid, _salesTotalLbl, printBtn });
            ctrlPanel.Dock = DockStyle.Top; _salesGrid.Dock = DockStyle.Fill; _salesTotalLbl.Dock = DockStyle.Bottom; printBtn.Dock = DockStyle.Bottom;
            return page;
        }

        private void LoadSalesReport()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"
SELECT
    CAST(inv.PaidAt AS DATE) AS SaleDate,
    COUNT(*) AS InvoiceCount,
    ISNULL(SUM(ii.Quantity*ii.Price),0) AS GrossSales,
    ISNULL(SUM(ii.Quantity*ii.Price*ii.TaxRate/100),0) AS TaxAmount,
    ISNULL(SUM(inv.DiscountAmount),0) AS DiscountAmount
FROM Invoices inv
JOIN InvoiceItems ii ON inv.Id=ii.InvoiceId
WHERE inv.Status='paid' AND inv.PaidAt>=@Fr AND inv.PaidAt<=@To
GROUP BY CAST(inv.PaidAt AS DATE)
ORDER BY SaleDate DESC",
                new Dictionary<string, object?> { ["@Fr"] = _salesFrom.Value.Date, ["@To"] = _salesTo.Value.Date.AddDays(1) });

            _salesGrid.Rows.Clear();
            decimal totalNet = 0;
            foreach (DataRow r in dt.Rows)
            {
                var gross = Convert.ToDecimal(r["GrossSales"]);
                var tax = Convert.ToDecimal(r["TaxAmount"]);
                var disc = Convert.ToDecimal(r["DiscountAmount"]);
                var net = gross - disc;
                totalNet += net;
                _salesGrid.Rows.Add(((DateTime)r["SaleDate"]).ToString("dd/MM/yyyy"), Convert.ToInt32(r["InvoiceCount"]), gross, tax, disc, net);
            }
            _salesTotalLbl.Text = $"  إجمالي صافي المبيعات: {totalNet:N0} ر.س  |  عدد الأيام: {dt.Rows.Count}";
        }

        private void PrintSalesReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═════════════════════════════════════════════════════");
            sb.AppendLine($"              تقرير المبيعات");
            sb.AppendLine($"    من: {_salesFrom.Value:dd/MM/yyyy}   إلى: {_salesTo.Value:dd/MM/yyyy}");
            sb.AppendLine("═════════════════════════════════════════════════════");
            sb.AppendLine($"{"التاريخ",-12} {"الفواتير",8} {"الإجمالي",14} {"الخصم",10} {"الصافي",14}");
            sb.AppendLine(new string('─', 65));
            decimal totalNet = 0;
            foreach (DataGridViewRow row in _salesGrid.Rows)
            {
                if (row.IsNewRow) continue;
                sb.AppendLine($"{row.Cells["Date"].Value,-12} {row.Cells["Count"].Value,8} {row.Cells["Gross"].Value,14} {row.Cells["Disc"].Value,10} {row.Cells["Net"].Value,14}");
                decimal.TryParse(row.Cells["Net"].Value?.ToString()?.Replace(",", ""), out decimal net);
                totalNet += net;
            }
            sb.AppendLine(new string('═', 65));
            sb.AppendLine($"{"الإجمالي الكلي:",-40} {totalNet,14:N0}");
            ShowTextPreview(sb.ToString(), "تقرير المبيعات");
        }

        // ── ITEMS REPORT ──────────────────────────────────────────────────
        private DataGridView _itemsGrid = null!;
        private DateTimePicker _itemsFrom = null!, _itemsTo = null!;
        private TabPage BuildItemsTab()
        {
            var page = new TabPage("🛒  مبيعات الأصناف");
            _itemsGrid = MakeGrid();
            _itemsGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الرقم", Name = "Num", Width = 70 },
                new DataGridViewTextBoxColumn { HeaderText = "الصنف", Name = "Name", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "القسم", Name = "Cat", Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "الكمية المباعة", Name = "Qty", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "إجمالي الإيرادات", Name = "Rev", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } }
            );
            var ctrlPanel = BuildFilterPanel(ref _itemsFrom, ref _itemsTo, LoadItemsReport);
            var printBtn = new Button { Text = "🖨 طباعة", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(80, 80, 160), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            printBtn.FlatAppearance.BorderSize = 0; printBtn.Click += (s, e) => PrintItemsReport();
            LoadItemsReport();
            page.Controls.AddRange(new Control[] { ctrlPanel, _itemsGrid, printBtn });
            ctrlPanel.Dock = DockStyle.Top; _itemsGrid.Dock = DockStyle.Fill; printBtn.Dock = DockStyle.Bottom;
            return page;
        }

        private void LoadItemsReport()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"
SELECT ii.ItemNumber, ii.Name, ISNULL(ii.CategoryName,N'—') AS CategoryName,
    SUM(ii.Quantity) AS TotalQty, SUM(ii.Quantity*ii.Price) AS TotalRev
FROM InvoiceItems ii
JOIN Invoices inv ON ii.InvoiceId=inv.Id
WHERE inv.Status='paid' AND inv.PaidAt>=@Fr AND inv.PaidAt<=@To
GROUP BY ii.ItemNumber, ii.Name, ii.CategoryName
ORDER BY TotalRev DESC",
                new Dictionary<string, object?> { ["@Fr"] = _itemsFrom.Value.Date, ["@To"] = _itemsTo.Value.Date.AddDays(1) });
            _itemsGrid.Rows.Clear();
            foreach (DataRow r in dt.Rows)
                _itemsGrid.Rows.Add(r["ItemNumber"] == DBNull.Value ? "" : r["ItemNumber"], r["Name"], r["CategoryName"], Convert.ToDecimal(r["TotalQty"]), Convert.ToDecimal(r["TotalRev"]));
        }

        private void PrintItemsReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"تقرير مبيعات الأصناف  —  {_itemsFrom.Value:dd/MM/yyyy} إلى {_itemsTo.Value:dd/MM/yyyy}");
            sb.AppendLine(new string('═', 70));
            sb.AppendLine($"{"الصنف",-28} {"القسم",-16} {"الكمية",10} {"الإيرادات",14}");
            sb.AppendLine(new string('─', 70));
            foreach (DataGridViewRow row in _itemsGrid.Rows)
            {
                if (row.IsNewRow) continue;
                sb.AppendLine($"{row.Cells["Name"].Value,-28} {row.Cells["Cat"].Value,-16} {row.Cells["Qty"].Value,10} {row.Cells["Rev"].Value,14}");
            }
            ShowTextPreview(sb.ToString(), "تقرير الأصناف");
        }

        // ── CASHIER REPORT ────────────────────────────────────────────────
        private DataGridView _cashGrid = null!;
        private DateTimePicker _cashFrom = null!, _cashTo = null!;
        private TabPage BuildCashierTab()
        {
            var page = new TabPage("👤  أداء الكاشيرية");
            _cashGrid = MakeGrid();
            _cashGrid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "الكاشير", Name = "Cashier", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "عدد الفواتير", Name = "Count", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "إجمالي المبيعات", Name = "Total", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } },
                new DataGridViewTextBoxColumn { HeaderText = "متوسط الفاتورة", Name = "Avg", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } }
            );
            var ctrlPanel = BuildFilterPanel(ref _cashFrom, ref _cashTo, LoadCashierReport);
            LoadCashierReport();
            page.Controls.AddRange(new Control[] { ctrlPanel, _cashGrid });
            ctrlPanel.Dock = DockStyle.Top; _cashGrid.Dock = DockStyle.Fill;
            return page;
        }

        private void LoadCashierReport()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"
SELECT u.Name AS CashierName, COUNT(inv.Id) AS InvoiceCount,
    ISNULL(SUM(ii.Quantity*ii.Price),0) AS TotalSales
FROM Invoices inv
JOIN Users u ON inv.CashierId=u.Id
JOIN InvoiceItems ii ON inv.Id=ii.InvoiceId
WHERE inv.Status='paid' AND inv.PaidAt>=@Fr AND inv.PaidAt<=@To
GROUP BY u.Name ORDER BY TotalSales DESC",
                new Dictionary<string, object?> { ["@Fr"] = _cashFrom.Value.Date, ["@To"] = _cashTo.Value.Date.AddDays(1) });
            _cashGrid.Rows.Clear();
            foreach (DataRow r in dt.Rows)
            {
                var total = Convert.ToDecimal(r["TotalSales"]);
                var count = Convert.ToInt32(r["InvoiceCount"]);
                _cashGrid.Rows.Add(r["CashierName"], count, total, count > 0 ? total / count : 0);
            }
        }

        // ── DAILY REPORT (جرد اليوم) ──────────────────────────────────────
        private TabPage BuildDailyTab()
        {
            var page = new TabPage("📃  جرد اليوم");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var datePicker = new DateTimePicker { Value = DateTime.Today, Location = new Point(20, 20), Size = new Size(180, 30), Font = new Font("Arial", 11) };
            var btnLoad = new Button { Text = "عرض التقرير", Location = new Point(210, 18), Size = new Size(150, 34),
                BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnLoad.FlatAppearance.BorderSize = 0;
            var btnPrint = new Button { Text = "🖨 طباعة", Location = new Point(370, 18), Size = new Size(120, 34),
                BackColor = Color.FromArgb(80, 80, 160), ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnPrint.FlatAppearance.BorderSize = 0;

            var rtb = new RichTextBox { Location = new Point(20, 60), Size = new Size(1060, 560), Font = new Font("Courier New", 9), ReadOnly = true, BackColor = Color.White };

            btnLoad.Click += (s, e) => { rtb.Text = GenerateDailyReport(datePicker.Value.Date); };
            btnPrint.Click += (s, e) =>
            {
                var content = rtb.Text;
                var pd = new System.Drawing.Printing.PrintDocument { DocumentName = "جرد اليوم" };
                pd.PrintPage += (ps, pe) => pe.Graphics!.DrawString(content, new Font("Courier New", 8), Brushes.Black, new System.Drawing.PointF(10, 10));
                var prd = new PrintDialog { Document = pd };
                if (prd.ShowDialog() == DialogResult.OK) pd.Print();
            };
            rtb.Text = GenerateDailyReport(DateTime.Today);
            panel.Controls.AddRange(new Control[] { datePicker, btnLoad, btnPrint, rtb });
            page.Controls.Add(panel);
            return page;
        }

        private static string GenerateDailyReport(DateTime date)
        {
            var sb = new System.Text.StringBuilder();
            int w = 72;
            sb.AppendLine(new string('═', w));
            sb.AppendLine(Center("جرد اليوم", w));
            sb.AppendLine(Center(date.ToString("dd/MM/yyyy"), w));
            sb.AppendLine(new string('═', w));

            var invDt = DatabaseHelper.ExecuteQuery(@"
SELECT COUNT(*) AS Cnt, ISNULL(SUM(ii.Quantity*ii.Price),0) AS Gross,
    ISNULL(SUM(ii.Quantity*ii.Price*ii.TaxRate/100),0) AS Tax,
    ISNULL(SUM(inv.DiscountAmount),0) AS Disc
FROM Invoices inv JOIN InvoiceItems ii ON inv.Id=ii.InvoiceId
WHERE inv.Status='paid' AND CAST(inv.PaidAt AS DATE)=@D",
                new Dictionary<string, object?> { ["@D"] = date.Date });
            var r = invDt.Rows[0];
            sb.AppendLine($"  عدد الفواتير:     {Convert.ToInt32(r["Cnt"])}");
            sb.AppendLine($"  إجمالي المبيعات:  {Convert.ToDecimal(r["Gross"]):N0}");
            sb.AppendLine($"  الضريبة المحصلة:  {Convert.ToDecimal(r["Tax"]):N0}");
            sb.AppendLine($"  إجمالي الخصومات:  {Convert.ToDecimal(r["Disc"]):N0}");
            sb.AppendLine($"  صافي المبيعات:    {(Convert.ToDecimal(r["Gross"]) - Convert.ToDecimal(r["Disc"])):N0}");
            sb.AppendLine(new string('─', w));

            var catDt = DatabaseHelper.ExecuteQuery(@"
SELECT ISNULL(ii.CategoryName,N'غير محدد') AS Cat, SUM(ii.Quantity*ii.Price) AS Rev
FROM InvoiceItems ii JOIN Invoices inv ON ii.InvoiceId=inv.Id
WHERE inv.Status='paid' AND CAST(inv.PaidAt AS DATE)=@D
GROUP BY ii.CategoryName ORDER BY Rev DESC",
                new Dictionary<string, object?> { ["@D"] = date.Date });
            if (catDt.Rows.Count > 0)
            {
                sb.AppendLine("  المبيعات حسب القسم:");
                foreach (DataRow cr in catDt.Rows)
                    sb.AppendLine($"    {cr["Cat"],-22} {Convert.ToDecimal(cr["Rev"]):N0}");
                sb.AppendLine(new string('─', w));
            }

            var payDt = DatabaseHelper.ExecuteQuery(@"
SELECT ISNULL(PaymentMethod,'غير محدد') AS PM, COUNT(*) AS Cnt, SUM(AmountPaid) AS Total
FROM Invoices WHERE Status='paid' AND CAST(PaidAt AS DATE)=@D
GROUP BY PaymentMethod",
                new Dictionary<string, object?> { ["@D"] = date.Date });
            if (payDt.Rows.Count > 0)
            {
                sb.AppendLine("  المبيعات حسب وسيلة الدفع:");
                foreach (DataRow pr in payDt.Rows)
                    sb.AppendLine($"    {pr["PM"],-22} {Convert.ToInt32(pr["Cnt"])} فاتورة   {Convert.ToDecimal(pr["Total"]):N0}");
                sb.AppendLine(new string('─', w));
            }

            var voucherDt = DatabaseHelper.ExecuteQuery(@"
SELECT VoucherType, COUNT(*) AS Cnt, SUM(Amount) AS Total
FROM CashVouchers WHERE CAST(VoucherDate AS DATE)=@D
GROUP BY VoucherType",
                new Dictionary<string, object?> { ["@D"] = date.Date });
            if (voucherDt.Rows.Count > 0)
            {
                sb.AppendLine("  حركات الصناديق:");
                foreach (DataRow vr in voucherDt.Rows)
                    sb.AppendLine($"    {(vr["VoucherType"].ToString()=="payment"?"صرف":"قبض"),-22} {Convert.ToInt32(vr["Cnt"])} سند   {Convert.ToDecimal(vr["Total"]):N0}");
                sb.AppendLine(new string('─', w));
            }

            sb.AppendLine(new string('═', w));
            sb.AppendLine($"  وقت الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            return sb.ToString();
        }

        // ── HELPERS ──────────────────────────────────────────────────────
        private Panel BuildFilterPanel(ref DateTimePicker from, ref DateTimePicker to, Action load)
        {
            var panel = new Panel { Height = 50, BackColor = Color.FromArgb(240, 244, 255), Padding = new Padding(8) };
            from = new DateTimePicker { Value = DateTime.Today.AddDays(-30), Dock = DockStyle.Right, Width = 160, Font = new Font("Arial", 9) };
            to = new DateTimePicker { Value = DateTime.Today, Dock = DockStyle.Right, Width = 160, Font = new Font("Arial", 9) };
            var fromRef = from; var toRef = to;
            var btn = new Button { Text = "بحث", Dock = DockStyle.Right, Width = 80, BackColor = Color.FromArgb(30, 100, 200), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btn.FlatAppearance.BorderSize = 0; btn.Click += (s, e) => load();
            panel.Controls.AddRange(new Control[] { btn, toRef, new Label { Text = "إلى:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter }, fromRef, new Label { Text = "من:", Dock = DockStyle.Right, Width = 35, TextAlign = ContentAlignment.MiddleCenter } });
            return panel;
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

        private static string Center(string text, int w)
        {
            if (string.IsNullOrEmpty(text) || text.Length >= w) return text;
            int p = (w - text.Length) / 2;
            return new string(' ', p) + text;
        }

        private static void ShowTextPreview(string content, string title)
        {
            var dlg = new Form { Text = title, Size = new Size(720, 580), StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes };
            var rtb = new RichTextBox { Dock = DockStyle.Fill, Text = content, Font = new Font("Courier New", 9), ReadOnly = true };
            var btnPrint = new Button { Text = "🖨 طباعة", Dock = DockStyle.Bottom, Height = 42, BackColor = Color.FromArgb(80, 80, 160), ForeColor = Color.White, Font = new Font("Arial", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) =>
            {
                var pd = new System.Drawing.Printing.PrintDocument { DocumentName = title };
                pd.PrintPage += (ps, pe) => pe.Graphics!.DrawString(content, new Font("Courier New", 8), Brushes.Black, new System.Drawing.PointF(10, 10));
                var prd = new PrintDialog { Document = pd };
                if (prd.ShowDialog() == DialogResult.OK) pd.Print();
            };
            dlg.Controls.AddRange(new Control[] { rtb, btnPrint }); dlg.ShowDialog();
        }
    }
}
