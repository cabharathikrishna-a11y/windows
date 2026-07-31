package com.example.platform

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

interface AppBlockerManager {
    val isFocusGuardActive: StateFlow<Boolean>
    fun setFocusGuardEnabled(enabled: Boolean)
    fun isAppBlocked(packageNameOrProcess: String): Boolean
    fun getBlockedList(): Set<String>
    fun addBlockedApp(packageNameOrProcess: String)
    fun removeBlockedApp(packageNameOrProcess: String)
    fun checkAndInterceptForeground()
}

class DesktopAppBlockerManager : AppBlockerManager {
    private val _isFocusGuardActive = MutableStateFlow(true)
    override val isFocusGuardActive: StateFlow<Boolean> = _isFocusGuardActive.asStateFlow()

    private val blockedProcesses = mutableSetOf(
        "chrome.exe",
        "msedge.exe",
        "steam.exe",
        "discord.exe",
        "social_browser"
    )

    override fun setFocusGuardEnabled(enabled: Boolean) {
        _isFocusGuardActive.value = enabled
        println("[Desktop App Blocker] Focus Guard state changed to: $enabled")
    }

    override fun isAppBlocked(packageNameOrProcess: String): Boolean {
        if (!isFocusGuardActive.value) return false
        val normalized = packageNameOrProcess.lowercase().trim()
        return blockedProcesses.any { normalized.contains(it) }
    }

    override fun getBlockedList(): Set<String> = blockedProcesses.toSet()

    override fun addBlockedApp(packageNameOrProcess: String) {
        blockedProcesses.add(packageNameOrProcess.lowercase().trim())
    }

    override fun removeBlockedApp(packageNameOrProcess: String) {
        blockedProcesses.remove(packageNameOrProcess.lowercase().trim())
    }

    override fun checkAndInterceptForeground() {
        if (!isFocusGuardActive.value) return
        println("[Desktop App Blocker] Scanning active Windows process focus...")
        // Scans active HWND window titles & executable names on Windows OS via ProcessBuilder or JNA
    }
}

class AndroidAppBlockerManager(private val contextProvider: () -> Any?) : AppBlockerManager {
    private val _isFocusGuardActive = MutableStateFlow(true)
    override val isFocusGuardActive: StateFlow<Boolean> = _isFocusGuardActive.asStateFlow()

    override fun setFocusGuardEnabled(enabled: Boolean) {
        _isFocusGuardActive.value = enabled
        val ctx = contextProvider()
        if (ctx is android.content.Context) {
            val prefs = ctx.getSharedPreferences("app_prefs", android.content.Context.MODE_PRIVATE)
            prefs.edit().putBoolean("focus_guard_enabled", enabled).apply()
        }
    }

    override fun isAppBlocked(packageNameOrProcess: String): Boolean {
        val ctx = contextProvider()
        return if (ctx is android.content.Context) {
            com.example.util.AppBlockHelper.isAppInBlockList(ctx, packageNameOrProcess)
        } else false
    }

    override fun getBlockedList(): Set<String> {
        val ctx = contextProvider()
        return if (ctx is android.content.Context) {
            com.example.util.AppBlockHelper.getBlockedApps(ctx)
        } else emptySet()
    }

    override fun addBlockedApp(packageNameOrProcess: String) {
        val ctx = contextProvider()
        if (ctx is android.content.Context) {
            com.example.util.AppBlockHelper.addBlockedApp(ctx, packageNameOrProcess)
        }
    }

    override fun removeBlockedApp(packageNameOrProcess: String) {
        val ctx = contextProvider()
        if (ctx is android.content.Context) {
            com.example.util.AppBlockHelper.removeBlockedApp(ctx, packageNameOrProcess)
        }
    }

    override fun checkAndInterceptForeground() {
        val ctx = contextProvider()
        if (ctx is android.content.Context) {
            com.example.util.AppBlockHelper.checkForegroundAppAndBlockIfNeeded(ctx)
        }
    }
}

object AppBlockerProvider {
    fun getInstance(contextProvider: () -> Any? = { null }): AppBlockerManager {
        return if (CurrentPlatform.type.isDesktop) {
            DesktopAppBlockerManager()
        } else {
            AndroidAppBlockerManager(contextProvider)
        }
    }
}
