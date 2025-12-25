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
            // CRITICAL: Multiple checks to prevent infinite loop
            // Check if modal was already shown for this version
            if (modalShownForVersion === currentVersion) {
                console.log('[DeployNotification] Modal already shown for this version - skipping init');
                return;
            }
            
            // Check if modal already exists in DOM
            if (document.getElementById('deploy-notification-modal')) {
                console.log('[DeployNotification] Modal already exists in DOM - skipping init');
                return;
            }
            
            // Check if user dismissed this version
            const dismissedVersion = localStorage.getItem('app-modal-dismissed-version');
            if (dismissedVersion === currentVersion) {
                console.log('[DeployNotification] Modal dismissed for this version - skipping init');
                return;
            }
            
            // Check if we should show modal
            // If user has already seen this version but not reloaded, show modal
            // If this is first time seeing this version, mark as seen and show modal after delay
            if (currentVersion === lastSeenVersion) {
                // User has seen this version before but not reloaded - show modal once
                console.log('[DeployNotification] Version changed - will show notification modal once');
                window.deployNotification.initTimeout = setTimeout(function() {
                    // CRITICAL: Triple-check before showing
                    if (!document.getElementById('deploy-notification-modal') && 
                        !window.deployNotification.modalShown &&
                        localStorage.getItem('app-modal-shown-version') !== currentVersion &&
                        localStorage.getItem('app-modal-dismissed-version') !== currentVersion) {
                        window.deployNotification.showModal(currentVersion);
                    }
                }, 2000); // Wait 2 seconds after page load
            } else {
                // First time seeing this version - mark as seen and show modal after delay
                localStorage.setItem('app-last-seen-version', currentVersion);
                console.log('[DeployNotification] New version detected - will show notification once');
                window.deployNotification.initTimeout = setTimeout(function() {
                    // CRITICAL: Triple-check before showing
                    if (!document.getElementById('deploy-notification-modal') && 
                        !window.deployNotification.modalShown &&
                        localStorage.getItem('app-modal-shown-version') !== currentVersion &&
                        localStorage.getItem('app-modal-dismissed-version') !== currentVersion) {
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
        // CRITICAL: Multiple checks to prevent infinite loop
        // Check if modal already exists in DOM
        if (document.getElementById('deploy-notification-modal')) {
            console.log('[DeployNotification] Modal already exists in DOM - skipping forced show');
            return;
        }
        
        // Check if modal flag is set
        if (window.deployNotification.modalShown) {
            console.log('[DeployNotification] Modal already shown (flag) - skipping forced show');
            return;
        }
        
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
        
        // CRITICAL: Check if modal was already shown for this version
        const modalShownForVersion = localStorage.getItem('app-modal-shown-version');
        if (modalShownForVersion === currentVersion) {
            // Already shown for this version - check if dismissed
            const dismissedVersion = localStorage.getItem('app-modal-dismissed-version');
            if (dismissedVersion === currentVersion) {
                console.log('[DeployNotification] Modal already dismissed for this version - skipping forced show');
                return;
            }
            // Even if not dismissed, don't show again for same version
            console.log('[DeployNotification] Modal already shown for this version - skipping forced show');
            return;
        }
        
        // Mark as shown IMMEDIATELY before creating modal
        window.deployNotification.modalShown = true;
        localStorage.setItem('app-modal-shown-version', currentVersion);
        
        // Remove existing modal if any (shouldn't happen, but just in case)
        const existing = document.getElementById('deploy-notification-modal');
        if (existing) {
            existing.remove();
        }
        
        this.showModalInternal(currentVersion, callback || function() {
            localStorage.setItem('app-last-reloaded-version', currentVersion);
            window.location.reload();
        }, false); // Always use false for isConnectionIssue - no connection error text
    },
    
    showModalInternal: function(version, onReloadCallback, isConnectionIssue) {
        // isConnectionIssue parameter is kept for compatibility but not used in UI text
        
        // CRITICAL: Multiple checks to prevent infinite loop
        // Check if modal already exists
        if (document.getElementById('deploy-notification-modal')) {
            console.log('[DeployNotification] Modal already exists in DOM (internal) - skipping');
            return;
        }
        
        // Check if modal flag is set
        if (window.deployNotification.modalShown && localStorage.getItem('app-modal-shown-version') === version) {
            console.log('[DeployNotification] Modal already shown for this version (internal) - skipping');
            return;
        }
        
        // CRITICAL: Check if user dismissed this version - prevent infinite loop
        const dismissedVersion = localStorage.getItem('app-modal-dismissed-version');
        const dismissedTimestamp = localStorage.getItem('app-modal-dismissed-timestamp');
        if (dismissedVersion === version && dismissedTimestamp) {
            const dismissedTime = parseInt(dismissedTimestamp, 10);
            const timeSinceDismissal = Date.now() - dismissedTime;
            // If dismissed, don't show again for this version (even if time passed)
            console.log('[DeployNotification] Modal dismissed for this version - not showing again');
            return;
        }
        
        // Create modal overlay using app's existing modal-container style
        const overlay = document.createElement('div');
        overlay.id = 'deploy-notification-modal';
        overlay.className = 'modal-container';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.45);
            z-index: 4000;
            display: flex;
            justify-content: center;
            align-items: flex-start;
            padding-top: 50px;
            overflow-y: auto;
        `;
        
        // Create modal content using app's existing bm-modal style
        const modal = document.createElement('div');
        modal.className = 'bm-modal';
        modal.style.cssText = `
            background: #fff;
            color: #0f1720;
            padding: 28px 28px 0 28px;
            border: 1px solid #e6e7ea;
            border-radius: 12px;
            width: 480px;
            box-shadow: 0 8px 28px rgba(16,24,40,.18);
            max-height: 80vh;
            overflow-y: auto;
            display: flex;
            flex-direction: column;
            gap: 12px;
        `;
        
        // Create header using app's bm-header style
        const header = document.createElement('div');
        header.className = 'bm-header';
        header.style.cssText = 'margin-bottom: 20px; text-align: center;';
        
        const iconDiv = document.createElement('div');
        iconDiv.style.cssText = 'margin-bottom: 16px;';
        const icon = document.createElement('span');
        icon.className = 'material-symbols-outlined';
        icon.style.cssText = 'font-size: 48px; color: #1976d2; font-variation-settings: "FILL" 0;';
        icon.textContent = 'info';
        iconDiv.appendChild(icon);
        
        const title = document.createElement('h2');
        title.className = 'bm-title';
        title.style.cssText = 'text-align: center; margin: 0 0 12px 0;';
        title.textContent = 'En ny version av denna sida är tillgänglig';
        
        const text = document.createElement('p');
        text.style.cssText = 'margin: 0; color: #6b7280; font-size: 14px; line-height: 1.5; text-align: center;';
        text.textContent = 'Ladda om för att se de senaste ändringarna.';
        
        header.appendChild(iconDiv);
        header.appendChild(title);
        header.appendChild(text);
        
        // Create actions using app's bm-actions style
        const actions = document.createElement('div');
        actions.className = 'bm-actions';
        actions.style.cssText = `
            display: flex;
            gap: 12px;
            justify-content: center;
            margin-top: 8px;
            position: sticky;
            bottom: 0;
            background: #fff;
            border-top: 1px solid #e6e7ea;
            padding-top: 12px;
            padding-bottom: 28px;
            z-index: 10;
        `;
        
        // "Inte nu" button using app's btn-outline style
        const notNowBtn = document.createElement('button');
        notNowBtn.type = 'button';
        notNowBtn.className = 'btn-outline';
        notNowBtn.textContent = 'Inte nu';
        notNowBtn.addEventListener('click', function() {
            // CRITICAL: Save dismissal info to prevent infinite loop
            localStorage.setItem('app-modal-dismissed-version', version);
            localStorage.setItem('app-modal-dismissed-timestamp', Date.now().toString());
            // Remove modal
            overlay.remove();
            window.deployNotification.modalShown = false;
            console.log('[DeployNotification] Modal dismissed by user - will not show again for this version');
        });
        
        // "Ladda om" button using app's btn-primary style
        const reloadBtn = document.createElement('button');
        reloadBtn.type = 'button';
        reloadBtn.id = 'deploy-reload-btn';
        reloadBtn.className = 'btn-primary';
        reloadBtn.textContent = 'Ladda om';
        reloadBtn.addEventListener('click', function() {
            // Mark this version as reloaded permanently
            localStorage.setItem('app-last-reloaded-version', version);
            // Clear dismissal info since user is reloading
            localStorage.removeItem('app-modal-dismissed-version');
            localStorage.removeItem('app-modal-dismissed-timestamp');
            // Call callback (which will reload)
            if (onReloadCallback) {
                onReloadCallback();
            }
        });
        
        actions.appendChild(notNowBtn);
        actions.appendChild(reloadBtn);
        
        modal.appendChild(header);
        modal.appendChild(actions);
        overlay.appendChild(modal);
        document.body.appendChild(overlay);
        
        // Prevent closing by clicking outside (user must click a button)
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) {
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

