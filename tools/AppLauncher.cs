using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LifeOS.Launcher
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            catch { }

            // Enable IE11 rendering mode for WebBrowser control fallback in registry
            try
            {
                string appName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 11001, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            string appUrl = "https://ais-pre-wzrnxak24bqyxyrm3fnzxv-269590861741.asia-southeast1.run.app";
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Check for custom app_url.txt in current dir or local app data
            try
            {
                string urlFile = Path.Combine(currentDir, "app_url.txt");
                if (!File.Exists(urlFile))
                {
                    urlFile = Path.Combine(localAppDataDir, @"LifeOS\app_url.txt");
                }

                if (File.Exists(urlFile))
                {
                    string fileUrl = File.ReadAllText(urlFile).Trim();
                    if (!string.IsNullOrEmpty(fileUrl) && fileUrl.StartsWith("http"))
                    {
                        appUrl = fileUrl;
                    }
                }
            }
            catch { }

            // Run Native Windows Application Window directly
            try
            {
                Application.Run(new LifeOSForm(appUrl));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Life OS Desktop Application\n\nError initializing native window:\n" + ex.Message + "\n\nApplication URL:\n" + appUrl, "Life OS Desktop", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class LifeOSForm : Form
    {
        private WebBrowser browser;
        private string currentUrl;

        public LifeOSForm(string url)
        {
            this.currentUrl = url;
            this.Text = "Life OS - Personal Operating System";
            this.Size = new Size(1300, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            this.BackColor = Color.FromArgb(18, 18, 18);

            // Create Top Control Bar
            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 40;
            topBar.BackColor = Color.FromArgb(30, 30, 30);

            Button btnRefresh = new Button();
            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.ForeColor = Color.White;
            btnRefresh.BackColor = Color.FromArgb(50, 50, 50);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Size = new Size(90, 28);
            btnRefresh.Location = new Point(10, 6);
            btnRefresh.Click += (s, e) => { if (browser != null) browser.Refresh(); };

            Button btnHome = new Button();
            btnHome.Text = "🏠 Home";
            btnHome.ForeColor = Color.White;
            btnHome.BackColor = Color.FromArgb(50, 50, 50);
            btnHome.FlatStyle = FlatStyle.Flat;
            btnHome.FlatAppearance.BorderSize = 0;
            btnHome.Size = new Size(80, 28);
            btnHome.Location = new Point(110, 6);
            btnHome.Click += (s, e) => { if (browser != null) browser.Navigate(currentUrl); };

            Label lblTitle = new Label();
            lblTitle.Text = "Life OS Native Desktop Application";
            lblTitle.ForeColor = Color.FromArgb(0, 230, 153);
            lblTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(210, 10);

            topBar.Controls.Add(btnRefresh);
            topBar.Controls.Add(btnHome);
            topBar.Controls.Add(lblTitle);

            // Create Embedded Web Browser Control
            browser = new WebBrowser();
            browser.Dock = DockStyle.Fill;
            browser.ScriptErrorsSuppressed = true;
            browser.Navigate(url);

            this.Controls.Add(browser);
            this.Controls.Add(topBar);
        }
    }
}
