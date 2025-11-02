// Blazor Server connection monitoring and error handling
window.blazorConnection = {
    connectionState: 'Unknown',
    
    init: function() {
        console.log('[Blazor] Initializing connection monitoring...');
        
        // Monitor SignalR connection
        if (window.Blazor) {
            // Listen for circuit errors
            Blazor.addEventListener('error', function(event) {
                console.error('[Blazor] Circuit error:', event.detail);
                window.blazorConnection.showConnectionError('Ett fel uppstod: ' + event.detail);
            });
            
            // Monitor connection state changes
            const originalReconnect = Blazor.reconnect;
            Blazor.reconnect = function(retryCount) {
                console.log('[Blazor] Attempting reconnect, attempt:', retryCount);
                window.blazorConnection.connectionState = 'Reconnecting';
                window.blazorConnection.updateConnectionStatus('Återansluter...');
                
                // Add timeout to prevent infinite reconnecting
                const maxRetries = 10;
                if (retryCount > maxRetries) {
                    console.error('[Blazor] Max reconnect attempts reached, reloading page to preserve authentication');
                    // Reload page instead of infinite reconnect - cookies will persist
                    setTimeout(() => {
                        window.location.reload();
                    }, 2000);
                    return;
                }
                
                return originalReconnect.apply(this, arguments);
            };
            
            // Listen for successful reconnection
            Blazor.addEventListener('reconnected', function() {
                console.log('[Blazor] Successfully reconnected!');
                window.blazorConnection.connectionState = 'Connected';
                window.blazorConnection.updateConnectionStatus('Ansluten');
            });
            
            // Listen for failed reconnection
            Blazor.addEventListener('reconnectionfailed', function() {
                console.error('[Blazor] Reconnection failed - reloading page to preserve authentication');
                // Reload to preserve auth cookies and retry connection
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            });
        }
        
        // Monitor page visibility (detect when tab is hidden/visible)
        document.addEventListener('visibilitychange', function() {
            if (document.hidden) {
                console.log('[Blazor] Page hidden');
            } else {
                console.log('[Blazor] Page visible, checking connection...');
                // Force reconnect check when page becomes visible again
                if (window.Blazor) {
                    // Check if connection is still alive
                    if (typeof Blazor.reconnect === 'function') {
                        // Try to manually trigger reconnect if needed
                        try {
                            Blazor.reconnect(0);
                            console.log('[Blazor] Manual reconnect triggered');
                        } catch (e) {
                            console.log('[Blazor] Auto-reconnect will handle it');
                        }
                    }
                }
            }
        });
        
        // Periodic connection health check (every 30 seconds)
        setInterval(function() {
            if (window.Blazor && !document.hidden) {
                // Check if connection is still alive by checking if Blazor is responsive
                // This helps detect stale connections that haven't officially disconnected
                try {
                    // Just logging - Blazor's keepalive should handle actual reconnects
                    if (window.blazorConnection.connectionState === 'Connected') {
                        console.log('[Blazor] Connection health check: OK');
                    }
                } catch (e) {
                    console.warn('[Blazor] Connection health check failed:', e);
                }
            }
        }, 30000); // Check every 30 seconds
        
        // Log when Blazor starts
        window.addEventListener('load', function() {
            console.log('[Blazor] Page loaded, Blazor should be connecting...');
            setTimeout(function() {
                if (window.Blazor) {
                    console.log('[Blazor] Blazor framework loaded');
                    window.blazorConnection.connectionState = 'Connected';
                    window.blazorConnection.updateConnectionStatus('Ansluten');
                } else {
                    console.error('[Blazor] ERROR: Blazor framework not loaded!');
                    window.blazorConnection.showConnectionError('Blazor-frameworket kunde inte laddas. Ladda om sidan.');
                }
            }, 1000);
        });
        
        console.log('[Blazor] Connection monitoring initialized');
    },
    
    showConnectionError: function(message) {
        // Create error banner if it doesn't exist
        let errorBanner = document.getElementById('blazor-connection-error');
        if (!errorBanner) {
            errorBanner = document.createElement('div');
            errorBanner.id = 'blazor-connection-error';
            errorBanner.style.cssText = 'position:fixed;top:0;left:0;right:0;background:#dc2626;color:white;padding:12px;text-align:center;z-index:9999;font-size:14px;';
            document.body.insertBefore(errorBanner, document.body.firstChild);
        }
        errorBanner.textContent = '⚠️ ' + message;
        errorBanner.style.display = 'block';
        
        // Auto-hide after 10 seconds
        setTimeout(function() {
            if (errorBanner) {
                errorBanner.style.display = 'none';
            }
        }, 10000);
    },
    
    updateConnectionStatus: function(status) {
        // Update connection status indicator if it exists
        const statusEl = document.getElementById('connection-status');
        if (statusEl) {
            statusEl.textContent = status;
        }
    },
    
    testClick: function() {
        console.log('[Blazor] Test click received - Blazor interop is working!');
        return true;
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.blazorConnection.init);
} else {
    window.blazorConnection.init();
}

