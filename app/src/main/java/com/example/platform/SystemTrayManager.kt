package com.example.platform

interface SystemTrayManager {
    fun setupSystemTray(onOpenApp: () -> Unit, onStartFocus: () -> Unit, onExit: () -> Unit)
    fun updateTrayTooltip(statusText: String)
    fun removeSystemTray()
}

class DesktopSystemTrayManager : SystemTrayManager {
    private var trayIcon: Any? = null

    override fun setupSystemTray(onOpenApp: () -> Unit, onStartFocus: () -> Unit, onExit: () -> Unit) {
        println("[Windows Desktop] Registering System Tray Icon with context menu...")
        try {
            if (java.awt.SystemTray.isSupported()) {
                val tray = java.awt.SystemTray.getSystemTray()
                val popup = java.awt.PopupMenu()

                val openItem = java.awt.MenuItem("Open Life OS").apply {
                    addActionListener { onOpenApp() }
                }
                val focusItem = java.awt.MenuItem("Start Quick Focus Timer").apply {
                    addActionListener { onStartFocus() }
                }
                val exitItem = java.awt.MenuItem("Exit").apply {
                    addActionListener { 
                        removeSystemTray()
                        onExit()
                    }
                }

                popup.add(openItem)
                popup.add(focusItem)
                popup.addSeparator()
                popup.add(exitItem)

                // Create a clean 16x16 tray icon image
                val img = java.awt.image.BufferedImage(16, 16, java.awt.image.BufferedImage.TYPE_INT_ARGB)
                val g2d = img.createGraphics()
                g2d.color = java.awt.Color(0x38, 0xBD, 0xF8)
                g2d.fillOval(2, 2, 12, 12)
                g2d.dispose()

                val icon = java.awt.TrayIcon(img, "Life OS Desktop - Active Focus", popup).apply {
                    isImageAutoSize = true
                    addActionListener { onOpenApp() }
                }

                tray.add(icon)
                this.trayIcon = icon
                println("[Windows Desktop] System Tray initialized successfully.")
            }
        } catch (e: Throwable) {
            println("[Windows Desktop] SystemTray error: ${e.message}")
        }
    }

    override fun updateTrayTooltip(statusText: String) {
        try {
            (trayIcon as? java.awt.TrayIcon)?.toolTip = "Life OS - $statusText"
        } catch (e: Throwable) {
            println("[Windows Desktop System Tray Status] $statusText")
        }
    }

    override fun removeSystemTray() {
        try {
            if (java.awt.SystemTray.isSupported() && trayIcon != null) {
                java.awt.SystemTray.getSystemTray().remove(trayIcon as java.awt.TrayIcon)
                trayIcon = null
            }
        } catch (e: Throwable) {
            // Ignored on teardown
        }
    }
}

class AndroidSystemTrayStub : SystemTrayManager {
    override fun setupSystemTray(onOpenApp: () -> Unit, onStartFocus: () -> Unit, onExit: () -> Unit) {
        // Android uses App Widgets and Notifications instead of Desktop SystemTray
    }

    override fun updateTrayTooltip(statusText: String) {
        // Handled via Notification / WidgetUpdater in Android
    }

    override fun removeSystemTray() {
        // Stub for Android
    }
}

object SystemTrayProvider {
    val instance: SystemTrayManager by lazy {
        if (CurrentPlatform.type.isDesktop) {
            DesktopSystemTrayManager()
        } else {
            AndroidSystemTrayStub()
        }
    }
}
