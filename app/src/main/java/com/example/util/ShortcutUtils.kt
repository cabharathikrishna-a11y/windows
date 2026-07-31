package com.example.util

import android.content.Context
import android.content.Intent
import android.util.Log
import androidx.core.content.pm.ShortcutInfoCompat
import androidx.core.content.pm.ShortcutManagerCompat
import androidx.core.graphics.drawable.IconCompat
import com.example.MainActivity
import com.example.R

object ShortcutUtils {

    fun createInstagramShortcut(context: Context): Boolean {
        return try {
            val intent = Intent(context, MainActivity::class.java).apply {
                action = Intent.ACTION_VIEW
                putExtra("OPEN_INSTAGRAM_WEB_APP", true)
                putExtra("NAVIGATE_TO", "INSTAGRAM_WEB_APP")
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP)
            }

            val shortcut = ShortcutInfoCompat.Builder(context, "instagram_web_app_shortcut")
                .setShortLabel("Instagram")
                .setLongLabel("Instagram (AntiGram)")
                .setIcon(IconCompat.createWithResource(context, R.mipmap.ic_launcher))
                .setIntent(intent)
                .build()

            // Push dynamic shortcut to app launcher icon menu
            ShortcutManagerCompat.pushDynamicShortcut(context, shortcut)

            // Pin to home screen if launcher supports requestPinShortcut
            if (ShortcutManagerCompat.isRequestPinShortcutSupported(context)) {
                ShortcutManagerCompat.requestPinShortcut(context, shortcut, null)
            }
            true
        } catch (e: Exception) {
            Log.e("ShortcutUtils", "Error creating Instagram shortcut", e)
            false
        }
    }
}
