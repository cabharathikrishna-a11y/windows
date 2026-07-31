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

            string urlFile = Path.Combine(currentDir, "app_url.txt");
            if (File.Exists(urlFile))
            {
                try
                {
                    string fileUrl = File.ReadAllText(urlFile).Trim();
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        appUrl = fileUrl;
                    }
                }
                catch { }
            }

            string[] possibleEdgePaths = new string[]
            {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe")
            };

            string edgePath = null;
            foreach (string p in possibleEdgePaths)
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    edgePath = p;
                    break;
                }
            }

            if (edgePath != null)
            {
                try
                {
                    string profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LifeOS\\EdgeProfile");
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = edgePath;
                    psi.Arguments = string.Format("--app=\"{0}\" --user-data-dir=\"{1}\" --window-size=1280,800", appUrl, profileDir);
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                    return;
                }
                catch { }
            }

            // Fallback to default system browser
            try
            {
                Process.Start(new ProcessStartInfo(appUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not launch Life OS Desktop: " + ex.Message, "Life OS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
