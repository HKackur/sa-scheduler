// Deploy notification - shows modal when app has been updated
window.deployNotification = {
    modalShown: false, // Flag to prevent showing modal multiple times
    initTimeout: null, // Store timeout to cancel if needed
    
    init: function() {
        // Prevent multiple initializations
        if (window.deployNotification._initialized) {
            console.log('[DeployNotification] Already initialized - skipping');
            return;
        }
        window.deployNotification._initialized = true;
        
        // Cancel any existing timeout
        if (window.deployNotification.initTimeout) {
            clearTimeout(window.deployNotification.initTimeout);
            window.deployNotification.initTimeout = null;
        }
        
        // Get app version from meta tag
        const versionMeta = document.querySelector('meta[name="app-version"]');
        if (!versionMeta) {
            console.warn('[DeployNotification] No app-version meta tag found');
            return;
        }
        
        const currentVersion = versionMeta.getAttribute('content');
        if (!currentVersion) {
            console.warn('[DeployNotification] No version value found');
            return;
        }
        
        // Get last seen version from localStorage
        const lastSeenVersion = localStorage.getItem('app-last-seen-version');
        const lastReloadedVersion = localStorage.getItem('app-last-reloaded-version');
        const modalShownForVersion = localStorage.getItem('app-modal-shown-version');
        
        console.log('[DeployNotification] Current version:', currentVersion);
        console.log('[DeployNotification] Last seen version:', lastSeenVersion);
        console.log('[DeployNotification] Last reloaded version:', lastReloadedVersion);
        console.log('[DeployNotification] Modal shown for version:', modalShownForVersion);
        
        // If this is first time user visits (no lastSeenVersion), just save current version and don't show modal
        if (!lastSeenVersion && !lastReloadedVersion) {
            // First visit - save version as both seen and reloaded (user is on latest version)
            localStorage.setItem('app-last-seen-version', currentVersion);
            localStorage.setItem('app-last-reloaded-version', currentVersion);
            localStorage.setItem('app-modal-shown-version', currentVersion); // Mark as shown to prevent showing
            console.log('[DeployNotification] First visit - saved version, no notification needed');
            return;
        }
        
        // If version changed and user hasn't reloaded for this version yet
        if (currentVersion !== lastReloadedVersion) {
            // CRITICAL: Check if modal was already shown for this version OR if modal already exists in DOM
            if (modalShownForVersion === currentVersion || document.getElementById('deploy-notification-modal')) {
                console.log('[DeployNotification] Modal already shown for this version or exists in DOM - skipping');
                return;
            }
            
            // Check if we should show modal
            // If user has already seen this version but not reloaded, show modal
            // If this is first time seeing this version, mark as seen and show modal after delay
            if (currentVersion === lastSeenVersion) {
                // User has seen this version before but not reloaded - show modal immediately
                console.log('[DeployNotification] Version changed - showing notification modal');
                window.deployNotification.initTimeout = setTimeout(function() {
                    // Double-check modal doesn't exist and hasn't been shown
                    if (!document.getElementById('deploy-notification-modal') && 
                        !window.deployNotification.modalShown &&
                        localStorage.getItem('app-modal-shown-version') !== currentVersion) {
                        window.deployNotification.showModal(currentVersion);
                    }
                }, 2000); // Wait 2 seconds after page load
            } else {
                // First time seeing this version - mark as seen and show modal after delay
                localStorage.setItem('app-last-seen-version', currentVersion);
                console.log('[DeployNotification] New version detected - will show notification');
                window.deployNotification.initTimeout = setTimeout(function() {
                    // Double-check modal doesn't exist and hasn't been shown
                    if (!document.getElementById('deploy-notification-modal') && 
                        !window.deployNotification.modalShown &&
                        localStorage.getItem('app-modal-shown-version') !== currentVersion) {
                        window.deployNotification.showModal(currentVersion);
                    }
                }, 3000); // Wait 3 seconds after page load for first detection
            }
        } else {
            // Version matches - user has already reloaded for this version
            console.log('[DeployNotification] Version up to date - no notification needed');
        }
    },
    
    shouldShowModal: function() {
        const versionMeta = document.querySelector('meta[name="app-version"]');
        if (!versionMeta) return false;
        
        const currentVersion = versionMeta.getAttribute('content');
        if (!currentVersion) return false;
        
        const lastReloadedVersion = localStorage.getItem('app-last-reloaded-version');
        
        // Should show if version changed and user hasn't reloaded for this version
        return currentVersion !== lastReloadedVersion;
    },
    
    showModal: function(version) {
        // CRITICAL: Multiple checks to prevent duplicates
        if (window.deployNotification.modalShown) {
            console.log('[DeployNotification] Modal already shown (flag) - skipping');
            return;
        }
        
        if (document.getElementById('deploy-notification-modal')) {
            console.log('[DeployNotification] Modal already exists in DOM - skipping');
            return;
        }
        
        const modalShownForVersion = localStorage.getItem('app-modal-shown-version');
        if (modalShownForVersion === version) {
            console.log('[DeployNotification] Modal already shown for this version - skipping');
            return;
        }
        
        // Mark as shown IMMEDIATELY to prevent duplicates
        window.deployNotification.modalShown = true;
        localStorage.setItem('app-modal-shown-version', version);
        console.log('[DeployNotification] Showing modal for version:', version);
        
        this.showModalInternal(version, function() {
            // Default callback - mark as reloaded and reload
            localStorage.setItem('app-last-reloaded-version', version);
            window.location.reload();
        });
    },
    
    showModalForced: function(callback, isConnectionIssue) {
        // Force show modal even if already shown (for connection health scenarios)
        // isConnectionIssue = true if this is triggered by connection health monitoring
        const versionMeta = document.querySelector('meta[name="app-version"]');
        if (!versionMeta) {
            // No version info - just use callback
            if (callback) callback();
            return;
        }
        
        const currentVersion = versionMeta.getAttribute('content');
        if (!currentVersion) {
            if (callback) callback();
            return;
        }
        
        // Check if modal was already shown for this version
        const modalShownForVersion = localStorage.getItem('app-modal-shown-version');
        if (modalShownForVersion === currentVersion && !isConnectionIssue) {
            // Already shown for this version and not a connection issue - skip
            console.log('[DeployNotification] Modal already shown for this version - skipping forced show');
            return;
        }
        
        // Mark as shown
        window.deployNotification.modalShown = true;
        localStorage.setItem('app-modal-shown-version', currentVersion);
        
        // Remove existing modal if any
        const existing = document.getElementById('deploy-notification-modal');
        if (existing) {
            existing.remove();
        }
        
        this.showModalInternal(currentVersion, callback || function() {
            localStorage.setItem('app-last-reloaded-version', currentVersion);
            window.location.reload();
        }, isConnectionIssue || false);
    },
    
    showModalInternal: function(version, onReloadCallback, isConnectionIssue) {
        // isConnectionIssue = true if modal is shown due to connection problem
        
        // Create modal overlay
        const overlay = document.createElement('div');
        overlay.id = 'deploy-notification-modal';
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
                        system_update_alt
                    </span>
                </div>
                <h2 style="margin: 0 0 12px 0; color: #1f2937; font-size: 24px; font-weight: 600;">
                    ${isConnectionIssue ? 'Anslutningen har brutits' : 'Appen har uppdaterats'}
                </h2>
                <p style="margin: 0; color: #6b7280; font-size: 16px; line-height: 1.5;">
                    ${isConnectionIssue 
                        ? 'Anslutningen till servern har brutits och appen har uppdaterats. För att fortsätta använda appen med de senaste funktionerna behöver du ladda om sidan.' 
                        : 'Vi har gjort en uppdatering av appen. För att fortsätta använda den med de senaste funktionerna och förbättringarna behöver du ladda om sidan.'}
                </p>
            </div>
            <div style="display: flex; gap: 12px; justify-content: center;">
                <button id="deploy-reload-btn" style="
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
        if (!document.getElementById('deploy-notification-styles')) {
            const style = document.createElement('style');
            style.id = 'deploy-notification-styles';
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
        const reloadBtn = document.getElementById('deploy-reload-btn');
        reloadBtn.addEventListener('click', function() {
            // Mark this version as reloaded
            localStorage.setItem('app-last-reloaded-version', version);
            // Call callback (which will reload)
            if (onReloadCallback) {
                onReloadCallback();
            }
        });
        
        // Prevent closing by clicking outside (user must click reload)
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) {
                // Don't close - user must click reload button
                e.stopPropagation();
            }
        });
        
        console.log('[DeployNotification] Modal shown for version:', version);
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.deployNotification.init);
} else {
    window.deployNotification.init();
}

