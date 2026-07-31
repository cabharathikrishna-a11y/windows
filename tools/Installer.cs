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

            try
            {
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[1/4] Connecting to GitHub Releases repository...");
                Console.ResetColor();
                
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                string downloadUrl = "https://github.com/cabharathikrishna-a11y/LifeOS/releases/latest/download/Life_OS_Windows.zip";
                
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "LifeOS-Windows-Installer");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[2/4] Downloading required Life OS application files from GitHub...");
                    Console.ResetColor();
                    
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        Console.Write($"\rProgress: {e.ProgressPercentage}% ({e.BytesReceived / 1024 / 1024} MB / {e.TotalBytesToReceive / 1024 / 1024} MB)");
                    };
                    
                    client.DownloadFileTaskAsync(new Uri(downloadUrl), zipPath).Wait();
                    Console.WriteLine();
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
                Console.WriteLine("Installation Error: " + ex.Message);
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
