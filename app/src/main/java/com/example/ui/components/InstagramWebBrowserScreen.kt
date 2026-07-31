package com.example.ui.components

import android.annotation.SuppressLint
import android.content.Context
import android.graphics.Bitmap
import android.view.ViewGroup
import android.webkit.WebChromeClient
import android.webkit.WebResourceRequest
import android.webkit.WebSettings
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.Toast
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import com.example.ui.AppViewModel

@OptIn(ExperimentalMaterial3Api::class)
@SuppressLint("SetJavaScriptEnabled")
@Composable
fun InstagramWebBrowserScreen(
    viewModel: AppViewModel,
    onBack: () -> Unit
) {
    val context = LocalContext.current
    var webViewInstance by remember { mutableStateOf<WebView?>(null) }
    var isLoading by remember { mutableStateOf(true) }
    var showAntiGramInfoDialog by remember { mutableStateOf(false) }

    val antiGramJs = """
        (function() {
            if (!document.getElementById('antigram-styles')) {
                var style = document.createElement('style');
                style.id = 'antigram-styles';
                style.textContent = `
                    a[href*="/reels/"],
                    a[href*="/reel/"],
                    a[href*="/explore/reels/"],
                    a[aria-label*="Reels"],
                    a[aria-label*="reels"],
                    svg[aria-label="Reels"],
                    svg[aria-label="reels"],
                    div[aria-label*="Reels"],
                    div[role="dialog"]:has(a[href*="/reel/"]) {
                        display: none !important;
                    }
                    article:has(a[href*="/reel/"]),
                    section:has(a[href*="/reel/"]) {
                        display: none !important;
                    }
                `;
                (document.head || document.documentElement).appendChild(style);
            }

            function blockReelsAndClean() {
                if (window.location.pathname.startsWith('/reels') || window.location.pathname.startsWith('/reel')) {
                    window.location.href = 'https://www.instagram.com/';
                    return;
                }

                var links = document.querySelectorAll('a[href*="/reels/"], a[href*="/reel/"], a[href*="/explore/reels/"]');
                for (var i = 0; i < links.length; i++) {
                    var el = links[i];
                    var navItem = el.closest('div[role="listitem"]') || el.closest('li') || el.parentElement;
                    if (navItem) {
                        navItem.style.display = 'none';
                    } else {
                        el.style.display = 'none';
                    }
                }

                var articles = document.querySelectorAll('article');
                for (var j = 0; j < articles.length; j++) {
                    if (articles[j].querySelector('a[href*="/reel/"]')) {
                        articles[j].style.display = 'none';
                    }
                }
            }

            blockReelsAndClean();

            if (!window.__antigramObserver) {
                window.__antigramObserver = new MutationObserver(function() {
                    blockReelsAndClean();
                });
                window.__antigramObserver.observe(document.body || document.documentElement, {
                    childList: true,
                    subtree: true
                });
            }
        })();
    """.trimIndent()

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(
                            text = "Instagram",
                            color = Color.White,
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Bold
                        )
                        Box(
                            modifier = Modifier
                                .background(Color(0xFFE1306C).copy(alpha = 0.2f), RoundedCornerShape(12.dp))
                                .border(1.dp, Color(0xFFE1306C).copy(alpha = 0.5f), RoundedCornerShape(12.dp))
                                .padding(horizontal = 8.dp, vertical = 3.dp)
                        ) {
                            Text(
                                text = "AntiGram Active 🛡️",
                                color = Color(0xFFE1306C),
                                fontSize = 10.sp,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                },
                navigationIcon = {
                    IconButton(
                        onClick = {
                            if (webViewInstance?.canGoBack() == true) {
                                webViewInstance?.goBack()
                            } else {
                                onBack()
                            }
                        },
                        modifier = Modifier.testTag("instagram_browser_back_btn")
                    ) {
                        Icon(
                            imageVector = Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = "Back",
                            tint = Color.White
                        )
                    }
                },
                actions = {
                    IconButton(
                        onClick = {
                            webViewInstance?.reload()
                        },
                        modifier = Modifier.testTag("instagram_browser_reload_btn")
                    ) {
                        Icon(
                            imageVector = Icons.Default.Refresh,
                            contentDescription = "Reload",
                            tint = Color.White
                        )
                    }
                    IconButton(
                        onClick = { showAntiGramInfoDialog = true },
                        modifier = Modifier.testTag("instagram_browser_info_btn")
                    ) {
                        Icon(
                            imageVector = Icons.Default.Info,
                            contentDescription = "AntiGram Info",
                            tint = Color.White
                        )
                    }
                    IconButton(
                        onClick = onBack,
                        modifier = Modifier.testTag("instagram_browser_close_btn")
                    ) {
                        Icon(
                            imageVector = Icons.Default.Close,
                            contentDescription = "Close",
                            tint = Color.White
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = Color(0xFF111111)
                )
            )
        },
        containerColor = Color.Black
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            AndroidView(
                modifier = Modifier
                    .fillMaxSize()
                    .testTag("instagram_webview"),
                factory = { ctx ->
                    WebView(ctx).apply {
                        layoutParams = ViewGroup.LayoutParams(
                            ViewGroup.LayoutParams.MATCH_PARENT,
                            ViewGroup.LayoutParams.MATCH_PARENT
                        )
                        settings.apply {
                            javaScriptEnabled = true
                            domStorageEnabled = true
                            databaseEnabled = true
                            useWideViewPort = true
                            loadWithOverviewMode = true
                            allowFileAccess = false
                            mixedContentMode = WebSettings.MIXED_CONTENT_NEVER_ALLOW
                            userAgentString = "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Mobile Safari/537.36"
                        }
                        webViewClient = object : WebViewClient() {
                            override fun onPageStarted(view: WebView?, url: String?, favicon: Bitmap?) {
                                super.onPageStarted(view, url, favicon)
                                isLoading = true
                                if (url != null && (url.contains("/reels") || url.contains("/reel/"))) {
                                    view?.loadUrl("https://www.instagram.com/")
                                } else {
                                    view?.evaluateJavascript(antiGramJs, null)
                                }
                            }

                            override fun onPageFinished(view: WebView?, url: String?) {
                                super.onPageFinished(view, url)
                                isLoading = false
                                view?.evaluateJavascript(antiGramJs, null)
                            }

                            override fun shouldOverrideUrlLoading(view: WebView?, request: WebResourceRequest?): Boolean {
                                val targetUrl = request?.url?.toString() ?: ""
                                if (targetUrl.contains("/reels") || targetUrl.contains("/reel/")) {
                                    Toast.makeText(ctx, "AntiGram: Reels are blocked 🛡️", Toast.LENGTH_SHORT).show()
                                    view?.loadUrl("https://www.instagram.com/")
                                    return true
                                }
                                return false
                            }
                        }
                        webChromeClient = WebChromeClient()
                        loadUrl("https://www.instagram.com/")
                        webViewInstance = this
                    }
                },
                update = { webView ->
                    webViewInstance = webView
                }
            )

            if (isLoading) {
                LinearProgressIndicator(
                    modifier = Modifier
                        .fillMaxWidth()
                        .align(Alignment.TopCenter),
                    color = Color(0xFFE1306C),
                    trackColor = Color(0xFF222222)
                )
            }
        }
    }

    if (showAntiGramInfoDialog) {
        AlertDialog(
            onDismissRequest = { showAntiGramInfoDialog = false },
            title = {
                Text(
                    text = "AntiGram Protection 🛡️",
                    color = Color.White,
                    fontWeight = FontWeight.Bold
                )
            },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text(
                        text = "AntiGram is an embedded focus extension built into this web browser.",
                        color = Color.LightGray,
                        fontSize = 13.sp
                    )
                    Text(
                        text = "• Blocks all Reels tabs, feed reels, and short video overlays\n" +
                                "• Redirects direct /reels/ links back to regular feed\n" +
                                "• Allows full access to regular posts, photos, and Instagram Direct Messages",
                        color = Color.Gray,
                        fontSize = 12.sp
                    )
                }
            },
            confirmButton = {
                TextButton(
                    onClick = { showAntiGramInfoDialog = false },
                    modifier = Modifier.testTag("antigram_info_dialog_ok_btn")
                ) {
                    Text("OK", color = Color(0xFFE1306C), fontWeight = FontWeight.Bold)
                }
            },
            containerColor = Color(0xFF1E1E1E)
        )
    }
}
