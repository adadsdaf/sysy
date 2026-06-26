using POSSystem.Models;

namespace POSSystem.Forms
{
    public partial class PaymentForm : Form
    {
        private readonly Invoice _invoice;
        private readonly string _symbol;
        public string SelectedMethod { get; private set; } = "cash";
        public decimal AmountPaid { get; private set; }

        public PaymentForm(Invoice invoice, string currencySymbol)
        {
            _invoice = invoice;
            _symbol = currencySymbol;
            InitializeComponent();
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            UpdateDisplay();
        }

        private ComboBox _methodCombo = null!;
        private NumericUpDown _amountInput = null!;
        private Label _totalLbl = null!, _changeLbl = null!;

        private void InitializeComponent()
        {
            this.Text = "إتمام الدفع";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 242, 245);

            var y = 20;
            AddLabel($"رقم الفاتورة: {_invoice.InvoiceNumber}", 20, ref y, 14, FontStyle.Bold);
            y += 5;
            _totalLbl = AddLabel("", 20, ref y, 16, FontStyle.Bold);
            _totalLbl.ForeColor = Color.FromArgb(220, 50, 50);
            y += 10;

            AddLabel("طريقة الدفع:", 20, ref y, 12);
            _methodCombo = new ComboBox { Location = new Point(20, y), Size = new Size(390, 30),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 12) };
            _methodCombo.Items.AddRange(new object[] { "نقدي", "شبكة", "بطاقة", "تحويل", "محفظة إلكترونية", "دفع مختلط" });
            _methodCombo.SelectedIndex = 0;
            this.Controls.Add(_methodCombo);
            y += 45;

            AddLabel("المبلغ المدفوع:", 20, ref y, 12);
            _amountInput = new NumericUpDown { Location = new Point(20, y), Size = new Size(390, 35),
                Font = new Font("Arial", 14), DecimalPlaces = 2, Minimum = 0, Maximum = 9999999, ThousandsSeparator = true };
            _amountInput.ValueChanged += (s, e) => UpdateChange();
            this.Controls.Add(_amountInput);
            y += 50;

            _changeLbl = AddLabel("", 20, ref y, 13, FontStyle.Bold);
            _changeLbl.ForeColor = Color.FromArgb(0, 150, 0);
            y += 15;

            var btnOk = new Button { Text = "تأكيد الدفع", Location = new Point(20, y), Size = new Size(180, 45),
                BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White,
                Font = new Font("Arial", 13, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button { Text = "إلغاء", Location = new Point(230, y), Size = new Size(180, 45),
                BackColor = Color.FromArgb(180, 50, 50), ForeColor = Color.White,
                Font = new Font("Arial", 13, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
        }

        private Label AddLabel(string text, int x, ref int y, int fontSize, FontStyle style = FontStyle.Regular)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), Size = new Size(400, fontSize + 12),
                Font = new Font("Arial", fontSize, style), AutoSize = false };
            this.Controls.Add(lbl);
            y += fontSize + 18;
            return lbl;
        }

        private void UpdateDisplay()
        {
            _totalLbl.Text = $"الإجمالي: {_invoice.Total:N2} {_symbol}";
            _amountInput.Value = _invoice.Total;
            UpdateChange();
        }

        private void UpdateChange()
        {
            var change = (decimal)_amountInput.Value - _invoice.Total;
            _changeLbl.Text = change >= 0
                ? $"الباقي للعميل: {change:N2} {_symbol}"
                : $"مبلغ غير كافٍ! ناقص: {Math.Abs(change):N2} {_symbol}";
            _changeLbl.ForeColor = change >= 0 ? Color.FromArgb(0, 150, 0) : Color.Red;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            AmountPaid = (decimal)_amountInput.Value;
            if (AmountPaid < _invoice.Total)
            {
                MessageBox.Show("المبلغ المدفوع أقل من الإجمالي!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var methods = new[] { "cash", "network", "card", "transfer", "ewallet", "mixed" };
            SelectedMethod = methods[_methodCombo.SelectedIndex];
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
