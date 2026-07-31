package com.example.platform

import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.MutableStateFlow

interface PlatformService {
    fun getPlatformName(): String
    fun isDesktop(): Boolean
    fun showNotification(title: String, message: String)
    fun requestForegroundExecution(taskName: String)
    fun stopForegroundExecution()
    fun isAppBlockerSupported(): Boolean
}

class DesktopPlatformService : PlatformService {
    override fun getPlatformName(): String = "Windows Desktop (Compose Multiplatform)"
    override fun isDesktop(): Boolean = true

    override fun showNotification(title: String, message: String) {
        println("[Windows Desktop System Tray Notification] $title - $message")
        // Invokes native Windows toast notification API or Swing SystemTray if available
        try {
            if (java.awt.SystemTray.isSupported()) {
                val tray = java.awt.SystemTray.getSystemTray()
                if (tray.trayIcons.isNotEmpty()) {
                    tray.trayIcons[0].displayMessage(title, message, java.awt.TrayIcon.MessageType.INFO)
                }
            }
        } catch (e: Throwable) {
            println("[Desktop Notification Fallback] $title: $message")
        }
    }

    override fun requestForegroundExecution(taskName: String) {
        println("[Desktop Background Daemon] Focus task running in background loop: $taskName")
    }

    override fun stopForegroundExecution() {
        println("[Desktop Background Daemon] Focus task ended.")
    }

    override fun isAppBlockerSupported(): Boolean = true
}

class AndroidPlatformService(private val contextProvider: () -> Any? = { com.example.api.Firebase.appContext }) : PlatformService {
    override fun getPlatformName(): String = "Android OS"
    override fun isDesktop(): Boolean = false

    override fun showNotification(title: String, message: String) {
        val ctx = contextProvider()
        if (ctx is android.content.Context) {
            com.example.util.UrgentNotificationHelper.showHighPriorityNotification(
                ctx,
                title = title,
                content = message
            )
        }
    }

    override fun requestForegroundExecution(taskName: String) {
        val ctx = contextProvider()
        if (ctx is android.content.Context) {
            try {
                val intent = android.content.Intent(ctx, com.example.service.FocusForegroundService::class.java)
                if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
                    ctx.startForegroundService(intent)
                } else {
                    ctx.startService(intent)
                }
            } catch (e: Exception) {
                android.util.Log.e("AndroidPlatformService", "Failed to start FocusForegroundService: ${e.message}")
            }
        }
    }

    override fun stopForegroundExecution() {
        val ctx = contextProvider()
        if (ctx is android.content.Context) {
            try {
                val intent = android.content.Intent(ctx, com.example.service.FocusForegroundService::class.java)
                ctx.stopService(intent)
            } catch (e: Exception) {
                android.util.Log.e("AndroidPlatformService", "Failed to stop FocusForegroundService: ${e.message}")
            }
        }
    }

    override fun isAppBlockerSupported(): Boolean = true
}

object PlatformProvider {
    val currentService: PlatformService by lazy {
        if (CurrentPlatform.type.isDesktop) {
            DesktopPlatformService()
        } else {
            AndroidPlatformService()
        }
    }
}
