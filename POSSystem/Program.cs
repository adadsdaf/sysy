using POSSystem.Database;
using POSSystem.Forms;
using POSSystem.Services;

namespace POSSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetCompatibleTextRenderingDefault(false);

            // ─── 1. قراءة سلسلة اتصال السيرفر ───────────────────────────
            string serverConn = ReadServerConnectionString();

            // ─── 2. إنشاء قاعدة البيانات POSSystem إن لم تكن موجودة ─────
            try
            {
                DatabaseHelper.EnsureDatabaseExists(serverConn);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    BuildErrorMessage(serverConn, ex),
                    "فشل الاتصال بـ SQL Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // ─── 3. تهيئة الاتصال وإنشاء الجداول ────────────────────────
            try
            {
                DatabaseHelper.Initialize(serverConn);
                DatabaseHelper.TestConnection();
                SchemaSetup.CreateTablesIfNotExist();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"فشل تهيئة قاعدة البيانات!\n\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ─── 4. تحميل الإعدادات والتحقق من الترخيص ──────────────────
            var settings = SettingsService.Get();
            if (settings.LicenseStatus == "expired")
            {
                MessageBox.Show("انتهت صلاحية الترخيص! تواصل مع الدعم الفني.",
                    "انتهاء الترخيص", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ─── 5. حلقة تسجيل الدخول ──────────────────────────────────
            while (true)
            {
                // شاشة تسجيل الدخول
                using var loginForm = new LoginForm();
                if (loginForm.ShowDialog() != DialogResult.OK || loginForm.LoggedInUser == null) return;
                var user = loginForm.LoggedInUser;

                // ─── 6. لوحة تحكم ما بعد الدخول ─────────────────────────
                using var dashboard = new CashierDashboardForm(user);
                var dashResult = dashboard.ShowDialog();

                if (dashResult == DialogResult.Cancel) return; // خروج كلي

                if (dashboard.GoToPOS)
                {
                    // ─── 7. واجهة نقطة المبيعات ────────────────────────
                    Application.Run(new MainForm(user));
                    return; // بعد إغلاق نافذة POS نخرج من التطبيق
                }

                // إذا أغلق الـ dashboard بدون GoToPOS، ارجع لتسجيل الدخول
                // (مثلاً: بعد تغيير كلمة السر أو الدخول للإدارة فقط)
                // في هذه الحالة نخرج
                return;
            }
        }

        private static string ReadServerConnectionString()
        {
            var paths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConnectionString.txt"),
                Path.Combine(Directory.GetCurrentDirectory(), "ConnectionString.txt"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "ConnectionString.txt"),
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return File.ReadAllText(path).Trim();
            }

            return @"Server=.\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        private static string BuildErrorMessage(string connStr, Exception ex)
        {
            string server;
            try { server = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr).DataSource; }
            catch { server = connStr; }

            return $"""
                فشل الاتصال بـ SQL Server!

                السيرفر: {server}
                الخطأ: {ex.Message}

                ─────────────────────────────────
                خطوات الإصلاح:

                1. تأكد من تثبيت SQL Server Express (مجاني):
                   https://www.microsoft.com/sql-server/sql-server-downloads

                2. تأكد أن الخدمة تعمل:
                   ابحث عن "Services" ← MSSQL$SQLEXPRESS ← Start

                3. عدّل ملف ConnectionString.txt بجانب البرنامج:

                   للـ SQLEXPRESS:
                   Server=.\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True;

                   للـ Default Instance:
                   Server=.;Trusted_Connection=True;TrustServerCertificate=True;

                   مع اسم مستخدم وكلمة مرور:
                   Server=.\SQLEXPRESS;User Id=sa;Password=YourPass;TrustServerCertificate=True;
                """;
        }
    }
}
