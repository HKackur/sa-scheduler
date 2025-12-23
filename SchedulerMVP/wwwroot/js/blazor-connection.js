// Blazor Server connection monitoring and auto-reload handling
window.blazorConnection = {
    connectionState: 'Unknown',
    reconnectAttempts: 0,
    maxReconnectAttempts: 3,
    autoReloadEnabled: true,
    lastActivity: Date.now(),
    lastConnectionTest: 0,
    pendingClickTest: null,
    disconnectedSince: null,
    clickTestTimeout: 2000, // 2 seconds timeout for click test
    
    init: function() {
        console.log('[Blazor] Initializing enhanced connection monitoring...');
        
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
        
        // Monitor page visibility - TEST CONNECTION IMMEDIATELY when tab becomes visible
        document.addEventListener('visibilitychange', function() {
            if (document.hidden) {
                console.log('[Blazor] Page hidden');
            } else {
                console.log('[Blazor] Page visible - testing connection immediately...');
                window.blazorConnection.lastActivity = Date.now();
                // Test connection immediately when tab becomes visible
                window.blazorConnection.testConnection(true); // true = force test
            }
        });
        
        // Enhanced connection health check with actual JS interop test
        setInterval(function() {
            window.blazorConnection.checkConnectionHealth();
        }, 10000); // Check every 10 seconds (reduced from 15)
        
        // Track user activity AND test connection on interaction after inactivity
        ['click', 'keydown'].forEach(function(event) {
            document.addEventListener(event, function(e) {
                window.blazorConnection.lastActivity = Date.now();
                
                // If user clicks after being away, test connection immediately
                const timeSinceLastTest = Date.now() - window.blazorConnection.lastConnectionTest;
                if (timeSinceLastTest > 5000) { // If last test was >5s ago
                    window.blazorConnection.testConnectionOnInteraction(e);
                }
            }, { passive: true, capture: true }); // Use capture to catch early
        });
        
        // Track scroll and mousemove for activity (but don't test on these)
        ['scroll', 'mousemove'].forEach(function(event) {
            document.addEventListener(event, function() {
                window.blazorConnection.lastActivity = Date.now();
            }, { passive: true });
        });
        
        console.log('[Blazor] Enhanced connection monitoring initialized');
    },
    
    // NEW: Test connection on user interaction (click/keydown)
    testConnectionOnInteraction: function(event) {
        // Don't test on every click - debounce
        if (window.blazorConnection.pendingClickTest) {
            return;
        }
        
        window.blazorConnection.pendingClickTest = setTimeout(function() {
            window.blazorConnection.pendingClickTest = null;
            window.blazorConnection.testConnection(true);
        }, 500); // Wait 500ms after click to see if Blazor responds
    },
    
    // IMPROVED: Actually test JS interop with timeout
    testConnection: function(force) {
        const now = Date.now();
        const timeSinceLastTest = now - window.blazorConnection.lastConnectionTest;
        
        // Don't test too frequently unless forced
        if (!force && timeSinceLastTest < 3000) {
            return Promise.resolve(true);
        }
        
        window.blazorConnection.lastConnectionTest = now;
        
        return new Promise(function(resolve) {
            if (!window.Blazor) {
                console.warn('[Blazor] Blazor not available for connection test');
                window.blazorConnection.handleConnectionError('Blazor not available');
                resolve(false);
                return;
            }
            
            // Try to access Blazor's circuit state to test if it's alive
            // Use a timeout to detect if circuit is dead
            const testTimeout = setTimeout(function() {
                console.warn('[Blazor] Connection test timeout - circuit appears dead');
                window.blazorConnection.handleConnectionError('Connection test timeout');
                resolve(false);
            }, window.blazorConnection.clickTestTimeout);
            
            try {
                // Try to access Blazor's internal state
                // If Blazor is loaded and circuit exists, this should work
                if (window.Blazor._internal && window.Blazor._internal.navigationManager) {
                    // Circuit seems alive
                    clearTimeout(testTimeout);
                    if (window.blazorConnection.connectionState === 'Disconnected') {
                        console.log('[Blazor] Connection restored');
                        window.blazorConnection.connectionState = 'Connected';
                        window.blazorConnection.hideReconnectingMessage();
                        window.blazorConnection.reconnectAttempts = 0;
                        window.blazorConnection.disconnectedSince = null;
                    }
                    resolve(true);
                } else {
                    // Try alternative method - check if Blazor.start was called
                    if (window.Blazor && typeof window.Blazor.reconnect === 'function') {
                        clearTimeout(testTimeout);
                        if (window.blazorConnection.connectionState === 'Disconnected') {
                            console.log('[Blazor] Connection restored (alternative check)');
                            window.blazorConnection.connectionState = 'Connected';
                            window.blazorConnection.hideReconnectingMessage();
                            window.blazorConnection.reconnectAttempts = 0;
                            window.blazorConnection.disconnectedSince = null;
                        }
                        resolve(true);
                    } else {
                        clearTimeout(testTimeout);
                        window.blazorConnection.handleConnectionError('Circuit state unavailable');
                        resolve(false);
                    }
                }
            } catch (e) {
                clearTimeout(testTimeout);
                console.warn('[Blazor] Connection test failed:', e);
                // Don't treat this as an error if we're just checking - might be normal
                if (window.blazorConnection.connectionState === 'Disconnected') {
                    window.blazorConnection.handleConnectionError('Connection test failed: ' + e.message);
                }
                resolve(false);
            }
        });
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
                    window.blazorConnection.testConnection(true).then(function(isAlive) {
                        if (!isAlive) {
                            console.log('[Blazor] Still disconnected after coming online - auto-reloading...');
                            window.blazorConnection.autoReload('Failed to reconnect after coming online');
                        }
                    });
                }, 5000);
            });
            
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
        
        // Actually test the connection instead of just checking state
        window.blazorConnection.testConnection(false).then(function(isAlive) {
            if (!isAlive && window.blazorConnection.connectionState === 'Disconnected') {
                // If disconnected for more than 10 seconds, auto-reload
                if (!window.blazorConnection.disconnectedSince) {
                    window.blazorConnection.disconnectedSince = Date.now();
                } else if (Date.now() - window.blazorConnection.disconnectedSince > 10000) {
                    console.log('[Blazor] Disconnected for >10s - auto-reloading...');
                    window.blazorConnection.autoReload('Connection lost');
                }
            }
        });
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
        
        // Check if deploy notification should be shown first
        // If version has changed, show deploy modal instead of just banner
        if (window.deployNotification && typeof window.deployNotification.shouldShowModal === 'function') {
            const shouldShowDeployModal = window.deployNotification.shouldShowModal();
            if (shouldShowDeployModal) {
                console.log('[Blazor] Connection lost AND version changed - showing deploy notification modal');
                window.deployNotification.showModalForced(function() {
                    // Callback when user clicks reload - then reload
                    window.location.reload();
                }, true); // true = this is a connection issue
                return; // Don't auto-reload yet - wait for user to click button
            }
        }
        
        // Version hasn't changed - just reload directly (no modal needed)
        console.log('[Blazor] Connection lost but version unchanged - reloading directly');
        
        // No deploy notification needed - just show banner and reload
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

