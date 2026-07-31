using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LifeOS.Installer
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        static void Main(string[] args)
        {
            Console.Title = "Life OS - Windows Application Installer & Web Downloader";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=========================================================================");
            Console.WriteLine("       Life OS Windows Desktop - Automatic Web Installer & Setup        ");
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

                // Strategy 0: Local Bundle Check (If installer is placed in same folder as zip/exe)
                string localZip = Path.Combine(currentExeDir, "Life_OS_Windows.zip");
                if (!File.Exists(localZip)) localZip = Path.Combine(currentExeDir, "Life OS Windows.zip");
                
                bool downloaded = false;

                if (File.Exists(localZip))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[1/4] Local application bundle found! Copying locally...");
                    Console.ResetColor();
                    File.Copy(localZip, zipPath, true);
                    downloaded = true;
                }

                if (!downloaded)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[1/4] Connecting to GitHub Releases repository...");
                    Console.ResetColor();
                    
                    try
                    {
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | (SecurityProtocolType)192; // TLS 1.2, 1.1, 1.0
                    }
                    catch { }

                    string[] candidateUrls = new string[]
                    {
                        "https://github.com/cabharathikrishna-a11y/LifeOS/releases/latest/download/Life_OS_Windows.zip",
                        "https://github.com/cabharathikrishna-a11y/LifeOS/releases/latest/download/Life%20OS%20Windows.zip"
                    };

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[2/4] Downloading required Life OS application files from GitHub...");
                    Console.ResetColor();

                    foreach (string url in candidateUrls)
                    {
                        try
                        {
                            using (WebClient client = new WebClient())
                            {
                                client.Headers.Add("User-Agent", "LifeOS-Windows-Installer");
                                client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e)
                                {
                                    Console.Write(string.Format("\rProgress: {0}% ({1} MB / {2} MB)", e.ProgressPercentage, e.BytesReceived / 1024 / 1024, e.TotalBytesToReceive / 1024 / 1024));
                                };
                                
                                client.DownloadFileTaskAsync(new Uri(url), zipPath).Wait();
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

                    // Fallback to GitHub API query if direct download links failed
                    if (!downloaded)
                    {
                        try
                        {
                            using (WebClient client = new WebClient())
                            {
                                client.Headers.Add("User-Agent", "LifeOS-Windows-Installer");
                                string json = client.DownloadString("https://api.github.com/repos/cabharathikrishna-a11y/LifeOS/releases/latest");
                                int idx = json.IndexOf("https://github.com/cabharathikrishna-a11y/LifeOS/releases/download/");
                                if (idx != -1)
                                {
                                    int endIdx = json.IndexOf("\"", idx);
                                    if (endIdx != -1)
                                    {
                                        string apiDownloadUrl = json.Substring(idx, endIdx - idx);
                                        client.DownloadFile(apiDownloadUrl, zipPath);
                                        if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 1000)
                                        {
                                            downloaded = true;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (!downloaded || !File.Exists(zipPath))
                {
                    throw new Exception("Could not download Life OS bundle from GitHub Releases. Please ensure GitHub has published a release with Life_OS_Windows.zip.");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[3/4] Unpacking and configuring Life OS Desktop installation...");
                Console.ResetColor();

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

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                // Create Desktop Shortcut
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string shortcutPath = Path.Combine(desktopPath, "Life OS.lnk");
                    string targetExe = Path.Combine(installDir, "Life_OS.exe");
                    if (!File.Exists(targetExe))
                    {
                        targetExe = Path.Combine(installDir, "Life_OS_Windows_Launcher.cmd");
                    }

                    CreateShortcut(shortcutPath, targetExe, installDir, "Life OS Desktop Application");
                }
                catch (Exception shortcutEx)
                {
                    Console.WriteLine("Note on Shortcut creation: " + shortcutEx.Message);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[4/4] Installation Complete! Launching Life OS Desktop...");
                Console.ResetColor();
                Console.WriteLine();

                string exeToRun = Path.Combine(installDir, "Life_OS.exe");
                if (File.Exists(exeToRun))
                {
                    Process.Start(new ProcessStartInfo(exeToRun) { UseShellExecute = true, WorkingDirectory = installDir });
                }
                else
                {
                    string cmdToRun = Path.Combine(installDir, "Life_OS_Windows_Launcher.cmd");
                    if (File.Exists(cmdToRun))
                    {
                        Process.Start(new ProcessStartInfo(cmdToRun) { UseShellExecute = true, WorkingDirectory = installDir });
                    }
                    else
                    {
                        string jarToRun = Path.Combine(installDir, "Life_OS_Desktop.jar");
                        if (File.Exists(jarToRun))
                        {
                            Process.Start(new ProcessStartInfo("java", "-jar \"" + jarToRun + "\"") { UseShellExecute = true, WorkingDirectory = installDir });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                string errMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine("Installation Error: " + errMsg);
                Console.ResetColor();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string description)
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
    }
}
