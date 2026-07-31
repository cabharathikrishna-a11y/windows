package com.example.desktop

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.CropSquare
import androidx.compose.material.icons.filled.Minimize
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.platform.CurrentPlatform
import com.example.platform.DesktopWindowProvider
import com.example.platform.SystemTrayProvider
import com.example.platform.WindowMode

/**
 * Main Compose Multiplatform Desktop Entry Point for Windows / JVM runtime.
 * Executes native Skiko rendering engine and integrates with Windows System Tray.
 */
fun main() {
    println("=======================================================")
    println("      Starting Life OS - Compose Multiplatform (Desktop)    ")
    println("      Platform: ${CurrentPlatform.type.name}               ")
    println("=======================================================")

    // Initialize Windows System Tray
    val trayManager = SystemTrayProvider.instance
    val windowController = DesktopWindowProvider.controller

    trayManager.setupSystemTray(
        onOpenApp = {
            windowController.restoreWindow()
        },
        onStartFocus = {
            windowController.restoreWindow()
            println("[Desktop Quick Action] Triggered Focus Timer from System Tray")
        },
        onExit = {
            println("[Desktop System] Life OS Desktop Shutdown.")
            System.exit(0)
        }
    )

    // Note: When running in full Desktop JVM with Compose Desktop JARs:
    // androidx.compose.ui.window.application { Window(onCloseRequest = { ... }) { DesktopAppContent() } }
}

@Composable
fun DesktopTitleBar(
    title: String,
    onMinimize: () -> Unit,
    onMaximize: () -> Unit,
    onClose: () -> Unit
) {
    Surface(
        color = Color(0xFF0F172A),
        modifier = Modifier.fillMaxWidth().height(36.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxSize().padding(horizontal = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier.size(10.dp)
                        .clip(CircleShape)
                        .background(Color(0xFF38BDF8))
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = title,
                    color = Color.White,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.Medium
                )
            }

            Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                IconButton(onClick = onMinimize, modifier = Modifier.size(28.dp)) {
                    Icon(Icons.Default.Minimize, contentDescription = "Minimize", tint = Color.LightGray, modifier = Modifier.size(14.dp))
                }
                IconButton(onClick = onMaximize, modifier = Modifier.size(28.dp)) {
                    Icon(Icons.Default.CropSquare, contentDescription = "Maximize", tint = Color.LightGray, modifier = Modifier.size(14.dp))
                }
                IconButton(onClick = onClose, modifier = Modifier.size(28.dp)) {
                    Icon(Icons.Default.Close, contentDescription = "Close to Tray", tint = Color(0xFFF43F5E), modifier = Modifier.size(14.dp))
                }
            }
        }
    }
}
