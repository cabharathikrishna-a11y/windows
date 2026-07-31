using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace LifeOS.Launcher
{
    class Program
    {
        private static HttpListener listener;
        private static int localPort = 18492;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            catch { }

            // Enable IE11 emulation for WinForms WebBrowser fallback
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

            // 1. Start Embedded Local Web & Cross-Device Sync Server Engine
            StartLocalServer();

            string appUrl = string.Format("http://127.0.0.1:{0}/", localPort);

            // Check for custom local url file override if specified by user
            try
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
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

            // 2. Launch in Edge / Chrome Standalone App Window Mode
            bool standaloneLaunched = false;
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string userAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] candidateBrowsers = new string[]
            {
                Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                Path.Combine(programFiles, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(programFilesX86, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(userAppData, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(userAppData, @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(programFiles, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                Path.Combine(programFilesX86, @"BraveSoftware\Brave-Browser\Application\brave.exe")
            };

            foreach (string browserPath in candidateBrowsers)
            {
                if (!string.IsNullOrEmpty(browserPath) && File.Exists(browserPath))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = browserPath;
                        psi.Arguments = string.Format("--app=\"{0}\" --no-first-run --no-default-browser-check", appUrl);
                        psi.UseShellExecute = true;
                        Process proc = Process.Start(psi);
                        if (proc != null)
                        {
                            standaloneLaunched = true;
                            break;
                        }
                    }
                    catch { }
                }
            }

            // 3. Fallback to Native Windows Form Window
            if (!standaloneLaunched)
            {
                try
                {
                    Application.Run(new LifeOSForm(appUrl));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Life OS Desktop Application\n\nError initializing window:\n" + ex.Message + "\n\nApplication URL:\n" + appUrl, "Life OS Desktop", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private static void StartLocalServer()
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", localPort));
                listener.Prefixes.Add(string.Format("http://localhost:{0}/", localPort));
                listener.Start();

                Thread serverThread = new Thread(() =>
                {
                    while (listener.IsListening)
                    {
                        try
                        {
                            HttpListenerContext context = listener.GetContext();
                            byte[] buffer = Encoding.UTF8.GetBytes(GetOfflineAppHtml());
                            context.Response.ContentType = "text/html; charset=utf-8";
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                        }
                        catch { }
                    }
                });
                serverThread.IsBackground = true;
                serverThread.Start();
            }
            catch { }
        }

        private static string GetOfflineAppHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Life OS - Personal Operating System (Windows Desktop)</title>
    <!-- Firebase App & Database SDKs for Realtime Sync -->
    <script src=""https://www.gstatic.com/firebasejs/9.23.0/firebase-app-compat.js""></script>
    <script src=""https://www.gstatic.com/firebasejs/9.23.0/firebase-database-compat.js""></script>
    <style>
        :root {
            --bg-color: #0f172a;
            --card-bg: #1e293b;
            --primary: #10b981;
            --primary-hover: #059669;
            --text-main: #f8fafc;
            --text-muted: #94a3b8;
            --border-color: #334155;
            --accent: #38bdf8;
            --warning: #f59e0b;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; }
        body { background-color: var(--bg-color); color: var(--text-main); display: flex; height: 100vh; overflow: hidden; }
        
        /* Sidebar */
        .sidebar { width: 260px; background-color: #090d16; border-right: 1px solid var(--border-color); display: flex; flex-direction: column; padding: 20px; flex-shrink: 0; }
        .logo { font-size: 20px; font-weight: bold; color: var(--primary); margin-bottom: 25px; display: flex; align-items: center; gap: 10px; }
        .nav-item { padding: 12px 16px; margin-bottom: 6px; border-radius: 8px; cursor: pointer; color: var(--text-muted); font-weight: 500; display: flex; align-items: center; gap: 12px; transition: all 0.2s; user-select: none; }
        .nav-item:hover, .nav-item.active { background-color: var(--card-bg); color: var(--primary); }
        .nav-item.active { border-left: 4px solid var(--primary); }
        
        /* Main Content */
        .main-content { flex: 1; padding: 25px 30px; overflow-y: auto; display: flex; flex-direction: column; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 25px; gap: 15px; flex-wrap: wrap; }
        .title { font-size: 24px; font-weight: bold; }
        .subtitle { color: var(--text-muted); font-size: 13px; margin-top: 4px; }
        
        /* Sync Account Control */
        .account-bar { display: flex; align-items: center; gap: 10px; background: var(--card-bg); padding: 8px 14px; border-radius: 8px; border: 1px solid var(--border-color); }
        .account-bar input { background: transparent; border: none; color: var(--text-main); font-size: 13px; outline: none; width: 220px; }
        
        /* Dashboard Cards Grid */
        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 20px; margin-bottom: 25px; }
        .card { background-color: var(--card-bg); border: 1px solid var(--border-color); border-radius: 12px; padding: 20px; display: flex; flex-direction: column; }
        .card-title { font-size: 16px; font-weight: bold; margin-bottom: 15px; color: var(--accent); display: flex; justify-content: space-between; align-items: center; }
        
        /* Form Inputs & Buttons */
        .input-group { display: flex; gap: 10px; margin-bottom: 15px; }
        input[type=""text""], input[type=""number""], select { flex: 1; padding: 10px 14px; background: #0f172a; border: 1px solid var(--border-color); border-radius: 6px; color: var(--text-main); font-size: 14px; outline: none; }
        input[type=""text""]:focus, input[type=""number""]:focus, select:focus { border-color: var(--primary); }
        button { background-color: var(--primary); color: white; border: none; padding: 10px 18px; border-radius: 6px; font-weight: bold; cursor: pointer; transition: background 0.2s; white-space: nowrap; }
        button:hover { background-color: var(--primary-hover); }
        .btn-secondary { background: #334155; }
        .btn-secondary:hover { background: #475569; }
        
        /* Item Lists */
        .item-list { list-style: none; overflow-y: auto; max-height: 280px; }
        .item-row { display: flex; align-items: center; justify-content: space-between; padding: 10px 12px; background: #0f172a; border-radius: 6px; margin-bottom: 8px; border: 1px solid var(--border-color); gap: 10px; }
        .item-row.completed span { text-decoration: line-through; color: var(--text-muted); }
        .item-row input[type=""checkbox""] { width: 18px; height: 18px; cursor: pointer; accent-color: var(--primary); }
        .delete-btn { background: transparent; color: #ef4444; border: none; cursor: pointer; font-size: 16px; padding: 2px 6px; }
        .delete-btn:hover { color: #dc2626; }
        
        /* Focus Timer */
        .timer-display { font-size: 48px; font-weight: bold; text-align: center; margin: 15px 0; font-family: monospace; color: var(--primary); }
        .timer-controls { display: flex; justify-content: center; gap: 12px; }

        .badge { background: rgba(16, 185, 129, 0.2); color: var(--primary); padding: 5px 12px; border-radius: 20px; font-size: 12px; font-weight: bold; display: inline-flex; align-items: center; gap: 6px; }
        .badge.offline { background: rgba(245, 158, 11, 0.2); color: var(--warning); }
        
        /* Tab Sections */
        .tab-section { display: none; }
        .tab-section.active { display: block; }
    </style>
</head>
<body>
    <div class=""sidebar"">
        <div class=""logo""><span>⚡</span> Life OS Desktop</div>
        <div class=""nav-item active"" onclick=""switchTab('dashboard', this)""><span>📊</span> Dashboard</div>
        <div class=""nav-item"" onclick=""switchTab('tasks', this)""><span>✅</span> Tasks & Planner</div>
        <div class=""nav-item"" onclick=""switchTab('notes', this)""><span>📝</span> Notes & Journal</div>
        <div class=""nav-item"" onclick=""switchTab('timer', this)""><span>⏱️</span> Focus Timer</div>
        <div class=""nav-item"" onclick=""switchTab('finance', this)""><span>💳</span> Finance Ledger</div>
        <div class=""nav-item"" onclick=""switchTab('sync', this)""><span>🔄</span> Android Sync Config</div>
        
        <div style=""margin-top: auto; padding-top: 15px; border-top: 1px solid var(--border-color); font-size: 12px; color: var(--text-muted);"">
            <div id=""sidebar-sync-text"">🟢 Android Protocol Engine</div>
            <div style=""font-size: 11px; margin-top: 4px; color: #64748b;"">Windows v19.0 Client</div>
        </div>
    </div>

    <div class=""main-content"">
        <!-- Top Bar -->
        <div class=""header"">
            <div>
                <div class=""title"" id=""page-title"">Personal Operating System</div>
                <div class=""subtitle"">Synchronized with Android Application</div>
            </div>
            <div style=""display: flex; gap: 12px; align-items: center;"">
                <div class=""account-bar"">
                    <span style=""font-size: 14px;"">👤</span>
                    <input type=""text"" id=""user-email"" placeholder=""Enter Android Account Email..."" onchange=""saveSyncConfig()"">
                    <button onclick=""saveSyncConfig()"" style=""padding: 4px 10px; font-size: 12px;"">Connect</button>
                </div>
                <div class=""badge offline"" id=""sync-status-badge"">⚡ Offline Local</div>
            </div>
        </div>

        <!-- DASHBOARD TAB -->
        <div id=""tab-dashboard"" class=""tab-section active"">
            <div class=""grid"">
                <!-- Daily Tasks -->
                <div class=""card"">
                    <div class=""card-title"">Daily Tasks <span id=""task-count-dash"">0 pending</span></div>
                    <div class=""input-group"">
                        <input type=""text"" id=""dash-new-task"" placeholder=""Add new task..."">
                        <button onclick=""addTaskFromInput('dash-new-task')"">Add</button>
                    </div>
                    <ul class=""item-list"" id=""dash-task-list""></ul>
                </div>

                <!-- Focus Timer -->
                <div class=""card"">
                    <div class=""card-title"">Focus Pomodoro Timer</div>
                    <div class=""timer-display"" id=""timer-display"">25:00</div>
                    <div class=""timer-controls"">
                        <button onclick=""toggleTimer()"" id=""timer-btn"">Start Session</button>
                        <button onclick=""resetTimer()"" class=""btn-secondary"">Reset</button>
                    </div>
                    <div style=""margin-top: 15px; text-align: center; font-size: 13px; color: var(--text-muted);"" id=""today-focus-stat"">
                        Today's Focus: 0 mins (Synced with Android Leaderboard)
                    </div>
                </div>

                <!-- Recent Notes -->
                <div class=""card"">
                    <div class=""card-title"">Quick Notes</div>
                    <textarea id=""quick-notes"" style=""width: 100%; height: 160px; background: #0f172a; border: 1px solid var(--border-color); border-radius: 6px; color: var(--text-main); padding: 12px; font-size: 14px; outline: none; resize: none;"" placeholder=""Type quick notes here... (Synced with Android)""></textarea>
                </div>
            </div>
        </div>

        <!-- TASKS TAB -->
        <div id=""tab-tasks"" class=""tab-section"">
            <div class=""card"">
                <div class=""card-title"">Tasks & Action Items</div>
                <div class=""input-group"">
                    <input type=""text"" id=""tasks-new-task"" placeholder=""What do you need to accomplish?"">
                    <select id=""tasks-category"" style=""max-width: 150px;"">
                        <option value=""Inbox"">Inbox</option>
                        <option value=""Work"">Work</option>
                        <option value=""Personal"">Personal</option>
                        <option value=""Urgent"">Urgent</option>
                    </select>
                    <button onclick=""addTaskWithCategory()"">Add Task</button>
                </div>
                <ul class=""item-list"" id=""full-task-list"" style=""max-height: 500px;""></ul>
            </div>
        </div>

        <!-- NOTES TAB -->
        <div id=""tab-notes"" class=""tab-section"">
            <div class=""card"">
                <div class=""card-title"">Journal & Keep Notes</div>
                <div class=""input-group"">
                    <input type=""text"" id=""journal-title"" placeholder=""Entry Title..."">
                    <button onclick=""saveJournalEntry()"">Save Entry</button>
                </div>
                <textarea id=""journal-text"" style=""width: 100%; height: 350px; background: #0f172a; border: 1px solid var(--border-color); border-radius: 6px; color: var(--text-main); padding: 14px; font-size: 15px; outline: none; resize: vertical; margin-bottom: 15px;"" placeholder=""Write your journal or detailed thoughts here...""></textarea>
                <div class=""card-title"">Saved Entries</div>
                <ul class=""item-list"" id=""journal-entries-list"" style=""max-height: 200px;""></ul>
            </div>
        </div>

        <!-- TIMER TAB -->
        <div id=""tab-timer"" class=""tab-section"">
            <div class=""card"" style=""max-width: 600px; margin: 0 auto; text-align: center;"">
                <div class=""card-title"" style=""justify-content: center;"">Focus & Productivity Session</div>
                <div style=""margin: 10px 0;"">
                    <button onclick=""setTimerMinutes(15)"" class=""btn-secondary"">15m</button>
                    <button onclick=""setTimerMinutes(25)"" class=""btn-secondary"">25m</button>
                    <button onclick=""setTimerMinutes(50)"" class=""btn-secondary"">50m</button>
                </div>
                <div class=""timer-display"" id=""big-timer-display"" style=""font-size: 72px;"">25:00</div>
                <div class=""timer-controls"" style=""margin-bottom: 20px;"">
                    <button onclick=""toggleTimer()"" id=""big-timer-btn"" style=""padding: 14px 30px; font-size: 16px;"">Start Session</button>
                    <button onclick=""resetTimer()"" class=""btn-secondary"" style=""padding: 14px 20px;"">Reset</button>
                </div>
                <div style=""color: var(--text-muted); font-size: 14px;"" id=""timer-sync-info"">
                    Completed focus sessions automatically log focus time to your Android account.
                </div>
            </div>
        </div>

        <!-- FINANCE TAB -->
        <div id=""tab-finance"" class=""tab-section"">
            <div class=""card"">
                <div class=""card-title"">Financial Ledger</div>
                <div class=""input-group"">
                    <select id=""fin-type"" style=""max-width: 130px;"">
                        <option value=""EXPENSE"">Expense</option>
                        <option value=""INCOME"">Income</option>
                    </select>
                    <input type=""number"" id=""fin-amount"" placeholder=""Amount"" step=""0.01"">
                    <input type=""text"" id=""fin-note"" placeholder=""Description / Category"">
                    <button onclick=""addFinanceTransaction()"">Record</button>
                </div>
                <ul class=""item-list"" id=""finance-list"" style=""max-height: 400px;""></ul>
            </div>
        </div>

        <!-- SYNC TAB -->
        <div id=""tab-sync"" class=""tab-section"">
            <div class=""card"">
                <div class=""card-title"">Android Sync Protocol Settings</div>
                <p style=""color: var(--text-muted); font-size: 14px; margin-bottom: 20px;"">
                    This Windows Desktop client uses the exact same Firebase Realtime Database protocols as the Android Life OS app:
                    <br>• Node: <code>FOCUS_TIMMER/USER/{sanitizedEmail}</code>
                    <br>• Subnodes: <code>TASKS_LIVE</code>, <code>JOURNAL_LIVE</code>, <code>FILE_EXPLORER_LIVE</code>, <code>FINANCE_LIVE</code>, <code>DEVICES_LOGGED_IN</code>
                </p>
                <div style=""display: flex; flex-direction: column; gap: 15px; max-width: 500px;"">
                    <div>
                        <label style=""font-size: 13px; color: var(--text-muted); display: block; margin-bottom: 6px;"">Account Email</label>
                        <input type=""text"" id=""cfg-email"" placeholder=""e.g. user@gmail.com"" style=""width: 100%;"">
                    </div>
                    <div>
                        <label style=""font-size: 13px; color: var(--text-muted); display: block; margin-bottom: 6px;"">Firebase Database URL (Optional override)</label>
                        <input type=""text"" id=""cfg-rtdb"" placeholder=""https://your-project-default-rtdb.firebaseio.com/"" style=""width: 100%;"">
                    </div>
                    <div>
                        <button onclick=""saveSyncConfigFromTab()"" style=""width: 100%; margin-top: 10px;"">Save & Connect Device Presence</button>
                    </div>
                    <div id=""cfg-status-message"" style=""font-size: 13px; margin-top: 10px;""></div>
                </div>
            </div>
        </div>
    </div>

    <script>
        // Data State
        let syncEmail = localStorage.getItem('lifeos_sync_email') || '';
        let firebaseDbUrl = localStorage.getItem('lifeos_rtdb_url') || 'https://ais-pre-wzrnxak24bqyxyrm3fnzxv-269590861741.asia-southeast1.run.app';
        let tasks = JSON.parse(localStorage.getItem('lifeos_tasks') || '[]');
        let journals = JSON.parse(localStorage.getItem('lifeos_journals') || '[]');
        let notes = localStorage.getItem('lifeos_notes') || '';
        let finance = JSON.parse(localStorage.getItem('lifeos_finance') || '[]');
        let todayFocusMs = parseInt(localStorage.getItem('lifeos_today_focus_ms') || '0');

        let firebaseApp = null;
        let firebaseDb = null;
        let isOnlineConnected = false;

        // Initialize UI
        document.getElementById('user-email').value = syncEmail;
        document.getElementById('cfg-email').value = syncEmail;
        document.getElementById('cfg-rtdb').value = firebaseDbUrl;
        document.getElementById('quick-notes').value = notes;

        renderTasks();
        renderJournals();
        renderFinance();
        updateTodayFocusDisplay();

        // Initialize Firebase Sync if email is configured
        if (syncEmail) {
            initFirebaseSync();
        }

        // Auto-save Quick Notes
        document.getElementById('quick-notes').addEventListener('input', (e) => {
            notes = e.target.value;
            localStorage.setItem('lifeos_notes', notes);
            pushNoteToCloud();
        });

        function switchTab(tabId, navEl) {
            document.querySelectorAll('.tab-section').forEach(el => el.classList.remove('active'));
            document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
            
            document.getElementById('tab-' + tabId).classList.add('active');
            if (navEl) navEl.classList.add('active');
            
            const titleMap = {
                'dashboard': 'Personal Operating System',
                'tasks': 'Tasks & Action Items',
                'notes': 'Journal & Notes',
                'timer': 'Focus Session Timer',
                'finance': 'Financial Ledger',
                'sync': 'Android Sync Configuration'
            };
            document.getElementById('page-title').innerText = titleMap[tabId] || 'Life OS';
        }

        // Sanitize Email (Android DevicePresenceManager protocol)
        function sanitizeEmail(email) {
            if (!email) return '';
            let trimmed = email.trim().toLowerCase();
            if (!trimmed.includes('@')) trimmed += '@gmail.com';
            return trimmed.replace(/\./g, '_')
                          .replace(/\$/g, '_')
                          .replace(/\[/g, '_')
                          .replace(/\]/g, '_')
                          .replace(/#/g, '_')
                          .replace(/\//g, '_');
        }

        function saveSyncConfig() {
            const emailInput = document.getElementById('user-email').value.trim();
            syncEmail = emailInput;
            localStorage.setItem('lifeos_sync_email', syncEmail);
            document.getElementById('cfg-email').value = syncEmail;
            initFirebaseSync();
        }

        function saveSyncConfigFromTab() {
            syncEmail = document.getElementById('cfg-email').value.trim();
            firebaseDbUrl = document.getElementById('cfg-rtdb').value.trim();
            localStorage.setItem('lifeos_sync_email', syncEmail);
            localStorage.setItem('lifeos_rtdb_url', firebaseDbUrl);
            document.getElementById('user-email').value = syncEmail;
            initFirebaseSync();
        }

        function initFirebaseSync() {
            if (!syncEmail) return;
            const sanitized = sanitizeEmail(syncEmail);
            
            try {
                // Initialize Firebase JS SDK
                if (!firebase.apps.length) {
                    firebaseApp = firebase.initializeApp({
                        databaseURL: firebaseDbUrl || ""https://ais-pre-wzrnxak24bqyxyrm3fnzxv-269590861741.asia-southeast1.run.app""
                    });
                } else {
                    firebaseApp = firebase.app();
                }
                
                firebaseDb = firebase.database();
                const userRef = firebaseDb.ref('FOCUS_TIMMER/USER/' + sanitized);

                // Register Device Presence (Android DevicePresenceManager protocol)
                const deviceKey = 'WIN_DESKTOP_' + window.location.hostname;
                const presenceRef = userRef.child('DEVICES_LOGGED_IN/' + deviceKey);
                
                presenceRef.update({
                    Login_status: true,
                    Upload_Status: 'COMPLETED',
                    deviceName: 'Windows PC (' + (navigator.platform || 'Desktop') + ')',
                    App_Version_No: '19.0',
                    Todays_Focus_Ms: todayFocusMs,
                    Last_Update_Time_and_Date: new Date().toISOString()
                });
                presenceRef.child('Login_status').onDisconnect().setValue(false);

                // Live Listeners for Tasks
                userRef.child('TASKS_LIVE').on('value', (snapshot) => {
                    if (snapshot.exists()) {
                        const cloudData = snapshot.val();
                        reconcileTasksFromCloud(cloudData);
                    }
                });

                // Live Listeners for Notes / Journal
                userRef.child('JOURNAL_LIVE').on('value', (snapshot) => {
                    if (snapshot.exists()) {
                        const cloudData = snapshot.val();
                        reconcileJournalsFromCloud(cloudData);
                    }
                });

                // Live Listeners for Finance
                userRef.child('FINANCE_LIVE').on('value', (snapshot) => {
                    if (snapshot.exists()) {
                        const cloudData = snapshot.val();
                        reconcileFinanceFromCloud(cloudData);
                    }
                });

                // Set Status UI
                isOnlineConnected = true;
                const badge = document.getElementById('sync-status-badge');
                badge.className = 'badge';
                badge.innerText = '🟢 Synced with Android';
                document.getElementById('sidebar-sync-text').innerText = '🟢 Synced (' + syncEmail + ')';
                document.getElementById('cfg-status-message').innerHTML = '<span style=""color: #10b981;"">✓ Active Realtime Connection established with ' + sanitized + '</span>';

            } catch (e) {
                console.warn('Firebase sync offline or waiting for RTDB config:', e);
                const badge = document.getElementById('sync-status-badge');
                badge.className = 'badge offline';
                badge.innerText = '⚡ Offline Standalone Engine';
            }
        }

        // TASK FUNCTIONS & SYNC
        function addTaskFromInput(inputId) {
            const el = document.getElementById(inputId);
            if (el && el.value.trim()) {
                const newTask = {
                    id: Date.now(),
                    title: el.value.trim(),
                    listCategory: 'Inbox',
                    isCompleted: false,
                    updatedAt: Date.now()
                };
                tasks.push(newTask);
                el.value = '';
                saveTasks();
                pushTaskToCloud(newTask);
            }
        }

        function addTaskWithCategory() {
            const titleEl = document.getElementById('tasks-new-task');
            const catEl = document.getElementById('tasks-category');
            if (titleEl && titleEl.value.trim()) {
                const newTask = {
                    id: Date.now(),
                    title: titleEl.value.trim(),
                    listCategory: catEl ? catEl.value : 'Inbox',
                    isCompleted: false,
                    updatedAt: Date.now()
                };
                tasks.push(newTask);
                titleEl.value = '';
                saveTasks();
                pushTaskToCloud(newTask);
            }
        }

        function toggleTask(id) {
            tasks = tasks.map(t => {
                if (t.id === id) {
                    const updated = { ...t, isCompleted: !t.isCompleted, updatedAt: Date.now() };
                    pushTaskToCloud(updated);
                    return updated;
                }
                return t;
            });
            saveTasks();
        }

        function deleteTask(id) {
            const task = tasks.find(t => t.id === id);
            if (task) {
                pushTaskToCloud(task, true);
            }
            tasks = tasks.filter(t => t.id !== id);
            saveTasks();
        }

        function saveTasks() {
            localStorage.setItem('lifeos_tasks', JSON.stringify(tasks));
            renderTasks();
        }

        function renderTasks() {
            const pendingCount = tasks.filter(t => !t.isCompleted).length;
            document.getElementById('task-count-dash').innerText = pendingCount + ' pending';

            const mapHtml = (t) => `
                <li class=""item-row ${t.isCompleted ? 'completed' : ''}"">
                    <div style=""display: flex; align-items: center; gap: 10px;"">
                        <input type=""checkbox"" ${t.isCompleted ? 'checked' : ''} onchange=""toggleTask(${t.id})"">
                        <span>${escapeHtml(t.title)} <small style=""color: var(--text-muted); font-size: 11px;"">[${escapeHtml(t.listCategory || 'Inbox')}]</small></span>
                    </div>
                    <button class=""delete-btn"" onclick=""deleteTask(${t.id})"">✕</button>
                </li>
            `;

            document.getElementById('dash-task-list').innerHTML = tasks.map(mapHtml).join('');
            document.getElementById('full-task-list').innerHTML = tasks.map(mapHtml).join('');
        }

        function pushTaskToCloud(task, isDeleted = false) {
            if (!firebaseDb || !syncEmail) return;
            const sanitized = sanitizeEmail(syncEmail);
            const key = 'task_' + Math.abs(hashCode(task.title + '|' + (task.listCategory || 'Inbox')));
            const ref = firebaseDb.ref('FOCUS_TIMMER/USER/' + sanitized + '/TASKS_LIVE/' + key);

            if (isDeleted) {
                ref.remove();
            } else {
                ref.set({
                    key: key,
                    title: task.title,
                    listCategory: task.listCategory || 'Inbox',
                    isCompleted: task.isCompleted,
                    updatedAt: task.updatedAt || Date.now()
                });
            }
        }

        function reconcileTasksFromCloud(cloudTasks) {
            let updated = false;
            for (let k in cloudTasks) {
                const item = cloudTasks[k];
                if (!item || !item.title) continue;
                let existing = tasks.find(t => t.title.toLowerCase() === item.title.toLowerCase());
                if (!existing) {
                    tasks.push({
                        id: Date.now() + Math.floor(Math.random() * 1000),
                        title: item.title,
                        listCategory: item.listCategory || 'Inbox',
                        isCompleted: !!item.isCompleted,
                        updatedAt: item.updatedAt || Date.now()
                    });
                    updated = true;
                } else if (existing.isCompleted !== !!item.isCompleted) {
                    existing.isCompleted = !!item.isCompleted;
                    updated = true;
                }
            }
            if (updated) {
                saveTasks();
            }
        }

        // JOURNAL & NOTES
        function saveJournalEntry() {
            const titleEl = document.getElementById('journal-title');
            const textEl = document.getElementById('journal-text');
            if (textEl && textEl.value.trim()) {
                const newEntry = {
                    id: Date.now(),
                    title: titleEl ? (titleEl.value.trim() || 'Untitled Journal') : 'Untitled Journal',
                    text: textEl.value.trim(),
                    dateString: new Date().toISOString().split('T')[0],
                    timestamp: Date.now()
                };
                journals.push(newEntry);
                if (titleEl) titleEl.value = '';
                textEl.value = '';
                saveJournals();
                pushJournalToCloud(newEntry);
            }
        }

        function saveJournals() {
            localStorage.setItem('lifeos_journals', JSON.stringify(journals));
            renderJournals();
        }

        function renderJournals() {
            const list = document.getElementById('journal-entries-list');
            list.innerHTML = journals.map(j => `
                <li class=""item-row"">
                    <div>
                        <strong>${escapeHtml(j.title)}</strong>
                        <div style=""font-size: 12px; color: var(--text-muted);"">${escapeHtml(j.text.substring(0, 60))}...</div>
                    </div>
                    <span style=""font-size: 11px; color: var(--text-muted);"">${j.dateString}</span>
                </li>
            `).join('');
        }

        function pushJournalToCloud(entry) {
            if (!firebaseDb || !syncEmail) return;
            const sanitized = sanitizeEmail(syncEmail);
            const key = 'journal_' + entry.timestamp + '_' + Math.abs(hashCode(entry.title));
            firebaseDb.ref('FOCUS_TIMMER/USER/' + sanitized + '/JOURNAL_LIVE/' + key).set({
                key: key,
                title: entry.title,
                text: entry.text,
                dateString: entry.dateString,
                timestamp: entry.timestamp,
                updatedAt: Date.now()
            });
        }

        function pushNoteToCloud() {
            if (!firebaseDb || !syncEmail) return;
            const sanitized = sanitizeEmail(syncEmail);
            firebaseDb.ref('FOCUS_TIMMER/USER/' + sanitized + '/KEEP_NOTES_LIVE/quick_desktop_notes').set({
                key: 'quick_desktop_notes',
                title: 'Quick Desktop Notes',
                content: notes,
                updatedAt: Date.now()
            });
        }

        function reconcileJournalsFromCloud(cloudJournals) {
            let updated = false;
            for (let k in cloudJournals) {
                const item = cloudJournals[k];
                if (!item || !item.title) continue;
                let existing = journals.find(j => j.timestamp === item.timestamp || j.title === item.title);
                if (!existing) {
                    journals.push({
                        id: Date.now() + Math.floor(Math.random() * 1000),
                        title: item.title,
                        text: item.text || '',
                        dateString: item.dateString || new Date().toISOString().split('T')[0],
                        timestamp: item.timestamp || Date.now()
                    });
                    updated = true;
                }
            }
            if (updated) {
                saveJournals();
            }
        }

        // FINANCE LEDGER
        function addFinanceTransaction() {
            const typeEl = document.getElementById('fin-type');
            const amtEl = document.getElementById('fin-amount');
            const noteEl = document.getElementById('fin-note');

            if (amtEl && amtEl.value) {
                const tx = {
                    id: Date.now(),
                    type: typeEl ? typeEl.value : 'EXPENSE',
                    amount: parseFloat(amtEl.value),
                    note: noteEl ? noteEl.value.trim() : '',
                    timestamp: Date.now()
                };
                finance.push(tx);
                amtEl.value = '';
                if (noteEl) noteEl.value = '';
                saveFinance();
                pushFinanceToCloud(tx);
            }
        }

        function saveFinance() {
            localStorage.setItem('lifeos_finance', JSON.stringify(finance));
            renderFinance();
        }

        function renderFinance() {
            const list = document.getElementById('finance-list');
            list.innerHTML = finance.map(f => `
                <li class=""item-row"">
                    <div>
                        <span style=""font-weight: bold; color: ${f.type === 'INCOME' ? '#10b981' : '#f43f5e'};"">
                            ${f.type === 'INCOME' ? '+' : '-'}$${f.amount.toFixed(2)}
                        </span>
                        <span style=""margin-left: 10px; font-size: 13px;"">${escapeHtml(f.note || 'General')}</span>
                    </div>
                    <span style=""font-size: 11px; color: var(--text-muted);"">${new Date(f.timestamp).toLocaleDateString()}</span>
                </li>
            `).join('');
        }

        function pushFinanceToCloud(tx) {
            if (!firebaseDb || !syncEmail) return;
            const sanitized = sanitizeEmail(syncEmail);
            const key = 'finance_' + tx.timestamp + '_' + Math.abs(hashCode(tx.note));
            firebaseDb.ref('FOCUS_TIMMER/USER/' + sanitized + '/FINANCE_LIVE/' + key).set({
                key: key,
                type: tx.type,
                amount: tx.amount,
                note: tx.note,
                timestamp: tx.timestamp,
                updatedAt: Date.now()
            });
        }

        function reconcileFinanceFromCloud(cloudFinance) {
            let updated = false;
            for (let k in cloudFinance) {
                const item = cloudFinance[k];
                if (!item || !item.amount) continue;
                let existing = finance.find(f => f.timestamp === item.timestamp);
                if (!existing) {
                    finance.push({
                        id: Date.now() + Math.floor(Math.random() * 1000),
                        type: item.type || 'EXPENSE',
                        amount: parseFloat(item.amount),
                        note: item.note || '',
                        timestamp: item.timestamp || Date.now()
                    });
                    updated = true;
                }
            }
            if (updated) {
                saveFinance();
            }
        }

        // FOCUS TIMER LOGIC
        let timerSeconds = 1500;
        let timerInterval = null;

        function setTimerMinutes(mins) {
            resetTimer();
            timerSeconds = mins * 60;
            updateTimerDisplay();
        }

        function toggleTimer() {
            const btn1 = document.getElementById('timer-btn');
            const btn2 = document.getElementById('big-timer-btn');
            if (timerInterval) {
                clearInterval(timerInterval);
                timerInterval = null;
                if (btn1) btn1.innerText = 'Start Session';
                if (btn2) btn2.innerText = 'Start Session';
            } else {
                if (btn1) btn1.innerText = 'Pause Session';
                if (btn2) btn2.innerText = 'Pause Session';
                timerInterval = setInterval(() => {
                    if (timerSeconds > 0) {
                        timerSeconds--;
                        todayFocusMs += 1000;
                        localStorage.setItem('lifeos_today_focus_ms', todayFocusMs.toString());
                        updateTimerDisplay();
                        updateTodayFocusDisplay();
                        
                        // Push today focus ms to Android Leaderboard every 10 seconds
                        if (timerSeconds % 10 === 0) {
                            pushFocusStatsToCloud();
                        }
                    } else {
                        clearInterval(timerInterval);
                        timerInterval = null;
                        alert('Focus Session Completed! Awesome work!');
                        pushFocusStatsToCloud();
                        resetTimer();
                    }
                }, 1000);
            }
        }

        function resetTimer() {
            if (timerInterval) clearInterval(timerInterval);
            timerInterval = null;
            timerSeconds = 1500;
            const btn1 = document.getElementById('timer-btn');
            const btn2 = document.getElementById('big-timer-btn');
            if (btn1) btn1.innerText = 'Start Session';
            if (btn2) btn2.innerText = 'Start Session';
            updateTimerDisplay();
        }

        function updateTimerDisplay() {
            const m = Math.floor(timerSeconds / 60).toString().padStart(2, '0');
            const s = (timerSeconds % 60).toString().padStart(2, '0');
            const txt = `${m}:${s}`;
            document.getElementById('timer-display').innerText = txt;
            document.getElementById('big-timer-display').innerText = txt;
        }

        function updateTodayFocusDisplay() {
            const mins = Math.floor(todayFocusMs / 60000);
            document.getElementById('today-focus-stat').innerText = `Today's Focus: ${mins} mins (Synced with Android Leaderboard)`;
        }

        function pushFocusStatsToCloud() {
            if (!firebaseDb || !syncEmail) return;
            const sanitized = sanitizeEmail(syncEmail);
            const deviceKey = 'WIN_DESKTOP_' + window.location.hostname;
            
            // Update Leaderboard node matching Android DevicePresenceManager
            firebaseDb.ref('FOCUS_TIMMER/LEADERBOARD/' + sanitized).update({
                email: syncEmail,
                displayName: 'Windows PC User',
                Todays_Focus_Ms: todayFocusMs,
                Last_Updated: Date.now()
            });

            // Update Device Focus Stats
            firebaseDb.ref('FOCUS_TIMMER/USER/' + sanitized + '/DEVICES_LOGGED_IN/' + deviceKey).update({
                Todays_Focus_Ms: todayFocusMs,
                Last_Update_Time_and_Date: new Date().toISOString()
            });
        }

        // UTILITY FUNCTIONS
        function escapeHtml(text) {
            if (!text) return '';
            return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
        }

        function hashCode(str) {
            let hash = 0;
            for (let i = 0; i < str.length; i++) {
                hash = (hash << 5) - hash + str.charCodeAt(i);
                hash |= 0;
            }
            return hash;
        }
    </script>
</body>
</html>";
        }
    }

    public class LifeOSForm : Form
    {
        private WebBrowser browser;

        public LifeOSForm(string url)
        {
            this.Text = "Life OS - Personal Operating System (Windows Desktop)";
            this.Size = new Size(1300, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            this.BackColor = Color.FromArgb(15, 23, 42);

            // Create Top Control Bar
            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 40;
            topBar.BackColor = Color.FromArgb(9, 13, 22);

            Button btnRefresh = new Button();
            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.ForeColor = Color.White;
            btnRefresh.BackColor = Color.FromArgb(30, 41, 59);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Size = new Size(90, 28);
            btnRefresh.Location = new Point(10, 6);
            btnRefresh.Click += (s, e) => { if (browser != null) browser.Refresh(); };

            Button btnHome = new Button();
            btnHome.Text = "🏠 Home";
            btnHome.ForeColor = Color.White;
            btnHome.BackColor = Color.FromArgb(30, 41, 59);
            btnHome.FlatStyle = FlatStyle.Flat;
            btnHome.FlatAppearance.BorderSize = 0;
            btnHome.Size = new Size(80, 28);
            btnHome.Location = new Point(110, 6);
            btnHome.Click += (s, e) => { if (browser != null) browser.Navigate(url); };

            Label lblTitle = new Label();
            lblTitle.Text = "⚡ Life OS Native Desktop Engine (Android Device Sync Protocol Active)";
            lblTitle.ForeColor = Color.FromArgb(56, 189, 248);
            lblTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(200, 10);

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
