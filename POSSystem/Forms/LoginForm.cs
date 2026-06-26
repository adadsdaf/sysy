using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Forms
{
    public partial class LoginForm : Form
    {
        public User? LoggedInUser { get; private set; }
        private string _pin = "";

        public LoginForm()
        {
            InitializeComponent();
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
        }

        private void InitializeComponent()
        {
            this.Text = "تسجيل الدخول - نقطة المبيعات";
            this.Size = new Size(450, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 46);

            var titleLbl = new Label { Text = "نقطة المبيعات", Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None, Size = new Size(380, 50), Location = new Point(35, 30) };
            var subtitleLbl = new Label { Text = "أدخل رمز PIN للدخول", Font = new Font("Arial", 12),
                ForeColor = Color.FromArgb(160,160,180), TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 30), Location = new Point(35, 85) };

            _pinDisplay = new Label { Text = "", Font = new Font("Arial", 28, FontStyle.Bold),
                ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(45,45,68),
                Size = new Size(300, 60), Location = new Point(75, 130), BorderStyle = BorderStyle.FixedSingle };

            var panel = new Panel { Size = new Size(300, 280), Location = new Point(75, 210), BackColor = Color.Transparent };
            string[] labels = { "1","2","3","4","5","6","7","8","9","C","0","OK" };
            var colors = new Color[] {
                Color.FromArgb(60,60,90), Color.FromArgb(60,60,90), Color.FromArgb(60,60,90),
                Color.FromArgb(60,60,90), Color.FromArgb(60,60,90), Color.FromArgb(60,60,90),
                Color.FromArgb(60,60,90), Color.FromArgb(60,60,90), Color.FromArgb(60,60,90),
                Color.FromArgb(200,50,50), Color.FromArgb(60,60,90), Color.FromArgb(34,139,34) };
            for (int i = 0; i < 12; i++)
            {
                int col = i % 3, row = i / 3;
                var btn = new Button
                {
                    Text = labels[i], Size = new Size(88, 60),
                    Location = new Point(col * 96, row * 68),
                    Font = new Font("Arial", 18, FontStyle.Bold),
                    ForeColor = Color.White, BackColor = colors[i],
                    FlatStyle = FlatStyle.Flat, Tag = labels[i]
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 120);
                btn.Click += PinButton_Click;
                panel.Controls.Add(btn);
            }

            _errorLbl = new Label { Text = "", ForeColor = Color.Red, Font = new Font("Arial", 11),
                TextAlign = ContentAlignment.MiddleCenter, Size = new Size(380, 25), Location = new Point(35, 500) };

            this.Controls.AddRange(new Control[] { titleLbl, subtitleLbl, _pinDisplay, panel, _errorLbl });
        }

        private Label _pinDisplay = null!;
        private Label _errorLbl = null!;

        private void PinButton_Click(object? sender, EventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "";
            if (tag == "C") { _pin = _pin.Length > 0 ? _pin[..^1] : ""; }
            else if (tag == "OK") { TryLogin(); return; }
            else if (_pin.Length < 8) { _pin += tag; }
            _pinDisplay.Text = new string('●', _pin.Length);
            _errorLbl.Text = "";
        }

        private void TryLogin()
        {
            if (string.IsNullOrEmpty(_pin)) { _errorLbl.Text = "أدخل رمز PIN"; return; }
            var user = UserService.Login(_pin);
            if (user != null) { LoggedInUser = user; DialogResult = DialogResult.OK; Close(); }
            else { _errorLbl.Text = "رمز PIN غير صحيح"; _pin = ""; _pinDisplay.Text = ""; }
        }
    }
}
