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
    lastClickTime: 0,
    connectionDeadModalShown: false,
    circuitDead: false, // Track if circuit is completely dead (not initialized)
    
    init: function() {
        console.log('[Blazor] Initializing enhanced connection monitoring...');
        
        // Monitor SignalR connection events
        if (window.Blazor) {
            // Listen for circuit errors - especially "Circuit not initialized"
            Blazor.addEventListener('error', function(event) {
                const errorDetail = event.detail?.toString() || '';
                console.error('[Blazor] Circuit error:', errorDetail);
                
                // Detect "Circuit not initialized" - this means circuit is completely dead
                if (errorDetail.includes('Circuit not initialized') || 
                    errorDetail.includes('No interop methods are registered')) {
                    window.blazorConnection.circuitDead = true;
                    window.blazorConnection.connectionState = 'Disconnected';
                    console.log('[Blazor] Circuit not initialized detected - circuit is completely dead');
                } else {
                    window.blazorConnection.handleConnectionError('Circuit error: ' + errorDetail);
                }
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
                // Wait a moment for Blazor to potentially reconnect, then test
                setTimeout(function() {
                    window.blazorConnection.testConnection(true).then(function(isAlive) {
                        if (!isAlive) {
                            console.log('[Blazor] Connection dead after tab became visible - checking for deploy');
                            // Only show modal if there's a new version - otherwise let Blazor reconnect silently
                            if (window.deployNotification && window.deployNotification.shouldShowModal()) {
                                window.deployNotification.showModalForced(function() {
                                    window.location.reload();
                                }, false);
                            }
                        }
                    });
                }, 1000); // Wait 1 second for potential reconnection
            }
        });
        
        // Enhanced connection health check with actual JS interop test
        // Check less frequently - app should stay alive for 5 minutes
        setInterval(function() {
            window.blazorConnection.checkConnectionHealth();
        }, 30000); // Check every 30 seconds (less aggressive)
        
        // Track user activity AND test connection on interaction after inactivity
        // CRITICAL: Test connection on EVERY click to detect silent failures
        document.addEventListener('click', function(e) {
            window.blazorConnection.lastActivity = Date.now();
            
            // Always test connection on click if we haven't tested recently
            // This catches "silent failures" where clicks don't work
            const timeSinceLastTest = Date.now() - window.blazorConnection.lastConnectionTest;
            if (timeSinceLastTest > 2000) { // Test if last test was >2s ago (more frequent)
                window.blazorConnection.testConnectionOnInteraction(e);
            }
        }, { passive: true, capture: true }); // Use capture to catch early
        
        // Also track keydown for activity
        document.addEventListener('keydown', function(e) {
            window.blazorConnection.lastActivity = Date.now();
        }, { passive: true });
        
        // Also test connection when user returns after long inactivity (mouse movement after being away)
        let lastMouseMove = Date.now();
        document.addEventListener('mousemove', function() {
            const now = Date.now();
            const timeSinceLastMove = now - lastMouseMove;
            lastMouseMove = now;
            
            // If mouse hasn't moved for >30 seconds, test connection when it moves again
            if (timeSinceLastMove > 30000) {
                console.log('[Blazor] Mouse moved after long inactivity - testing connection');
                window.blazorConnection.testConnection(true).then(function(isAlive) {
                    if (!isAlive) {
                        console.log('[Blazor] Connection dead after inactivity - checking for deploy');
                        // Only show modal if there's a new version - otherwise let Blazor reconnect silently
                        if (window.deployNotification && window.deployNotification.shouldShowModal()) {
                            window.deployNotification.showModalForced(function() {
                                window.location.reload();
                            }, false);
                        }
                    }
                });
            }
        }, { passive: true });
        
        // Track scroll and mousemove for activity (but don't test on these)
        ['scroll', 'mousemove'].forEach(function(event) {
            document.addEventListener(event, function() {
                window.blazorConnection.lastActivity = Date.now();
            }, { passive: true });
        });
        
        console.log('[Blazor] Enhanced connection monitoring initialized');
    },
    
    // NEW: Test connection on user interaction (click/keydown) - try to wake up the app
    testConnectionOnInteraction: function(event) {
        // Don't test on every click - debounce
        if (window.blazorConnection.pendingClickTest) {
            return;
        }
        
        // Mark that we're expecting a response
        const clickTime = Date.now();
        window.blazorConnection.lastClickTime = clickTime;
        
        // CRITICAL: If circuit is dead (not initialized), auto-reload immediately
        // JavaScript can detect clicks even when circuit is dead
        if (window.blazorConnection.circuitDead) {
            console.log('[Blazor] Circuit is dead (not initialized) - user clicked, auto-reloading...');
            
            // Check for deploy first - if new version, show modal with "Inte nu" option
            if (window.deployNotification && window.deployNotification.shouldShowModal()) {
                console.log('[Blazor] Circuit dead and new version available - showing deploy modal');
                window.deployNotification.showModalForced(function() {
                    window.location.reload();
                }, false);
            } else {
                // No deploy, just reload automatically
                console.log('[Blazor] Circuit dead - auto-reloading page');
                window.location.reload();
            }
            return; // Don't try to reconnect if circuit is dead
        }
        
        // CRITICAL: First, try to wake up the app by attempting reconnection
        // Blazor Server should automatically reconnect when user interacts
        if (window.Blazor && typeof window.Blazor.reconnect === 'function') {
            try {
                console.log('[Blazor] User clicked - attempting to wake up app via reconnect');
                window.Blazor.reconnect();
            } catch (e) {
                console.log('[Blazor] Reconnect not available, will test connection instead');
            }
        }
        
        // Wait a moment for reconnection to happen, then test
        window.blazorConnection.pendingClickTest = setTimeout(function() {
            window.blazorConnection.pendingClickTest = null;
            
            // Test connection after giving reconnection a chance
            window.blazorConnection.testConnection(true).then(function(isAlive) {
                if (isAlive) {
                    // Connection restored! Reset state
                    if (window.blazorConnection.connectionState === 'Disconnected') {
                        console.log('[Blazor] Connection restored after user click - app woke up!');
                        window.blazorConnection.connectionState = 'Connected';
                        window.blazorConnection.reconnectAttempts = 0;
                        window.blazorConnection.disconnectedSince = null;
                        window.blazorConnection.circuitDead = false; // Reset circuit dead flag
                    }
                } else if (window.blazorConnection.lastClickTime === clickTime) {
                    // Still dead after click - circuit is completely dead
                    // Since we can detect the click, we can automatically reload
                    console.log('[Blazor] Connection is dead and cannot reconnect - user clicked, auto-reloading...');
                    
                    // Check for deploy first
                    if (window.deployNotification && window.deployNotification.shouldShowModal()) {
                        console.log('[Blazor] Connection dead and new version available - showing deploy modal');
                        window.deployNotification.showModalForced(function() {
                            window.location.reload();
                        }, false);
                    } else {
                        // No deploy, but connection is dead - just reload automatically
                        console.log('[Blazor] Connection dead - auto-reloading page');
                        window.location.reload();
                    }
                }
            });
        }, 2000); // Wait 2 seconds after click to give reconnection time
    },
    
    // IMPROVED: Actually test JS interop with timeout - uses real JS interop call to PingAsync
    testConnection: function(force) {
        const now = Date.now();
        const timeSinceLastTest = now - window.blazorConnection.lastConnectionTest;
        
        // Don't test too frequently unless forced
        if (!force && timeSinceLastTest < 3000) {
            return Promise.resolve(true);
        }
        
        window.blazorConnection.lastConnectionTest = now;
        
        return new Promise(function(resolve) {
            if (!window.Blazor || !window.DotNet) {
                console.warn('[Blazor] Blazor or DotNet not available for connection test');
                window.blazorConnection.handleConnectionError('Blazor not available');
                resolve(false);
                return;
            }
            
            // Use a timeout to detect if circuit is dead
            const testTimeout = setTimeout(function() {
                console.warn('[Blazor] Connection test timeout - circuit appears dead');
                window.blazorConnection.handleConnectionError('Connection test timeout');
                resolve(false);
            }, window.blazorConnection.clickTestTimeout);
            
            try {
                // Try to call the PingAsync method via JS interop using DotNetObjectReference
                // This is the most reliable way to test if the circuit is actually alive
                const healthCheckRef = window.blazorConnection.healthCheckRef;
                if (!healthCheckRef) {
                    clearTimeout(testTimeout);
                    console.warn('[Blazor] Health check reference not available');
                    window.blazorConnection.handleConnectionError('Health check reference not available');
                    resolve(false);
                    return;
                }
                
                healthCheckRef.invokeMethodAsync('PingAsync')
                    .then(function(result) {
                        clearTimeout(testTimeout);
                        if (result === true) {
                            // Circuit is alive
                        if (window.blazorConnection.connectionState === 'Disconnected') {
                            console.log('[Blazor] Connection restored via JS interop test');
                            window.blazorConnection.connectionState = 'Connected';
                            window.blazorConnection.hideReconnectingMessage();
                            window.blazorConnection.reconnectAttempts = 0;
                            window.blazorConnection.disconnectedSince = null;
                            window.blazorConnection.circuitDead = false; // Reset circuit dead flag
                        }
                        resolve(true);
                        } else {
                            window.blazorConnection.handleConnectionError('Ping returned false');
                            resolve(false);
                        }
                    })
                    .catch(function(error) {
                        clearTimeout(testTimeout);
                        console.warn('[Blazor] JS interop test failed:', error);
                        // If JS interop fails, the circuit is likely dead
                        window.blazorConnection.handleConnectionError('JS interop test failed: ' + error.message);
                        resolve(false);
                    });
            } catch (e) {
                clearTimeout(testTimeout);
                console.warn('[Blazor] Connection test exception:', e);
                window.blazorConnection.handleConnectionError('Connection test exception: ' + e.message);
                resolve(false);
            }
        });
    },
    
    setupReconnectionHandling: function() {
        // Listen for Blazor connection events
        if (window.Blazor) {
            console.log('[Blazor] Setting up reconnection event listeners...');
            
            // Listen for Blazor circuit events
            // These are fired by Blazor Server when the SignalR connection state changes
            window.addEventListener('beforeunload', function() {
                // Clean up on page unload
                window.blazorConnection.connectionState = 'Disconnected';
            });
            
            // Listen for browser network events
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
                            console.log('[Blazor] Still disconnected after coming online - checking for deploy...');
                            // Check if there's a new version deployed
                            if (window.deployNotification && window.deployNotification.shouldShowModal()) {
                                window.deployNotification.showModalForced(function() {
                                    window.location.reload();
                                }, false);
                            } else {
                                window.blazorConnection.autoReload('Failed to reconnect after coming online');
                            }
                        }
                    });
                }, 5000);
            });
            
            // REMOVED: Automatic reconnection monitoring that kept triggering modals
            // Blazor Server handles reconnection automatically - we don't need to constantly check
            // Only check when user interacts or when explicitly needed
            
            console.log('[Blazor] Reconnection handling configured');
            window.blazorConnection.connectionState = 'Connected';
        }
    },
    
    checkConnectionHealth: function() {
        if (document.hidden) return; // Skip when tab is hidden
        
        // CRITICAL: Don't check if modal is already shown - prevents infinite loop
        if (window.blazorConnection.connectionDeadModalShown || document.getElementById('connection-dead-modal')) {
            return;
        }
        
        // CRITICAL: Don't check if deploy modal is shown - prevents infinite loop
        if (document.getElementById('deploy-notification-modal')) {
            return;
        }
        
        const timeSinceActivity = Date.now() - window.blazorConnection.lastActivity;
        
        // If user has been inactive for > 10 minutes, don't show modal (user might be away)
        if (timeSinceActivity > 10 * 60 * 1000) {
            return;
        }
        
        // Actually test the connection instead of just checking state
        window.blazorConnection.testConnection(false).then(function(isAlive) {
            if (!isAlive && window.blazorConnection.connectionState === 'Disconnected') {
                // CRITICAL: Only show modal after 5 MINUTES of disconnection, not 10 seconds
                // App should stay alive for up to 5 minutes before requiring reload
                if (!window.blazorConnection.disconnectedSince) {
                    window.blazorConnection.disconnectedSince = Date.now();
                } else if (Date.now() - window.blazorConnection.disconnectedSince > 5 * 60 * 1000) {
                    // Only show modal if there's a new version - otherwise let Blazor handle reconnection silently
                    if (window.deployNotification && window.deployNotification.shouldShowModal()) {
                        console.log('[Blazor] Disconnected for 5+ minutes and new version available - showing deploy modal');
                        window.deployNotification.showModalForced(function() {
                            window.location.reload();
                        }, false);
                    }
                    // Don't show connection dead modal - let Blazor handle reconnection silently
                }
            }
        });
    },
    
    handleConnectionError: function(reason) {
        console.error('[Blazor] Connection error:', reason);
        window.blazorConnection.connectionState = 'Disconnected';
        window.blazorConnection.reconnectAttempts++;
        
        // Don't show reconnecting banner - it's annoying and keeps popping up
        // Instead, silently try to reconnect and only show modal if it fails after a while
        
        if (window.blazorConnection.reconnectAttempts >= window.blazorConnection.maxReconnectAttempts) {
            console.log('[Blazor] Max reconnect attempts reached - checking for deploy...');
            // Check if there's a new version deployed
            if (window.deployNotification && window.deployNotification.shouldShowModal()) {
                window.deployNotification.showModalForced(function() {
                    window.location.reload();
                }, false);
            } else {
                // No new version, but connection failed - don't show modal
                // Let Blazor handle reconnection silently - app should wake up on user interaction
                console.log('[Blazor] Connection failed but no new version - letting Blazor reconnect silently');
            }
        }
        // Don't show banner - let Blazor handle reconnection silently
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
        // REMOVED: Don't show annoying banner that keeps popping up
        // Blazor handles reconnection automatically - no need to annoy user
    },
    
    hideReconnectingMessage: function() {
        // REMOVED: No banner to hide
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
    },
    
    // NEW: Show modal when connection is dead and user tries to interact
    showConnectionDeadModal: function() {
        // Don't show if already shown
        if (window.blazorConnection.connectionDeadModalShown || document.getElementById('connection-dead-modal')) {
            return;
        }
        
        window.blazorConnection.connectionDeadModalShown = true;
        
        // Create modal overlay
        const overlay = document.createElement('div');
        overlay.id = 'connection-dead-modal';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.5);
            z-index: 10001;
            display: flex;
            align-items: center;
            justify-content: center;
            animation: fadeIn 0.3s ease-out;
        `;
        
        // Create modal content
        const modal = document.createElement('div');
        modal.style.cssText = `
            background: white;
            border-radius: 12px;
            padding: 32px;
            max-width: 480px;
            width: 90%;
            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
            animation: slideUp 0.3s ease-out;
        `;
        
        modal.innerHTML = `
            <div style="text-align: center; margin-bottom: 24px;">
                <div style="margin-bottom: 16px;">
                    <span class="material-symbols-outlined" style="font-size: 48px; color: #1976d2; font-variation-settings: 'FILL' 0;">
                        refresh
                    </span>
                </div>
                <h2 style="margin: 0 0 12px 0; color: #1f2937; font-size: 24px; font-weight: 600;">
                    Ladda om sidan
                </h2>
                <p style="margin: 0; color: #6b7280; font-size: 16px; line-height: 1.5;">
                    För att fortsätta använda appen behöver du ladda om sidan.
                </p>
            </div>
            <div style="display: flex; gap: 12px; justify-content: center;">
                <button id="connection-reload-btn" style="
                    background: #1976d2;
                    color: white;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 8px;
                    font-size: 16px;
                    font-weight: 500;
                    cursor: pointer;
                    transition: background 0.2s;
                " onmouseover="this.style.background='#1565c0'" onmouseout="this.style.background='#1976d2'">
                    Ladda om sidan
                </button>
            </div>
        `;
        
        overlay.appendChild(modal);
        document.body.appendChild(overlay);
        
        // Add animations if not already added
        if (!document.getElementById('connection-dead-modal-styles')) {
            const style = document.createElement('style');
            style.id = 'connection-dead-modal-styles';
            style.textContent = `
                @keyframes fadeIn {
                    from { opacity: 0; }
                    to { opacity: 1; }
                }
                @keyframes slideUp {
                    from { 
                        transform: translateY(20px);
                        opacity: 0;
                    }
                    to { 
                        transform: translateY(0);
                        opacity: 1;
                    }
                }
            `;
            document.head.appendChild(style);
        }
        
        // Handle reload button click
        const reloadBtn = document.getElementById('connection-reload-btn');
        reloadBtn.addEventListener('click', function() {
            window.location.reload();
        });
        
        // Prevent closing by clicking outside (user must click reload)
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) {
                e.stopPropagation();
            }
        });
        
        console.log('[Blazor] Connection dead modal shown');
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.blazorConnection.init);
} else {
    window.blazorConnection.init();
}

