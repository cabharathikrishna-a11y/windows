using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

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

            // List of candidate web browsers that support app window mode
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
                Path.Combine(localAppDataDir, @"Microsoft\Edge\Application\msedge.exe")
            };

            bool launched = false;

            // 1. Try launching in standalone app window mode via Edge / Chrome
            foreach (string browserPath in candidateBrowsers)
            {
                if (!string.IsNullOrEmpty(browserPath) && File.Exists(browserPath))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = browserPath;
                        psi.Arguments = "--app=" + appUrl;
                        psi.UseShellExecute = false;
                        Process.Start(psi);
                        launched = true;
                        break;
                    }
                    catch { }
                }
            }

            // 2. Fallback: Launch using default system browser
            if (!launched)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = appUrl;
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                    launched = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not launch Life OS Application:\n" + ex.Message + "\n\nPlease visit: " + appUrl, "Life OS Desktop Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
