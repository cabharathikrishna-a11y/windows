package com.example.platform

enum class PlatformType {
    ANDROID,
    WINDOWS_DESKTOP,
    MACOS,
    LINUX;

    val isDesktop: Boolean
        get() = this == WINDOWS_DESKTOP || this == MACOS || this == LINUX

    val isAndroid: Boolean
        get() = this == ANDROID
}

object CurrentPlatform {
    val type: PlatformType by lazy {
        val osName = System.getProperty("os.name")?.lowercase() ?: ""
        when {
            System.getProperty("java.vendor")?.contains("Android", ignoreCase = true) == true ||
            System.getProperty("java.runtime.name")?.contains("Android", ignoreCase = true) == true -> PlatformType.ANDROID
            osName.contains("win") -> PlatformType.WINDOWS_DESKTOP
            osName.contains("mac") -> PlatformType.MACOS
            osName.contains("nix") || osName.contains("nux") || osName.contains("aix") -> PlatformType.LINUX
            else -> PlatformType.ANDROID
        }
    }
}
