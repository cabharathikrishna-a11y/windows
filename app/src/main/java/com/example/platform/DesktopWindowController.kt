package com.example.platform

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

enum class WindowMode {
    NORMAL,
    MINIMIZED,
    MAXIMIZED,
    MINIMIZED_TO_TRAY,
    IMMERSIVE_FULLSCREEN
}

class DesktopWindowController {
    private val _windowMode = MutableStateFlow(WindowMode.NORMAL)
    val windowMode: StateFlow<WindowMode> = _windowMode.asStateFlow()

    private val _windowTitle = MutableStateFlow("Life OS - All-in-One Focus & Productivity")
    val windowTitle: StateFlow<String> = _windowTitle.asStateFlow()

    fun setWindowTitle(title: String) {
        _windowTitle.value = title
    }

    fun minimizeToTray() {
        _windowMode.value = WindowMode.MINIMIZED_TO_TRAY
        println("[Desktop Window Controller] Window minimized to Windows System Tray.")
    }

    fun restoreWindow() {
        _windowMode.value = WindowMode.NORMAL
        println("[Desktop Window Controller] Window restored to foreground.")
    }

    fun toggleMaximize() {
        _windowMode.value = if (_windowMode.value == WindowMode.MAXIMIZED) {
            WindowMode.NORMAL
        } else {
            WindowMode.MAXIMIZED
        }
    }

    fun setImmersiveMode(enabled: Boolean) {
        _windowMode.value = if (enabled) WindowMode.IMMERSIVE_FULLSCREEN else WindowMode.NORMAL
    }
}

object DesktopWindowProvider {
    val controller: DesktopWindowController by lazy {
        DesktopWindowController()
    }
}
