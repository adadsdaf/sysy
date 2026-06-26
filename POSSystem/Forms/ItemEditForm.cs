using System.Drawing;
using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public class ItemEditForm : Form
    {
        private readonly Item? _item;
        private readonly List<Category> _categories;
        public Item? ResultItem { get; private set; }

        private TextBox _nameBox = null!, _nameArBox = null!, _barcodeBox = null!, _itemNumBox = null!, _notesBox = null!;
        private NumericUpDown _priceInput = null!, _taxInput = null!, _discInput = null!, _stockInput = null!;
        private ComboBox _categoryCombo = null!;
        private CheckBox _favCheck = null!, _availCheck = null!;

        public ItemEditForm(Item? item = null)
        {
            _item = item;
            _categories = CategoryService.GetAll();
            InitializeComponent();
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            if (_item != null) LoadItem();
        }

        private void InitializeComponent()
        {
            this.Text = _item == null ? "إضافة صنف جديد" : "تعديل الصنف";
            this.Size = new Size(520, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 10,
                Padding = new Padding(15), AutoSize = true };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(panel, 0, "الاسم *", _nameBox = new TextBox());
            AddRow(panel, 1, "الاسم بالعربي", _nameArBox = new TextBox());
            AddRow(panel, 2, "الباركود", _barcodeBox = new TextBox());
            AddRow(panel, 3, "رقم الصنف", _itemNumBox = new TextBox());
            AddRow(panel, 4, "السعر *", _priceInput = MakeNumeric(9999999, 2));
            AddRow(panel, 5, "نسبة الضريبة %", _taxInput = MakeNumeric(100, 2));
            AddRow(panel, 6, "نسبة الخصم %", _discInput = MakeNumeric(100, 2));
            AddRow(panel, 7, "الفئة", _categoryCombo = MakeCombo());
            AddRow(panel, 8, "المخزون", _stockInput = MakeNumeric(999999, 0));

            _notesBox = new TextBox { Multiline = true, Height = 55 };
            AddRow(panel, 9, "ملاحظات", _notesBox);

            _favCheck = new CheckBox { Text = "مفضل", Font = new Font("Arial", 11) };
            _availCheck = new CheckBox { Text = "متاح للبيع", Font = new Font("Arial", 11), Checked = true };
            var chkPanel = new FlowLayoutPanel { AutoSize = true };
            chkPanel.Controls.AddRange(new Control[] { _availCheck, _favCheck });
            panel.Controls.Add(new Label { Text = "خيارات", Font = new Font("Arial", 10), TextAlign = ContentAlignment.MiddleRight });
            panel.Controls.Add(chkPanel);

            var btnSave = new Button { Text = "حفظ", DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(34,139,34), ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(120,120,120), ForeColor = Color.White,
                Font = new Font("Arial", 12), FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 55, FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 8, 10, 8), BackColor = Color.FromArgb(230, 232, 235) };
            btnSave.Size = btnCancel.Size = new Size(130, 38);
            btnPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });

            this.Controls.AddRange(new Control[] { panel, btnPanel });
        }

        private void AddRow(TableLayoutPanel panel, int row, string label, Control ctrl)
        {
            panel.Controls.Add(new Label { Text = label, Font = new Font("Arial", 10), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
            ctrl.Font = new Font("Arial", 11); ctrl.Dock = DockStyle.Fill;
            panel.Controls.Add(ctrl, 1, row);
        }

        private NumericUpDown MakeNumeric(decimal max, int decimals) =>
            new NumericUpDown { DecimalPlaces = decimals, Maximum = max, ThousandsSeparator = true };

        private ComboBox MakeCombo()
        {
            var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            combo.Items.Add(new { Name = "(بدون فئة)", Id = 0 });
            foreach (var c in _categories) combo.Items.Add(c);
            combo.DisplayMember = "Name"; combo.ValueMember = "Id";
            combo.SelectedIndex = 0;
            return combo;
        }

        private void LoadItem()
        {
            _nameBox.Text = _item!.Name;
            _nameArBox.Text = _item.NameAr ?? "";
            _barcodeBox.Text = _item.Barcode ?? "";
            _itemNumBox.Text = _item.ItemNumber ?? "";
            _priceInput.Value = _item.Price;
            _taxInput.Value = _item.TaxRate;
            _discInput.Value = _item.DiscountRate;
            _stockInput.Value = _item.Stock ?? 0;
            _favCheck.Checked = _item.IsFavorite;
            _availCheck.Checked = _item.IsAvailable;
            _notesBox.Text = _item.Notes ?? "";
            if (_item.CategoryId.HasValue)
                for (int i = 0; i < _categoryCombo.Items.Count; i++)
                    if (_categoryCombo.Items[i] is Category c && c.Id == _item.CategoryId) { _categoryCombo.SelectedIndex = i; break; }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameBox.Text)) { MessageBox.Show("أدخل اسم الصنف", "تحقق", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
            int? catId = null;
            if (_categoryCombo.SelectedItem is Category cat) catId = cat.Id;

            ResultItem = new Item
            {
                Id = _item?.Id ?? 0, Name = _nameBox.Text.Trim(), NameAr = _nameArBox.Text.Trim().NullIfEmpty(),
                Barcode = _barcodeBox.Text.Trim().NullIfEmpty(), ItemNumber = _itemNumBox.Text.Trim().NullIfEmpty(),
                Price = _priceInput.Value, TaxRate = _taxInput.Value, DiscountRate = _discInput.Value,
                CategoryId = catId, Stock = (int)_stockInput.Value == 0 ? null : (int?)_stockInput.Value,
                IsFavorite = _favCheck.Checked, IsAvailable = _availCheck.Checked, Notes = _notesBox.Text.Trim().NullIfEmpty()
            };
        }
    }

    public static class StringExtensions
    {
        public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
