// Blazor Server connection monitoring and auto-reload handling
window.blazorConnection = {
    connectionState: 'Unknown',
    reconnectAttempts: 0,
    maxReconnectAttempts: 3,
    autoReloadEnabled: true,
    lastActivity: Date.now(),
    
    init: function() {
        console.log('[Blazor] Initializing connection monitoring with auto-reload...');
        
        // Monitor SignalR connection events
        if (window.Blazor) {
            // Listen for circuit errors
            Blazor.addEventListener('error', function(event) {
                console.error('[Blazor] Circuit error:', event.detail);
                window.blazorConnection.handleConnectionError('Circuit error: ' + event.detail);
            });
        }
        
        // Override Blazor's default reconnection UI to add auto-reload
        window.addEventListener('load', function() {
            setTimeout(function() {
                window.blazorConnection.setupReconnectionHandling();
            }, 1000);
        });
        
        // Monitor page visibility (detect when tab is hidden/visible)
        document.addEventListener('visibilitychange', function() {
            if (document.hidden) {
                console.log('[Blazor] Page hidden');
            } else {
                console.log('[Blazor] Page visible - checking connection...');
                window.blazorConnection.lastActivity = Date.now();
                // Check if we've been away for a long time (> 5 minutes)
                if (window.blazorConnection.connectionState === 'Disconnected') {
                    console.log('[Blazor] Page was hidden and connection lost - auto-reloading...');
                    window.blazorConnection.autoReload('Page became visible after disconnect');
                }
            }
        });
        
        // Enhanced connection health check
        setInterval(function() {
            window.blazorConnection.checkConnectionHealth();
        }, 15000); // Check every 15 seconds
        
        // Track user activity
        ['click', 'keydown', 'scroll', 'mousemove'].forEach(function(event) {
            document.addEventListener(event, function() {
                window.blazorConnection.lastActivity = Date.now();
            }, { passive: true });
        });
        
        console.log('[Blazor] Connection monitoring with auto-reload initialized');
    },
    
    setupReconnectionHandling: function() {
        // Listen for Blazor connection events
        if (window.Blazor) {
            console.log('[Blazor] Setting up reconnection event listeners...');
            
            // Listen for disconnection
            window.addEventListener('offline', function() {
                console.log('[Blazor] Browser went offline');
                window.blazorConnection.connectionState = 'Disconnected';
                window.blazorConnection.showReconnectingMessage();
            });
            
            window.addEventListener('online', function() {
                console.log('[Blazor] Browser came online - checking connection...');
                // Give Blazor a moment to reconnect, then check
                setTimeout(function() {
                    if (window.blazorConnection.connectionState === 'Disconnected') {
                        console.log('[Blazor] Still disconnected after coming online - auto-reloading...');
                        window.blazorConnection.autoReload('Failed to reconnect after coming online');
                    }
                }, 5000);
            });
            
            // Monitor for Blazor circuit failures
            const checkBlazorHealth = function() {
                try {
                    // Try to access Blazor's internal state
                    if (window.Blazor && typeof window.Blazor.reconnect === 'function') {
                        // Blazor is loaded and functional
                        if (window.blazorConnection.connectionState === 'Disconnected') {
                            console.log('[Blazor] Connection restored');
                            window.blazorConnection.connectionState = 'Connected';
                            window.blazorConnection.hideReconnectingMessage();
                            window.blazorConnection.reconnectAttempts = 0;
                            window.blazorConnection.disconnectedSince = null;
                        }
                    }
                } catch (e) {
                    console.warn('[Blazor] Health check error:', e);
                    if (window.blazorConnection.connectionState === 'Connected') {
                        window.blazorConnection.handleConnectionError('Health check failed');
                    }
                }
            };
            
            // Check Blazor health every 10 seconds
            setInterval(checkBlazorHealth, 10000);
            
            console.log('[Blazor] Reconnection handling configured');
            window.blazorConnection.connectionState = 'Connected';
        }
    },
    
    checkConnectionHealth: function() {
        if (document.hidden) return; // Skip when tab is hidden
        
        const timeSinceActivity = Date.now() - window.blazorConnection.lastActivity;
        
        // If user has been inactive for > 10 minutes, don't auto-reload
        if (timeSinceActivity > 10 * 60 * 1000) {
            return;
        }
        
        // Check if Blazor is responsive
        try {
            if (window.Blazor && window.blazorConnection.connectionState === 'Connected') {
                // Try to invoke a simple JS interop to test connection
                console.log('[Blazor] Connection health check: OK');
            } else if (window.blazorConnection.connectionState === 'Disconnected') {
                console.log('[Blazor] Connection health check: Disconnected');
                // If disconnected for more than 30 seconds, auto-reload
                if (!window.blazorConnection.disconnectedSince) {
                    window.blazorConnection.disconnectedSince = Date.now();
                } else if (Date.now() - window.blazorConnection.disconnectedSince > 30000) {
                    console.log('[Blazor] Disconnected for >30s - auto-reloading...');
                    window.blazorConnection.autoReload('Prolonged disconnection');
                }
            }
        } catch (e) {
            console.warn('[Blazor] Connection health check failed:', e);
            window.blazorConnection.handleConnectionError('Health check failed');
        }
    },
    
    handleConnectionError: function(reason) {
        console.error('[Blazor] Connection error:', reason);
        window.blazorConnection.connectionState = 'Disconnected';
        window.blazorConnection.reconnectAttempts++;
        
        if (window.blazorConnection.reconnectAttempts >= window.blazorConnection.maxReconnectAttempts) {
            console.log('[Blazor] Max reconnect attempts reached - auto-reloading...');
            window.blazorConnection.autoReload('Max reconnect attempts reached');
        } else {
            window.blazorConnection.showReconnectingMessage();
        }
    },
    
    autoReload: function(reason) {
        if (!window.blazorConnection.autoReloadEnabled) {
            console.log('[Blazor] Auto-reload disabled');
            return;
        }
        
        console.log('[Blazor] Auto-reloading page. Reason:', reason);
        window.blazorConnection.showReloadMessage(reason);
        
        // Reload after a short delay to show the message
        setTimeout(function() {
            window.location.reload();
        }, 1500);
    },
    
    showReconnectingMessage: function() {
        window.blazorConnection.showBanner('🔄 Återansluter...', '#f59e0b', 'blazor-reconnecting');
    },
    
    hideReconnectingMessage: function() {
        window.blazorConnection.hideBanner('blazor-reconnecting');
    },
    
    showReloadMessage: function(reason) {
        window.blazorConnection.showBanner('🔄 Laddar om sidan... (' + reason + ')', '#3b82f6', 'blazor-reloading');
    },
    
    showBanner: function(message, backgroundColor, id) {
        // Remove any existing banner
        window.blazorConnection.hideBanner(id);
        
        // Create banner
        const banner = document.createElement('div');
        banner.id = id;
        banner.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            background: ${backgroundColor};
            color: white;
            padding: 12px;
            text-align: center;
            z-index: 10000;
            font-size: 14px;
            font-weight: 500;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            animation: slideDown 0.3s ease-out;
        `;
        banner.textContent = message;
        
        // Add animation CSS if not already added
        if (!document.getElementById('blazor-banner-styles')) {
            const style = document.createElement('style');
            style.id = 'blazor-banner-styles';
            style.textContent = `
                @keyframes slideDown {
                    from { transform: translateY(-100%); }
                    to { transform: translateY(0); }
                }
            `;
            document.head.appendChild(style);
        }
        
        document.body.insertBefore(banner, document.body.firstChild);
    },
    
    hideBanner: function(id) {
        const banner = document.getElementById(id);
        if (banner) {
            banner.remove();
        }
    },
    
    showConnectionError: function(message) {
        window.blazorConnection.showBanner('⚠️ ' + message, '#dc2626', 'blazor-connection-error');
        
        // Auto-hide after 10 seconds
        setTimeout(function() {
            window.blazorConnection.hideBanner('blazor-connection-error');
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
    },
    
    // Allow disabling auto-reload (can be called from browser console)
    disableAutoReload: function() {
        window.blazorConnection.autoReloadEnabled = false;
        console.log('[Blazor] Auto-reload disabled. Call enableAutoReload() to re-enable.');
        window.blazorConnection.showBanner('Auto-reload inaktiverat', '#6b7280', 'blazor-autoreload-disabled');
    },
    
    enableAutoReload: function() {
        window.blazorConnection.autoReloadEnabled = true;
        console.log('[Blazor] Auto-reload enabled.');
        window.blazorConnection.hideBanner('blazor-autoreload-disabled');
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.blazorConnection.init);
} else {
    window.blazorConnection.init();
}

