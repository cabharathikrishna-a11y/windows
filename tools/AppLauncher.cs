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

            // Set WebBrowser control to use IE11 rendering engine mode in registry for local user
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

            // Check for custom app_url.txt
            string urlFile = Path.Combine(currentDir, "app_url.txt");
            if (!File.Exists(urlFile))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                urlFile = Path.Combine(localAppData, "LifeOS\\app_url.txt");
            }

            if (File.Exists(urlFile))
            {
                try
                {
                    string fileUrl = File.ReadAllText(urlFile).Trim();
                    if (!string.IsNullOrEmpty(fileUrl) && fileUrl.StartsWith("http"))
                    {
                        appUrl = fileUrl;
                    }
                }
                catch { }
            }

            // Attempt 1: Try launching as standalone Frameless Desktop App via Edge / Chrome
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] candidateBrowsers = new string[]
            {
                Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                Path.Combine(programFiles, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(programFilesX86, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(localAppDataDir, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(localAppDataDir, @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(programFiles, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                Path.Combine(programFilesX86, @"BraveSoftware\Brave-Browser\Application\brave.exe")
            };

            bool standaloneLaunched = false;

            foreach (string browserPath in candidateBrowsers)
            {
                if (!string.IsNullOrEmpty(browserPath) && File.Exists(browserPath))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = browserPath;
                        psi.Arguments = string.Format("--new-window --app=\"{0}\"", appUrl);
                        psi.UseShellExecute = true;
                        Process proc = Process.Start(psi);
                        if (proc != null)
                        {
                            standaloneLaunched = true;
                            break;
                        }
                    }
                    catch
                    {
                        try
                        {
                            ProcessStartInfo psi2 = new ProcessStartInfo();
                            psi2.FileName = browserPath;
                            psi2.Arguments = string.Format("--app=\"{0}\"", appUrl);
                            psi2.UseShellExecute = false;
                            Process proc = Process.Start(psi2);
                            if (proc != null)
                            {
                                standaloneLaunched = true;
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }

            // Attempt 2: If standalone browser launch succeeded, keep a lightweight system tray or exit cleanly
            // If standalone launch failed or browser was not found, run native WinForms Application Form directly!
            if (!standaloneLaunched)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = appUrl;
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                }
                catch { }

                Application.Run(new LifeOSForm(appUrl));
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
            lblTitle.Text = "Life OS Desktop Application";
            lblTitle.ForeColor = Color.FromArgb(0, 230, 153);
            lblTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(210, 10);

            topBar.Controls.Add(btnRefresh);
            topBar.Controls.Add(btnHome);
            topBar.Controls.Add(lblTitle);

            // Create Web Browser Control
            browser = new WebBrowser();
            browser.Dock = DockStyle.Fill;
            browser.ScriptErrorsSuppressed = true;
            browser.Navigate(url);

            this.Controls.Add(browser);
            this.Controls.Add(topBar);
        }
    }
}
