package com.example.widget

import android.app.PendingIntent
import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.os.Build
import android.util.Log
import android.util.SizeF
import android.widget.RemoteViews
import com.example.MainActivity
import com.example.R
import com.example.util.FocusTimerManager

object WidgetUpdater {

    fun getPendingIntentFlags(isMutable: Boolean = false): Int {
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            if (isMutable) PendingIntent.FLAG_MUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
            else PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        } else {
            PendingIntent.FLAG_UPDATE_CURRENT
        }
    }

    /**
     * Programmatically requests the Android Launcher to pin a widget to the Home Screen (Android 8.0+ / API 26+)
     */
    fun requestPinWidget(context: Context, providerClass: Class<*>) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val appWidgetManager = AppWidgetManager.getInstance(context)
            if (appWidgetManager.isRequestPinAppWidgetSupported) {
                val myProvider = ComponentName(context, providerClass)
                val successCallback = PendingIntent.getBroadcast(
                    context,
                    9000,
                    Intent(context, providerClass).apply { action = "com.example.widget.ACTION_WIDGET_PINNED" },
                    getPendingIntentFlags()
                )
                appWidgetManager.requestPinAppWidget(myProvider, null, successCallback)
            }
        }
    }

    /**
     * Updates the Friends Focus Widget ("Who is Focusing")
     * Displays logos/avatars of active focusing users without any text labels.
     */
    fun updateFriendsFocusWidget(context: Context, statusText: String? = null) {
        val appWidgetManager = AppWidgetManager.getInstance(context)
        val thisWidget = ComponentName(context, FriendsFocusWidgetProvider::class.java)
        val allWidgetIds = appWidgetManager.getAppWidgetIds(thisWidget)
        if (allWidgetIds.isEmpty()) return

        data class FocusingUserLogo(val name: String, val avatar: String)
        val focusingLogos = mutableListOf<FocusingUserLogo>()

        FocusTimerManager.init(context)
        val isMeFocusing = (FocusTimerManager.isTimerRunning.value || FocusTimerManager.isStopwatchActive.value)
                && FocusTimerManager.isFocusPhase.value
                && !FocusTimerManager.isPaused.value
                && FocusTimerManager.pendingFocusReview.value == null

        if (isMeFocusing) {
            val prefs = context.getSharedPreferences("app_prefs", Context.MODE_PRIVATE)
            val myName = prefs.getString("username", "")?.ifEmpty { prefs.getString("nickname", "Me") } ?: "Me"
            val myEmoji = prefs.getString("user_emoji", "") ?: ""
            focusingLogos.add(FocusingUserLogo(myName, myEmoji))
        }

        val activePeers = com.example.api.PeerLiveSphereManager.peerLiveStates.value.values.filter {
            it.status.equals("Focusing", ignoreCase = true)
        }
        activePeers.forEach { peer ->
            if (focusingLogos.none { it.name.equals(peer.displayName, ignoreCase = true) }) {
                focusingLogos.add(FocusingUserLogo(peer.displayName, peer.customEmoji ?: ""))
            }
        }

        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
            putExtra("SHOW_TIMER_PAGE", true)
        }
        val pendingIntent = PendingIntent.getActivity(context, 2001, intent, getPendingIntentFlags())

        val logoIds = arrayOf(
            R.id.focus_logo_1,
            R.id.focus_logo_2,
            R.id.focus_logo_3,
            R.id.focus_logo_4,
            R.id.focus_logo_5
        )

        for (widgetId in allWidgetIds) {
            val largeView = RemoteViews(context.packageName, R.layout.widget_friends_focus).apply {
                setOnClickPendingIntent(R.id.widget_root, pendingIntent)
                if (focusingLogos.isEmpty()) {
                    setViewVisibility(R.id.focus_logo_idle, android.view.View.VISIBLE)
                    val idleBmp = createAvatarLogoBitmap(context, "🎯", "Idle", isIdle = true, sizeDp = 44)
                    setImageViewBitmap(R.id.focus_logo_idle, idleBmp)
                    for (id in logoIds) {
                        setViewVisibility(id, android.view.View.GONE)
                    }
                } else {
                    setViewVisibility(R.id.focus_logo_idle, android.view.View.GONE)
                    for (i in logoIds.indices) {
                        if (i < focusingLogos.size) {
                            val logo = focusingLogos[i]
                            val bmp = createAvatarLogoBitmap(context, logo.avatar, logo.name, isIdle = false, sizeDp = 44)
                            setImageViewBitmap(logoIds[i], bmp)
                            setViewVisibility(logoIds[i], android.view.View.VISIBLE)
                        } else {
                            setViewVisibility(logoIds[i], android.view.View.GONE)
                        }
                    }
                }
            }

            val finalViews = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                val smallLogoIds = arrayOf(
                    R.id.focus_logo_1,
                    R.id.focus_logo_2,
                    R.id.focus_logo_3,
                    R.id.focus_logo_4
                )
                val smallView = RemoteViews(context.packageName, R.layout.widget_friends_focus_small).apply {
                    setOnClickPendingIntent(android.R.id.background, pendingIntent)
                    if (focusingLogos.isEmpty()) {
                        setViewVisibility(R.id.focus_logo_idle, android.view.View.VISIBLE)
                        val idleBmp = createAvatarLogoBitmap(context, "🎯", "Idle", isIdle = true, sizeDp = 36)
                        setImageViewBitmap(R.id.focus_logo_idle, idleBmp)
                        for (id in smallLogoIds) {
                            setViewVisibility(id, android.view.View.GONE)
                        }
                    } else {
                        setViewVisibility(R.id.focus_logo_idle, android.view.View.GONE)
                        for (i in smallLogoIds.indices) {
                            if (i < focusingLogos.size) {
                                val logo = focusingLogos[i]
                                val bmp = createAvatarLogoBitmap(context, logo.avatar, logo.name, isIdle = false, sizeDp = 36)
                                setImageViewBitmap(smallLogoIds[i], bmp)
                                setViewVisibility(smallLogoIds[i], android.view.View.VISIBLE)
                            } else {
                                setViewVisibility(smallLogoIds[i], android.view.View.GONE)
                            }
                        }
                    }
                }
                val viewMap = mapOf(
                    SizeF(140f, 50f) to smallView,
                    SizeF(200f, 80f) to largeView
                )
                RemoteViews(viewMap)
            } else {
                largeView
            }

            appWidgetManager.updateAppWidget(widgetId, finalViews)
        }
    }

    private fun createAvatarLogoBitmap(
        context: Context,
        logoTextOrEmoji: String,
        displayName: String,
        isIdle: Boolean = false,
        sizeDp: Int = 44
    ): android.graphics.Bitmap {
        val density = context.resources.displayMetrics.density
        val px = (sizeDp * density).toInt().coerceAtLeast(32)
        val bitmap = android.graphics.Bitmap.createBitmap(px, px, android.graphics.Bitmap.Config.ARGB_8888)
        val canvas = android.graphics.Canvas(bitmap)

        val radius = px / 2f

        val bgPaint = android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.FILL
            color = if (isIdle) {
                android.graphics.Color.argb(50, 255, 255, 255)
            } else {
                val colors = intArrayOf(
                    android.graphics.Color.rgb(16, 185, 129),
                    android.graphics.Color.rgb(59, 130, 246),
                    android.graphics.Color.rgb(139, 92, 246),
                    android.graphics.Color.rgb(236, 72, 153),
                    android.graphics.Color.rgb(245, 158, 11)
                )
                val colorIndex = Math.abs(displayName.hashCode()) % colors.size
                colors[colorIndex]
            }
        }

        val borderPaint = android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.STROKE
            strokeWidth = 2f * density
            color = if (isIdle) android.graphics.Color.argb(80, 255, 255, 255) else android.graphics.Color.rgb(16, 185, 129)
        }

        canvas.drawCircle(radius, radius, radius - (1f * density), bgPaint)
        canvas.drawCircle(radius, radius, radius - (1f * density), borderPaint)

        val textToDraw = when {
            isIdle -> "🎯"
            logoTextOrEmoji.isNotEmpty() && logoTextOrEmoji != "👤" -> logoTextOrEmoji
            displayName.isNotBlank() -> {
                val parts = displayName.trim().split(" ")
                if (parts.size >= 2) {
                    "${parts[0].take(1)}${parts[1].take(1)}".uppercase()
                } else {
                    displayName.take(2).uppercase()
                }
            }
            else -> "👤"
        }

        val isEmoji = textToDraw.any { 
            Character.getType(it) == Character.SURROGATE.toInt() || 
            Character.getType(it) == Character.OTHER_SYMBOL.toInt() 
        } || textToDraw == "🎯" || textToDraw == "👤"

        val textPaint = android.text.TextPaint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            color = android.graphics.Color.WHITE
            textAlign = android.graphics.Paint.Align.CENTER
            textSize = if (isEmoji) radius * 1.0f else radius * 0.75f
            typeface = android.graphics.Typeface.create(android.graphics.Typeface.DEFAULT, android.graphics.Typeface.BOLD)
        }

        val fontMetrics = textPaint.fontMetrics
        val baseline = radius - (fontMetrics.ascent + fontMetrics.descent) / 2f
        canvas.drawText(textToDraw, radius, baseline, textPaint)

        return bitmap
    }

    private fun formatTime(seconds: Int): String {
        val hrs = seconds / 3600
        val mins = (seconds % 3600) / 60
        val secs = seconds % 60
        return if (hrs > 0) {
            String.format(java.util.Locale.US, "%02d:%02d:%02d", hrs, mins, secs)
        } else {
            String.format(java.util.Locale.US, "%02d:%02d", mins, secs)
        }
    }

    /**
     * Updates the Stopwatch Widget using Chronometer and responsive layouts (Android 12+ API 31+)
     */
    fun updateStopwatchWidget(context: Context, isPartialUpdate: Boolean = false) {
        val appWidgetManager = AppWidgetManager.getInstance(context)
        val thisWidget = ComponentName(context, TimerStopwatchWidgetProvider::class.java)
        val allWidgetIds = appWidgetManager.getAppWidgetIds(thisWidget)
        if (allWidgetIds.isEmpty()) return

        FocusTimerManager.init(context)
        val isStopwatchActive = FocusTimerManager.isStopwatchActive.value
        val isPaused = FocusTimerManager.isPaused.value
        val wasStartedFromStopwatch = FocusTimerManager.wasStartedFromStopwatch.value

        // Stopwatch mode is only active if stopwatch is running OR if session was started from stopwatch and is paused
        val isStopwatchMode = isStopwatchActive || (isPaused && wasStartedFromStopwatch)
        val isRunning = isStopwatchActive && !isPaused

        val baseAccumulatedMs = if (isStopwatchMode) FocusTimerManager.accumulatedSessionTimeMs.value else 0L
        val seconds = if (isStopwatchMode) (baseAccumulatedMs / 1000).toInt() else 0

        val startPauseIntent = Intent(context, TimerStopwatchWidgetProvider::class.java).apply {
            action = "com.example.widget.ACTION_STOPWATCH_START_PAUSE"
        }
        val startPausePending = PendingIntent.getBroadcast(context, 3001, startPauseIntent, getPendingIntentFlags())

        val breakIntent = Intent(context, TimerStopwatchWidgetProvider::class.java).apply {
            action = "com.example.widget.ACTION_STOPWATCH_BREAK"
        }
        val breakPending = PendingIntent.getBroadcast(context, 3004, breakIntent, getPendingIntentFlags())

        val resetIntent = Intent(context, TimerStopwatchWidgetProvider::class.java).apply {
            action = "com.example.widget.ACTION_STOPWATCH_RESET"
        }
        val resetPending = PendingIntent.getBroadcast(context, 3002, resetIntent, getPendingIntentFlags())

        val rootIntent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
            putExtra("SHOW_TIMER_PAGE", true)
        }
        val rootPending = PendingIntent.getActivity(context, 3003, rootIntent, getPendingIntentFlags())

        val btnStartPauseText = if (isRunning) "⏸ PAUSE" else if (isPaused && wasStartedFromStopwatch) "▶ RESUME" else "▶ START"
        val btnResetText = if (isRunning || (isPaused && wasStartedFromStopwatch) || seconds > 0) "◼ END" else "◼ RESET"

        val lastResumeRealtime = FocusTimerManager.activeSessionStartRealtimeMs.let {
            if (it > 0L) it else android.os.SystemClock.elapsedRealtime()
        }
        val runningBaseTime = lastResumeRealtime - baseAccumulatedMs

        for (widgetId in allWidgetIds) {
            val largeView = RemoteViews(context.packageName, R.layout.widget_stopwatch).apply {
                if (isRunning) {
                    setChronometer(R.id.stopwatch_time_display, runningBaseTime, null, true)
                } else {
                    val staticText = formatTime(seconds)
                    setChronometer(R.id.stopwatch_time_display, 0L, null, false)
                    setTextViewText(R.id.stopwatch_time_display, staticText)
                }

                setTextViewText(R.id.btn_stopwatch_start_pause, btnStartPauseText)
                setOnClickPendingIntent(R.id.btn_stopwatch_start_pause, startPausePending)

                setTextViewText(R.id.btn_stopwatch_reset, btnResetText)
                setOnClickPendingIntent(R.id.btn_stopwatch_reset, resetPending)
                if (isRunning || (isPaused && wasStartedFromStopwatch) || seconds > 0) {
                    setViewVisibility(R.id.btn_stopwatch_reset, android.view.View.VISIBLE)
                } else {
                    setViewVisibility(R.id.btn_stopwatch_reset, android.view.View.GONE)
                }

                if (isRunning) {
                    setViewVisibility(R.id.btn_stopwatch_break, android.view.View.VISIBLE)
                    setOnClickPendingIntent(R.id.btn_stopwatch_break, breakPending)
                } else {
                    setViewVisibility(R.id.btn_stopwatch_break, android.view.View.GONE)
                }

                setOnClickPendingIntent(R.id.stopwatch_title, rootPending)
                setOnClickPendingIntent(R.id.stopwatch_time_display, rootPending)
            }

            if (isPartialUpdate) {
                appWidgetManager.partiallyUpdateAppWidget(widgetId, largeView)
                continue
            }

            val finalViews = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                val smallView = RemoteViews(context.packageName, R.layout.widget_stopwatch_small).apply {
                    if (isRunning) {
                        setChronometer(R.id.stopwatch_time_display, runningBaseTime, null, true)
                    } else {
                        val staticText = formatTime(seconds)
                        setChronometer(R.id.stopwatch_time_display, 0L, null, false)
                        setTextViewText(R.id.stopwatch_time_display, staticText)
                    }

                    setTextViewText(R.id.btn_stopwatch_start_pause, btnStartPauseText)
                    setOnClickPendingIntent(R.id.btn_stopwatch_start_pause, startPausePending)
                    setOnClickPendingIntent(R.id.stopwatch_time_display, rootPending)
                }
                val viewMap = mapOf(
                    SizeF(140f, 70f) to smallView,
                    SizeF(200f, 100f) to largeView
                )
                RemoteViews(viewMap)
            } else {
                largeView
            }

            appWidgetManager.updateAppWidget(widgetId, finalViews)
        }
    }

    /**
     * Updates the Pomodoro Widget using countdown Chronometer and responsive layouts (Android 12+ API 31+)
     */
    fun updatePomodoroWidget(context: Context, isPartialUpdate: Boolean = false) {
        val appWidgetManager = AppWidgetManager.getInstance(context)
        val thisWidget = ComponentName(context, PomodoroWidgetProvider::class.java)
        val allWidgetIds = appWidgetManager.getAppWidgetIds(thisWidget)
        if (allWidgetIds.isEmpty()) return

        FocusTimerManager.init(context)
        val isTimerRunning = FocusTimerManager.isTimerRunning.value
        val isPaused = FocusTimerManager.isPaused.value
        val wasStartedFromStopwatch = FocusTimerManager.wasStartedFromStopwatch.value
        val isFocus = FocusTimerManager.isFocusPhase.value

        val isPomodoroMode = isTimerRunning || (isPaused && !wasStartedFromStopwatch)
        val isRunning = isTimerRunning && !isPaused

        val totalDurationMs = if (isFocus) {
            FocusTimerManager.timerDurationMinutes.value * 60 * 1000L
        } else {
            val prefs = context.getSharedPreferences("app_prefs", Context.MODE_PRIVATE)
            val bMins = prefs.getInt("break_duration", 5)
            bMins * 60 * 1000L
        }

        val baseAccumulatedMs = if (isPomodoroMode) FocusTimerManager.accumulatedSessionTimeMs.value else 0L

        val lastResumeRealtime = FocusTimerManager.activeSessionStartRealtimeMs.let {
            if (it > 0L) it else android.os.SystemClock.elapsedRealtime()
        }

        val runningBaseTime = lastResumeRealtime + (totalDurationMs - baseAccumulatedMs)

        val displaySecs = if (isRunning) {
            val elapsedMs = baseAccumulatedMs + (android.os.SystemClock.elapsedRealtime() - lastResumeRealtime)
            maxOf(0, ((totalDurationMs - elapsedMs) / 1000).toInt())
        } else if (isPaused && !wasStartedFromStopwatch) {
            maxOf(0, ((totalDurationMs - baseAccumulatedMs) / 1000).toInt())
        } else {
            (totalDurationMs / 1000).toInt()
        }

        val headerText = if (isFocus) "POMODORO FOCUS 🎯" else "REST BREAK ☕"
        val headerColor = if (isFocus) 0xFF30D158.toInt() else 0xFFFF9500.toInt()
        val btnStartPauseText = if (isRunning) "⏸ PAUSE" else if (isPaused && !wasStartedFromStopwatch) "▶ RESUME" else "▶ START"
        val btnBreakText = if (isFocus) "☕ BREAK" else "⏭ FOCUS"
        val btnResetText = if (isRunning || (isPaused && !wasStartedFromStopwatch)) "◼ END" else "◼ RESET"

        val startPauseIntent = Intent(context, PomodoroWidgetProvider::class.java).apply {
            action = "com.example.widget.ACTION_POMO_START_PAUSE"
        }
        val startPausePending = PendingIntent.getBroadcast(context, 4001, startPauseIntent, getPendingIntentFlags())

        val breakIntent = Intent(context, PomodoroWidgetProvider::class.java).apply {
            action = "com.example.widget.ACTION_POMO_BREAK"
        }
        val breakPending = PendingIntent.getBroadcast(context, 4004, breakIntent, getPendingIntentFlags())

        val resetIntent = Intent(context, PomodoroWidgetProvider::class.java).apply {
            action = "com.example.widget.ACTION_POMO_RESET"
        }
        val resetPending = PendingIntent.getBroadcast(context, 4002, resetIntent, getPendingIntentFlags())

        val rootIntent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
            putExtra("SHOW_TIMER_PAGE", true)
        }
        val rootPending = PendingIntent.getActivity(context, 4003, rootIntent, getPendingIntentFlags())

        for (widgetId in allWidgetIds) {
            val largeView = RemoteViews(context.packageName, R.layout.widget_pomodoro).apply {
                setTextViewText(R.id.pomo_title, headerText)
                setTextColor(R.id.pomo_title, headerColor)

                if (isRunning) {
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                        setChronometerCountDown(R.id.pomo_time_display, true)
                    }
                    setChronometer(R.id.pomo_time_display, runningBaseTime, null, true)
                } else {
                    val staticText = formatTime(displaySecs)
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                        setChronometerCountDown(R.id.pomo_time_display, false)
                    }
                    setChronometer(R.id.pomo_time_display, 0L, null, false)
                    setTextViewText(R.id.pomo_time_display, staticText)
                }

                setTextViewText(R.id.btn_pomo_start_pause, btnStartPauseText)
                setOnClickPendingIntent(R.id.btn_pomo_start_pause, startPausePending)

                setTextViewText(R.id.btn_pomo_reset, btnResetText)
                setOnClickPendingIntent(R.id.btn_pomo_reset, resetPending)

                if (isRunning || !isFocus) {
                    setViewVisibility(R.id.btn_pomo_break, android.view.View.VISIBLE)
                    setTextViewText(R.id.btn_pomo_break, btnBreakText)
                    setOnClickPendingIntent(R.id.btn_pomo_break, breakPending)
                } else {
                    setViewVisibility(R.id.btn_pomo_break, android.view.View.GONE)
                }

                setOnClickPendingIntent(R.id.pomo_title, rootPending)
                setOnClickPendingIntent(R.id.pomo_time_display, rootPending)
            }

            if (isPartialUpdate) {
                appWidgetManager.partiallyUpdateAppWidget(widgetId, largeView)
                continue
            }

            val finalViews = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                val smallView = RemoteViews(context.packageName, R.layout.widget_pomodoro_small).apply {
                    setTextViewText(R.id.pomo_title, headerText)
                    setTextColor(R.id.pomo_title, headerColor)

                    if (isRunning) {
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                            setChronometerCountDown(R.id.pomo_time_display, true)
                        }
                        setChronometer(R.id.pomo_time_display, runningBaseTime, null, true)
                    } else {
                        val staticText = formatTime(displaySecs)
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                            setChronometerCountDown(R.id.pomo_time_display, false)
                        }
                        setChronometer(R.id.pomo_time_display, 0L, null, false)
                        setTextViewText(R.id.pomo_time_display, staticText)
                    }

                    setTextViewText(R.id.btn_pomo_start_pause, btnStartPauseText)
                    setOnClickPendingIntent(R.id.btn_pomo_start_pause, startPausePending)
                    setOnClickPendingIntent(R.id.pomo_time_display, rootPending)
                }
                val viewMap = mapOf(
                    SizeF(140f, 70f) to smallView,
                    SizeF(200f, 100f) to largeView
                )
                RemoteViews(viewMap)
            } else {
                largeView
            }

            appWidgetManager.updateAppWidget(widgetId, finalViews)
        }
    }

    /**
     * Forces full updates across all widgets
     */
    fun updateAllWidgets(context: Context) {
        try {
            updateFriendsFocusWidget(context)
            updateStopwatchWidget(context)
            updatePomodoroWidget(context)
        } catch (e: Exception) {
            Log.e("WidgetUpdater", "Error updating widgets: ${e.message}")
        }
    }
}
