using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace LifeOS.Installer
{
    class CustomWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            if (request != null)
            {
                request.AllowAutoRedirect = true;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) LifeOS-Setup/1.0";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Timeout = 600000;
            }
            return request;
        }
    }

    class Program
    {
        private const string DEFAULT_APP_URL = "https://ais-pre-wzrnxak24bqyxyrm3fnzxv-269590861741.asia-southeast1.run.app";

        static void Main(string[] args)
        {
            Console.Title = "Life OS - Automatic Application Setup & Installer";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=========================================================================");
            Console.WriteLine("       Life OS Windows Desktop - Automatic Installer & Launcher          ");
            Console.WriteLine("=========================================================================");
            Console.ResetColor();
            Console.WriteLine();

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string installDir = Path.Combine(localAppData, "LifeOS");
            string zipPath = Path.Combine(installDir, "Life_OS_Windows_Bundle.zip");
            string currentExeDir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                // Write App URL configuration file
                string appUrlFile = Path.Combine(installDir, "app_url.txt");
                File.WriteAllText(appUrlFile, DEFAULT_APP_URL);

                // Strategy 0: Check if local zip bundle exists next to installer
                string localZip = Path.Combine(currentExeDir, "Life_OS_Windows.zip");
                if (!File.Exists(localZip)) localZip = Path.Combine(currentExeDir, "Life OS Windows.zip");
                
                bool downloaded = false;

                if (File.Exists(localZip))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[1/4] Local package bundle found! Preparing local package...");
                    Console.ResetColor();
                    try { File.Copy(localZip, zipPath, true); downloaded = true; } catch { }
                }

                if (!downloaded)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[1/4] Connecting to GitHub server...");
                    Console.ResetColor();
                    
                    try
                    {
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | (SecurityProtocolType)192;
                    }
                    catch { }

                    string targetRepo = "GH_REPO_PLACEHOLDER";
                    string[] reposToTry = new string[]
                    {
                        targetRepo,
                        "cabharathikrishna-a11y/windows",
                        "cabharathikrishna-a11y/LifeOS",
                        "cabharathikrishna-a11y/Life-OS"
                    };

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[2/4] Downloading Life OS desktop bundle...");
                    Console.ResetColor();

                    foreach (string repo in reposToTry)
                    {
                        if (string.IsNullOrEmpty(repo) || (repo == "GH_REPO_PLACEHOLDER" && reposToTry.Length > 1 && repo != reposToTry[1]))
                        {
                            continue;
                        }

                        string[] candidateUrls = new string[]
                        {
                            string.Format("https://github.com/{0}/releases/latest/download/Life_OS_Windows.zip", repo),
                            string.Format("https://github.com/{0}/releases/latest/download/Life%20OS%20Windows.zip", repo)
                        };

                        foreach (string url in candidateUrls)
                        {
                            try
                            {
                                Console.WriteLine("Source: " + url);
                                using (CustomWebClient client = new CustomWebClient())
                                {
                                    client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e)
                                    {
                                        int percent = e.ProgressPercentage;
                                        int progressWidth = 28;
                                        int filled = (percent * progressWidth) / 100;
                                        string bar = new string('=', filled) + (filled < progressWidth ? ">" : "") + new string(' ', Math.Max(0, progressWidth - filled - 1));
                                        double mbReceived = e.BytesReceived / 1024.0 / 1024.0;
                                        double mbTotal = e.TotalBytesToReceive / 1024.0 / 1024.0;
                                        Console.Write(string.Format("\rProgress: [{0}] {1}% ({2:F1} MB / {3:F1} MB) ", bar, percent, mbReceived, mbTotal));
                                    };
                                    
                                    client.DownloadFile(new Uri(url), zipPath);
                                    Console.WriteLine();
                                    if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 1000)
                                    {
                                        downloaded = true;
                                        break;
                                    }
                                }
                            }
                            catch { }
                        }

                        if (downloaded) break;

                        try
                        {
                            string apiUrl = string.Format("https://api.github.com/repos/{0}/releases/latest", repo);
                            using (CustomWebClient client = new CustomWebClient())
                            {
                                string json = client.DownloadString(apiUrl);
                                int idx = json.IndexOf(string.Format("https://github.com/{0}/releases/download/", repo));
                                if (idx != -1)
                                {
                                    int endIdx = json.IndexOf("\"", idx);
                                    if (endIdx != -1)
                                    {
                                        string apiDownloadUrl = json.Substring(idx, endIdx - idx);
                                        Console.WriteLine("Source via API: " + apiDownloadUrl);
                                        client.DownloadFile(apiDownloadUrl, zipPath);
                                        if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 1000)
                                        {
                                            downloaded = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }

                        if (downloaded) break;
                    }
                }

                if (downloaded && File.Exists(zipPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[3/4] Unpacking and configuring Life OS Desktop installation...");
                    Console.ResetColor();

                    try
                    {
                        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                string destinationPath = Path.Combine(installDir, entry.FullName);
                                string dirPath = Path.GetDirectoryName(destinationPath);
                                
                                if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                                {
                                    Directory.CreateDirectory(dirPath);
                                }
                                
                                if (!string.IsNullOrEmpty(entry.Name))
                                {
                                    entry.ExtractToFile(destinationPath, true);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Extract note: " + ex.Message);
                    }

                    try { File.Delete(zipPath); } catch { }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[3/4] Initializing Life OS Application...");
                    Console.ResetColor();
                }

                // Copy executable to installation directory
                string targetExe = Path.Combine(installDir, "Life_OS.exe");
                string launcherSource = Path.Combine(currentExeDir, "Life_OS.exe");
                if (File.Exists(launcherSource) && launcherSource != targetExe)
                {
                    try { File.Copy(launcherSource, targetExe, true); } catch { }
                }

                // Create Desktop & Start Menu Shortcuts
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[4/4] Creating Start Menu and Desktop shortcuts...");
                Console.ResetColor();

                string finalExeToRun = File.Exists(targetExe) ? targetExe : Path.Combine(installDir, "Life_OS_Windows_Launcher.cmd");

                // 1. Desktop Shortcut
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string desktopShortcut = Path.Combine(desktopPath, "Life OS.lnk");
                    CreateShortcut(desktopShortcut, finalExeToRun, installDir, "Life OS Desktop Application");
                    Console.WriteLine(" -> Created Desktop Shortcut: " + desktopShortcut);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Desktop Shortcut note: " + ex.Message);
                }

                // 2. Start Menu Programs Shortcut
                try
                {
                    string startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Life OS");
                    if (!Directory.Exists(startMenuFolder))
                    {
                        Directory.CreateDirectory(startMenuFolder);
                    }
                    string startMenuShortcut = Path.Combine(startMenuFolder, "Life OS.lnk");
                    CreateShortcut(startMenuShortcut, finalExeToRun, installDir, "Life OS Desktop Application");
                    Console.WriteLine(" -> Created Start Menu Shortcut: " + startMenuShortcut);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Start Menu Shortcut note: " + ex.Message);
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("=========================================================================");
                Console.WriteLine("       Setup Complete! Launching Life OS Desktop Application...          ");
                Console.WriteLine("=========================================================================");
                Console.ResetColor();

                // Launch Application
                bool launched = false;
                if (File.Exists(targetExe))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(targetExe);
                        psi.UseShellExecute = true;
                        psi.WorkingDirectory = installDir;
                        Process.Start(psi);
                        launched = true;
                    }
                    catch { }
                }

                if (!launched && File.Exists(finalExeToRun))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(finalExeToRun);
                        psi.UseShellExecute = true;
                        psi.WorkingDirectory = installDir;
                        Process.Start(psi);
                        launched = true;
                    }
                    catch { }
                }

                // Guaranteed Fallback Launcher
                if (!launched)
                {
                    LaunchDirectBrowserAppMode(DEFAULT_APP_URL);
                }

                Thread.Sleep(1500);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                string errMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine("Setup note: " + errMsg);
                Console.ResetColor();

                // Even on error, attempt to open Life OS app window!
                LaunchDirectBrowserAppMode(DEFAULT_APP_URL);
                Thread.Sleep(2000);
            }
        }

        private static void LaunchDirectBrowserAppMode(string appUrl)
        {
            string localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string installDir = Path.Combine(localAppDataDir, "LifeOS");
            string targetExe = Path.Combine(installDir, "Life_OS.exe");

            if (File.Exists(targetExe))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo(targetExe);
                    psi.UseShellExecute = true;
                    psi.WorkingDirectory = installDir;
                    Process.Start(psi);
                    return;
                }
                catch { }
            }

            try
            {
                Process.Start(new ProcessStartInfo(appUrl) { UseShellExecute = true });
            }
            catch { }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    shortcut.Description = description;
                    shortcut.Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateShortcut error: " + ex.Message);
            }
        }
    }
}
