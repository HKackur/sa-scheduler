var SchedulerMVP = {
    // Format minutes since midnight to HH:mm format
    formatTime: function (minutes) {
        const hours = Math.floor(minutes / 60);
        const mins = minutes % 60;
        return String(hours).padStart(2, '0') + ':' + String(mins).padStart(2, '0');
    },
    
    scrollToPos: function (topPx) {
        try {
            // Scroll the entire week-grid container
            const weekGrid = document.querySelector('.week-grid');
            if (weekGrid) {
                weekGrid.scrollTop = Math.max(0, topPx);
            }
        } catch (_) { }
    },

    scrollToTime: function (minutes) {
        try {
            // Convert minutes to pixels (1 minute = 1 pixel)
            const topPx = minutes;
            this.scrollToPos(topPx);
        } catch (_) { }
    },
    
    initResourceViewScroll: function () {
        try {
            const listColumn = document.querySelector('.resource-list-column');
            const gridColumn = document.querySelector('.resource-grid-column');
            const timeAxisHeader = document.querySelector('.resource-time-axis-header');
            const scrollContainer = document.querySelector('.resource-scroll-container');
            
            if (!listColumn || !gridColumn || !timeAxisHeader || !scrollContainer) {
                console.log('[ResourceView] Elements not found, retrying...');
                setTimeout(() => SchedulerMVP.initResourceViewScroll(), 100);
                return;
            }
            
            // Clean up any existing listeners
            if (SchedulerMVP._resourceScrollCleanup) {
                SchedulerMVP._resourceScrollCleanup();
            }
            
            // Header is outside scroll column but needs horizontal sync
            // It's sticky vertically, but we need to sync its horizontal position with grid scroll
            const syncHeaderScroll = () => {
                if (gridColumn && timeAxisHeader) {
                    const scrollLeft = gridColumn.scrollLeft;
                    // Use transform for better compatibility with sticky positioning
                    timeAxisHeader.style.transform = `translateX(-${scrollLeft}px)`;
                }
            };
            
            // Initial sync
            syncHeaderScroll();
            
            // Sync on scroll - use both scroll and input events for better compatibility
            gridColumn.addEventListener('scroll', syncHeaderScroll, { passive: true });
            gridColumn.addEventListener('scrollend', syncHeaderScroll, { passive: true });
            
            // Also listen to wheel events for smoother sync
            gridColumn.addEventListener('wheel', syncHeaderScroll, { passive: true });
            
            // Store cleanup function
            SchedulerMVP._resourceScrollCleanup = () => {
                if (gridColumn) {
                    gridColumn.removeEventListener('scroll', syncHeaderScroll);
                    gridColumn.removeEventListener('scrollend', syncHeaderScroll);
                    gridColumn.removeEventListener('wheel', syncHeaderScroll);
                }
            };
            
            console.log('[ResourceView] Scroll synchronization initialized');
        } catch (error) {
            console.error('[ResourceView] Error initializing scroll:', error);
        }
    },

    // Drag and Drop functionality
    initDragDrop: function (dotNetHelper) {
        try {
            // Remove existing listeners to avoid duplicates
            document.querySelectorAll('.booking-block').forEach(block => {
                block.removeEventListener('dragstart', SchedulerMVP.handleDragStart);
                block.removeEventListener('dragend', SchedulerMVP.handleDragEnd);
                block.removeEventListener('click', SchedulerMVP.handleBookingBlockClick, true);
            });

            document.querySelectorAll('.day-content').forEach(day => {
                day.removeEventListener('dragover', SchedulerMVP.handleDragOver);
                day.removeEventListener('drop', SchedulerMVP.handleDrop);
                day.removeEventListener('dragenter', SchedulerMVP.handleDragEnter);
                day.removeEventListener('dragleave', SchedulerMVP.handleDragLeave);
            });

            // Add drag listeners to booking blocks (only primary bookings, not ghosts)
            document.querySelectorAll('.booking-block').forEach(block => {
                // Only make primary bookings draggable (not ghost bookings)
                if (block.classList.contains('booking--primary')) {
                    // Make sure resize handles are not draggable
                    block.querySelectorAll('.resize-handle').forEach(handle => {
                        handle.setAttribute('draggable', 'false');
                    });
                    block.setAttribute('draggable', 'true');
                    block.addEventListener('dragstart', SchedulerMVP.handleDragStart);
                    block.addEventListener('dragend', SchedulerMVP.handleDragEnd);
                } else {
                    block.setAttribute('draggable', 'false');
                }
                
                // Add click listener to handle clicks (since we removed Blazor's @onclick)
                // This uses capture phase (true) to intercept
                block.addEventListener('click', SchedulerMVP.handleBookingBlockClick, true);
            });

            // Add drop listeners to day columns
            document.querySelectorAll('.day-content').forEach(day => {
                day.addEventListener('dragover', SchedulerMVP.handleDragOver);
                day.addEventListener('drop', SchedulerMVP.handleDrop);
                day.addEventListener('dragenter', SchedulerMVP.handleDragEnter);
                day.addEventListener('dragleave', SchedulerMVP.handleDragLeave);
            });

            // Store DotNetHelper for callbacks
            SchedulerMVP.dotNetHelper = dotNetHelper;
            
            // Also initialize resize - COMMENTED OUT: Resize functionality disabled
            // SchedulerMVP.initResize(dotNetHelper);
        } catch (error) {
            console.error('Error initializing drag and drop:', error);
        }
    },
    
    // Handle click on booking block - prevent if resize just finished
    handleBookingBlockClick: function (e) {
        // Don't handle clicks on resize handles (they have their own handlers)
        if (e.target.classList.contains('resize-handle') || e.target.closest('.resize-handle')) {
            return; // Let resize handle handle it
        }
        
        // Find the booking block
        const bookingBlock = e.target.closest('.booking-block');
        if (!bookingBlock) {
            return; // Not a booking block
        }
        
        // CRITICAL: If drag just happened, prevent click from firing
        // This prevents popover from opening after drag
        if (SchedulerMVP.isDragging || bookingBlock.classList.contains('dragging')) {
            e.stopPropagation();
            e.stopImmediatePropagation();
            e.preventDefault();
            return false;
        }
        
        // Check if resize just finished - check flag, data attribute, and timestamp
        const hasJustResizedAttr = bookingBlock.hasAttribute('data-just-resized');
        const resizeEndTime = bookingBlock.getAttribute('data-resize-end-time');
        const timeSinceResize = resizeEndTime ? (Date.now() - parseInt(resizeEndTime)) : Infinity;
        const justFinishedResize = SchedulerMVP.justFinishedResize || 
                                  (hasJustResizedAttr && timeSinceResize < 500); // Within 500ms of resize
        
        if (justFinishedResize) {
            e.stopPropagation();
            e.stopImmediatePropagation();
            e.preventDefault();
            
            // Clean up attributes immediately
            bookingBlock.removeAttribute('data-just-resized');
            bookingBlock.removeAttribute('data-resize-end-time');
            
            return false;
        }
        
        // Trigger Blazor handler via DotNetHelper
        // Get booking data from attributes
        const bookingId = bookingBlock.getAttribute('data-booking-id');
        const bookingType = bookingBlock.getAttribute('data-booking-type');
        
        if (SchedulerMVP.dotNetHelper && bookingId) {
            // Call the appropriate Blazor method based on booking type
            if (bookingType === 'calendar') {
                SchedulerMVP.dotNetHelper.invokeMethodAsync('OnCalendarBookingClickFromJS', bookingId, e.clientX, e.clientY)
                    .catch(err => console.error('Error calling OnCalendarBookingClickFromJS:', err));
            } else {
                SchedulerMVP.dotNetHelper.invokeMethodAsync('OnTemplateClickFromJS', bookingId, e.clientX, e.clientY)
                    .catch(err => console.error('Error calling OnTemplateClickFromJS:', err));
            }
        }
        
        // Prevent default to avoid any other handlers
        e.stopPropagation();
        e.preventDefault();
        return false;
    },

    handleDragStart: function (e) {
        // Don't start drag if clicking on resize handle
        if (e.target.classList.contains('resize-handle') || e.target.closest('.resize-handle')) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        }
        
        const bookingBlock = e.target.closest('.booking-block');
        if (!bookingBlock) {
            e.preventDefault();
            return false;
        }
        
        // CRITICAL: Mark that drag is in progress to prevent refreshDragDrop from removing listeners
        SchedulerMVP.isDragging = true;
        
        // CRITICAL: Stop propagation to prevent any click handlers from firing
        e.stopPropagation();

        // Store booking data
        const bookingId = bookingBlock.getAttribute('data-booking-id');
        const bookingType = bookingBlock.getAttribute('data-booking-type'); // 'template' or 'calendar'
        const originalDay = bookingBlock.getAttribute('data-day');
        const originalStartMin = parseInt(bookingBlock.getAttribute('data-start-min'));
        const originalEndMin = parseInt(bookingBlock.getAttribute('data-end-min'));
        const duration = originalEndMin - originalStartMin;

        e.dataTransfer.setData('text/plain', JSON.stringify({
            bookingId: bookingId,
            bookingType: bookingType,
            originalDay: originalDay,
            originalStartMin: originalStartMin,
            originalEndMin: originalEndMin,
            duration: duration
        }));

        // Visual feedback - make original block transparent when dragging
        bookingBlock.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        
        // Store duration globally for preview
        SchedulerMVP.currentDragDuration = duration;
        
        // Store original position for conflict rollback
        SchedulerMVP.currentDragData = {
            bookingId: bookingId,
            bookingType: bookingType,
            originalDay: parseInt(originalDay),
            originalStartMin: originalStartMin,
            originalEndMin: originalEndMin
        };
        SchedulerMVP.hasConflict = false;
        
        // Store reference to dragged block - but DON'T update its time
        // The original block stays in place and keeps its original time
        SchedulerMVP.draggedBlock = bookingBlock;
        
        // Store the original block's width/position style to apply to preview
        // Get the inline style or computed style for width/position
        const computedStyle = window.getComputedStyle(bookingBlock);
        const rect = bookingBlock.getBoundingClientRect();
        const parentRect = bookingBlock.parentElement?.getBoundingClientRect();
        
        // Store style information
        SchedulerMVP.originalBlockStyle = {
            left: computedStyle.left,
            right: computedStyle.right,
            width: computedStyle.width,
            // Also store the actual style attribute if it exists
            styleLeft: bookingBlock.style.left || null,
            styleRight: bookingBlock.style.right || null,
            styleWidth: bookingBlock.style.width || null
        };
        
        // Create our custom floating drag element FIRST - before setting drag image
        // This ensures it's ready to show immediately
        SchedulerMVP.createDragElement(bookingBlock, originalStartMin, originalEndMin);
        
        // COMPLETELY HIDE browser's default drag image (document icon)
        // Use multiple methods to ensure it works in all browsers
        
        // Method 1: Create a transparent 1x1 pixel image using data URL
        // This is the most reliable cross-browser method
        const img = new Image();
        img.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAUEBAAAACwAAAAAAQABAAACAkQBADs=';
        img.width = 1;
        img.height = 1;
        
        // Method 2: Create invisible clone as backup
        const hiddenClone = bookingBlock.cloneNode(true);
        hiddenClone.style.position = 'absolute';
        hiddenClone.style.top = '-9999px';
        hiddenClone.style.left = '-9999px';
        hiddenClone.style.opacity = '0';
        hiddenClone.style.visibility = 'hidden';
        hiddenClone.style.pointerEvents = 'none';
        hiddenClone.style.width = '1px';
        hiddenClone.style.height = '1px';
        hiddenClone.style.overflow = 'hidden';
        document.body.appendChild(hiddenClone);
        
        // Force render of clone
        void hiddenClone.offsetWidth;
        
        // Try to set drag image - use image first, fallback to clone
        try {
            // Wait for image to load, then use it
            if (img.complete) {
                e.dataTransfer.setDragImage(img, 0, 0);
            } else {
                img.onload = function() {
                    try {
                        e.dataTransfer.setDragImage(img, 0, 0);
                    } catch (e) {}
                };
                // Fallback to clone if image not ready
                e.dataTransfer.setDragImage(hiddenClone, 0, 0);
            }
        } catch (err) {
            // Use clone as fallback
            try {
                e.dataTransfer.setDragImage(hiddenClone, 0, 0);
            } catch (err2) {
                // Last resort: transparent canvas
                const canvas = document.createElement('canvas');
                canvas.width = 1;
                canvas.height = 1;
                e.dataTransfer.setDragImage(canvas, 0, 0);
            }
        }
        
        // Clean up clone after a brief delay
        setTimeout(() => {
            if (hiddenClone.parentElement) {
                hiddenClone.remove();
            }
        }, 100);
        
        // Also set dropEffect to prevent any default visuals
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.dropEffect = 'move';
        
        // Show our drag element IMMEDIATELY and position it at mouse
        // This must happen synchronously in dragstart to override browser's drag image
        if (SchedulerMVP.dragElement) {
            SchedulerMVP.dragElement.style.display = 'block';
            SchedulerMVP.dragElement.style.visibility = 'visible';
            SchedulerMVP.dragElement.style.opacity = '1';
            SchedulerMVP.dragElement.style.zIndex = '99999';
            // Position it at mouse immediately
            SchedulerMVP.dragElement.style.left = (e.clientX + 10) + 'px';
            SchedulerMVP.dragElement.style.top = (e.clientY - 20) + 'px';
        }
        
        // Also use requestAnimationFrame to ensure it stays visible
        requestAnimationFrame(() => {
            if (SchedulerMVP.dragElement) {
                SchedulerMVP.dragElement.style.display = 'block';
                SchedulerMVP.dragElement.style.visibility = 'visible';
                SchedulerMVP.dragElement.style.opacity = '1';
                SchedulerMVP.dragElement.style.zIndex = '99999';
            }
        });
    },

    handleDragEnd: function (e) {
        const bookingBlock = e.target.closest('.booking-block') || SchedulerMVP.draggedBlock;
        if (bookingBlock) {
            bookingBlock.classList.remove('dragging');
            // Original block keeps its time - server will update it after successful drop
            // No need to restore time as original block was never modified
        }

        // CRITICAL: Mark that drag is finished - allow refreshDragDrop again
        SchedulerMVP.isDragging = false;

        // Clean up floating drag element
        SchedulerMVP.cleanupDragElement();

        // Clear any pending conflict check
        if (SchedulerMVP.pendingConflictCheck) {
            clearTimeout(SchedulerMVP.pendingConflictCheck);
            SchedulerMVP.pendingConflictCheck = null;
        }

        // Remove preview and reset state
        SchedulerMVP.removeDropPreview();
        SchedulerMVP.currentDragDuration = null;
        SchedulerMVP.lastConflictKey = null;
        SchedulerMVP.hasConflict = false;
        SchedulerMVP.draggedBlock = null;
        SchedulerMVP.wasDropped = false;
        SchedulerMVP.originalBlockStyle = null;
    },

    handleDragEnter: function (e) {
        e.preventDefault();
        e.stopPropagation();
        const dayContent = e.currentTarget;
        // Ensure drag element is visible when entering drop zone
        if (SchedulerMVP.dragElement) {
            SchedulerMVP.dragElement.style.display = 'block';
            SchedulerMVP.dragElement.style.visibility = 'visible';
        }
    },

    handleDragLeave: function (e) {
        e.preventDefault();
        const dayContent = e.currentTarget;
        
        // Clear any pending conflict check
        if (SchedulerMVP.pendingConflictCheck) {
            clearTimeout(SchedulerMVP.pendingConflictCheck);
            SchedulerMVP.pendingConflictCheck = null;
        }
        
        // Only remove preview if we're actually leaving the day-content element
        // relatedTarget might be null or point to a child, so check carefully
        const relatedTarget = e.relatedTarget;
        if (!relatedTarget || !dayContent.contains(relatedTarget)) {
            SchedulerMVP.removeDropPreview();
            SchedulerMVP.lastConflictKey = null;
            SchedulerMVP.hasConflict = false;
        }
    },

    handleDragOver: function (e) {
        e.preventDefault();
        
        // Show drop preview at current mouse position FIRST
        const dayContent = e.currentTarget;
        
        // Get the week-grid container (the scrolling element)
        const weekGrid = document.querySelector('.week-grid');
        if (!weekGrid) return;
        
        // Get headers (both are sticky and don't scroll)
        const timeHeader = document.querySelector('.time-header');
        const timeHeaderHeight = timeHeader ? timeHeader.getBoundingClientRect().height : 0;
        
        // Get day-header for this column (sticky, doesn't scroll)
        const dayColumn = dayContent.parentElement;
        const dayHeader = dayColumn ? dayColumn.querySelector('.day-header') : null;
        const dayHeaderHeight = dayHeader ? dayHeader.getBoundingClientRect().height : 0;
        
        // Total header height (both headers are sticky)
        const totalHeaderHeight = Math.max(timeHeaderHeight, dayHeaderHeight);
        
        // Get week-grid's bounding rect
        const weekGridRect = weekGrid.getBoundingClientRect();
        
        // Get mouse Y position in viewport
        const mouseY = e.clientY;
        
        // Calculate offset from top of week-grid
        const offsetFromWeekGridTop = mouseY - weekGridRect.top;
        
        // Subtract header height (sticky headers don't scroll with content)
        const offsetFromContentStart = offsetFromWeekGridTop - totalHeaderHeight;
        
        // Get scroll position of week-grid
        const scrollTop = weekGrid.scrollTop;
        
        // Total offset = offset from content start + scroll position
        // This gives us the position in the full 1440px day-content/time-body
        const totalOffsetY = offsetFromContentStart + scrollTop;
        
        // Round to nearest 15 minutes (1px = 1 minute)
        const snappedMinutes = Math.max(0, Math.min(1439, Math.round(totalOffsetY / 15) * 15));
        
        // Calculate duration and end time
        const duration = SchedulerMVP.currentDragDuration || 90;
        const newEndMin = Math.min(1440, snappedMinutes + duration);
        
        // Show/update preview block FIRST (this must happen before conflict check)
        // This will update the time display automatically
        SchedulerMVP.showDropPreview(dayContent, snappedMinutes);
        
        // ALWAYS check for conflicts - this is critical for visual feedback
        const dayIndexAttr = dayContent.getAttribute('data-day-index');
        const dayIndex = dayIndexAttr ? parseInt(dayIndexAttr) : null;
        
        if (dayIndex !== null && SchedulerMVP.currentDragData && SchedulerMVP.dotNetHelper) {
            // Create a unique key for this conflict check
            const conflictKey = `${SchedulerMVP.currentDragData.bookingId}-${dayIndex}-${snappedMinutes}-${newEndMin}`;
            
            // Reset conflict state when position changes significantly
            const positionChanged = SchedulerMVP.lastConflictKey !== conflictKey;
            
            // Update the floating drag element (the one that follows the mouse)
            // The original block stays in place with its original time
            // Update both position and time when over a valid drop zone
            if (SchedulerMVP.dragElement) {
                SchedulerMVP.dragElement.style.display = 'block';
                SchedulerMVP.dragElement.style.left = (e.clientX + 10) + 'px';
                SchedulerMVP.dragElement.style.top = (e.clientY - 20) + 'px';
                // Update time in drag element
                if (SchedulerMVP.dragElementTimeElement) {
                    const startTime = SchedulerMVP.formatTime(snappedMinutes);
                    const endTime = SchedulerMVP.formatTime(newEndMin);
                    SchedulerMVP.dragElementTimeElement.textContent = startTime + ' - ' + endTime;
                }
            }
            
            // QUICK CLIENT-SIDE CHECK: Check for visual overlaps in DOM for immediate feedback
            const quickConflict = SchedulerMVP.checkQuickConflict(dayContent, snappedMinutes, newEndMin, SchedulerMVP.currentDragData.bookingId);
            
            // Update preview styling based on quick check
            if (SchedulerMVP.currentPreview) {
                if (quickConflict) {
                    // Quick conflict found - show red immediately
                    SchedulerMVP.currentPreview.classList.add('drop-preview-conflict');
                    SchedulerMVP.hasConflict = true;
                    if (SchedulerMVP.DEBUG) {
                        console.log('🔴 QUICK CONFLICT - Preview is RED immediately at', snappedMinutes, 'min');
                    }
                } else if (positionChanged) {
                    // Position changed and no quick conflict - reset to blue
                    SchedulerMVP.currentPreview.classList.remove('drop-preview-conflict');
                    SchedulerMVP.hasConflict = false;
                }
            }
            
            // Clear any pending conflict check
            if (SchedulerMVP.pendingConflictCheck) {
                clearTimeout(SchedulerMVP.pendingConflictCheck);
                SchedulerMVP.pendingConflictCheck = null;
            }
            
            // Check conflict with server for accurate result
            SchedulerMVP.pendingConflictCheck = setTimeout(() => {
                // Store key for this check
                const checkKey = conflictKey;
                
                // Validate prerequisites
                if (!SchedulerMVP.dotNetHelper || !SchedulerMVP.currentDragData) {
                    return;
                }
                
                // Get current preview element
                const previewElement = SchedulerMVP.currentPreview;
                if (!previewElement) {
                    return;
                }
                
                // Perform server-side conflict check
                SchedulerMVP.dotNetHelper.invokeMethodAsync('CheckBookingConflict', {
                    bookingId: SchedulerMVP.currentDragData.bookingId,
                    bookingType: SchedulerMVP.currentDragData.bookingType,
                    newDay: dayIndex,
                    newStartMin: snappedMinutes,
                    newEndMin: newEndMin
                }).then(hasConflict => {
                    // Always update preview if it exists - simpler logic
                    const currentPreview = SchedulerMVP.currentPreview;
                    
                    if (currentPreview && currentPreview.parentElement) {
                        // Update conflict state and key
                        SchedulerMVP.hasConflict = hasConflict;
                        SchedulerMVP.lastConflictKey = checkKey;
                        
                        // Update preview block styling - CRITICAL VISUAL FEEDBACK
                        if (hasConflict) {
                            currentPreview.classList.add('drop-preview-conflict');
                            if (SchedulerMVP.DEBUG) {
                                console.log('🔴 SERVER CONFLICT CONFIRMED! Preview is RED at', snappedMinutes, 'min');
                            }
                        } else {
                            currentPreview.classList.remove('drop-preview-conflict');
                            if (SchedulerMVP.DEBUG) {
                                console.log('🔵 No server conflict - Preview is BLUE at', snappedMinutes, 'min');
                            }
                        }
                        
                        // Update drag element time when server confirms position
                        // Note: We don't have e.clientX/Y here, so we'll update on next dragover
                    } else {
                        if (SchedulerMVP.DEBUG) {
                            console.warn('⚠️ Cannot update preview - element missing');
                        }
                    }
                }).catch(err => {
                    console.error('Error checking conflict:', err); // Always log errors
                    SchedulerMVP.hasConflict = false;
                    if (SchedulerMVP.currentPreview) {
                        SchedulerMVP.currentPreview.classList.remove('drop-preview-conflict');
                    }
                });
            }, 50); // 50ms debounce for server check
        } else {
            // No drag data or helper - reset conflict state
            SchedulerMVP.hasConflict = false;
            if (SchedulerMVP.currentPreview) {
                SchedulerMVP.currentPreview.classList.remove('drop-preview-conflict');
            }
        }
        
        // Update drop effect based on current conflict state
        if (SchedulerMVP.hasConflict) {
            e.dataTransfer.dropEffect = 'none';
        } else {
            e.dataTransfer.dropEffect = 'move';
        }
    },
    
    showDropPreview: function (dayContent, startMinutes) {
        // Remove existing preview if it's in a different container
        if (SchedulerMVP.currentPreview && SchedulerMVP.currentPreview.parentElement !== dayContent) {
            SchedulerMVP.removeDropPreview();
        }
        
        if (!SchedulerMVP.currentDragDuration) {
            if (SchedulerMVP.DEBUG) {
                console.warn('No drag duration set');
            }
            return;
        }
        
        const duration = SchedulerMVP.currentDragDuration;
        const height = duration; // 1px per minute
        
        // Check if preview already exists in this container
        if (SchedulerMVP.currentPreview && SchedulerMVP.currentPreview.parentElement === dayContent) {
            // Update existing preview position and size (keep width/position from original)
            // Use integer pixels to prevent sub-pixel rendering that causes visual "skew"
            SchedulerMVP.currentPreview.style.top = Math.floor(startMinutes) + 'px';
            SchedulerMVP.currentPreview.style.height = Math.floor(height) + 'px';
            // ABSOLUTELY NO transforms, rotation, or skew
            SchedulerMVP.currentPreview.style.transform = 'none';
            SchedulerMVP.currentPreview.style.rotate = '0deg';
            SchedulerMVP.currentPreview.style.transformOrigin = 'top left';
            // Ensure width/position are still correct (they should be, but verify)
            if (SchedulerMVP.draggedBlock) {
                const originalStyle = SchedulerMVP.draggedBlock.getAttribute('style');
                if (originalStyle) {
                    // Re-apply width/position in case they were lost
                    const styleParts = originalStyle.split(';');
                    for (const part of styleParts) {
                        const trimmed = part.trim();
                        if (trimmed.includes(':')) {
                            const colonIndex = trimmed.indexOf(':');
                            const prop = trimmed.substring(0, colonIndex).trim();
                            const value = trimmed.substring(colonIndex + 1).trim();
                            if (prop === 'left' || prop === 'right' || prop === 'width') {
                                SchedulerMVP.currentPreview.style[prop] = value;
                            }
                        }
                    }
                    // If left/right, ensure width is auto
                    if (SchedulerMVP.currentPreview.style.left && SchedulerMVP.currentPreview.style.right) {
                        SchedulerMVP.currentPreview.style.width = 'auto';
                    }
                }
            }
            // Drag element time is updated in handleDragOver where we have mouse coordinates
            return; // Preview exists and updated, no need to create new one
        }
        
        // Create new preview element (drop-zone) - no text, just visual indicator
        const preview = document.createElement('div');
        preview.className = 'drop-preview';
        preview.style.position = 'absolute';
        // Use integer pixels ONLY to prevent sub-pixel rendering that causes visual skew
        preview.style.top = Math.floor(startMinutes) + 'px';
        preview.style.height = Math.floor(height) + 'px';
        preview.style.pointerEvents = 'none';
        preview.style.zIndex = '999';
        // ABSOLUTELY NO transforms, rotation, or skew - keep it perfectly straight
        preview.style.transform = 'none';
        preview.style.transformOrigin = 'top left';
        preview.style.rotate = '0deg';
        preview.style.scale = '1';
        preview.style.border = '2px dashed #2597F3';
        preview.style.borderRadius = '6px';
        preview.style.boxSizing = 'border-box';
        preview.style.margin = '0';
        preview.style.padding = '0';
        // No content - just visual indicator
        
        // Apply same width/position as original block
        if (SchedulerMVP.draggedBlock) {
            // Copy the exact style attribute from the original block for width/position
            const originalStyle = SchedulerMVP.draggedBlock.getAttribute('style');
            if (originalStyle) {
                // Parse the style string and extract left/right/width
                const styleParts = originalStyle.split(';');
                let hasLeft = false;
                let hasRight = false;
                let hasWidth = false;
                
                for (const part of styleParts) {
                    const trimmed = part.trim();
                    if (!trimmed) continue;
                    
                    // Match CSS properties: "left: 2px", "width: 50%", "right: 2px"
                    if (trimmed.includes(':')) {
                        const colonIndex = trimmed.indexOf(':');
                        const prop = trimmed.substring(0, colonIndex).trim();
                        const value = trimmed.substring(colonIndex + 1).trim();
                        
                        // Only copy left, right, and width (ignore top, height as we set those separately)
                        if (prop === 'left') {
                            preview.style.left = value;
                            hasLeft = true;
                        } else if (prop === 'right') {
                            preview.style.right = value;
                            hasRight = true;
                        } else if (prop === 'width') {
                            preview.style.width = value;
                            hasWidth = true;
                        }
                    }
                }
                
                // If we have left/right, ensure width is auto (not set)
                if (hasLeft && hasRight) {
                    preview.style.width = 'auto';
                }
            }
            
            // Fallback: if nothing was copied, use full width
            if (!preview.style.left && !preview.style.right && !preview.style.width) {
                preview.style.left = '2px';
                preview.style.right = '2px';
            }
        } else {
            // Fallback to full width if no original block
            preview.style.left = '2px';
            preview.style.right = '2px';
        }
        
        dayContent.appendChild(preview);
        SchedulerMVP.currentPreview = preview;
        
        if (SchedulerMVP.DEBUG) {
            console.log('Preview block created at', startMinutes, 'px');
        }
    },
    
    // Create a floating drag element that follows the mouse and shows updated time
    createDragElement: function (bookingBlock, startMinutes, endMinutes) {
        // Get the group name from the original block
        const groupElement = bookingBlock.querySelector('.booking-group');
        const groupName = groupElement ? groupElement.textContent : '';
        
        // Get styling from original block
        const computedStyle = window.getComputedStyle(bookingBlock);
        const bgColor = computedStyle.backgroundColor;
        const borderColor = computedStyle.borderColor;
        const borderRadius = computedStyle.borderRadius;
        
        // Create a NEW element from scratch (don't clone to avoid any unwanted elements)
        const dragElement = document.createElement('div');
        dragElement.className = bookingBlock.className.replace('dragging', '').trim();
        dragElement.style.position = 'fixed';
        dragElement.style.opacity = '1'; // Fully opaque - this is the main visual feedback
        dragElement.style.transform = 'none'; // NO rotation - keep it straight
        dragElement.style.boxShadow = '0 4px 16px rgba(0,0,0,0.5)'; // Stronger shadow for better visibility
        dragElement.style.pointerEvents = 'none';
        dragElement.style.zIndex = '10001'; // Very high z-index - above everything including preview
        dragElement.style.cursor = 'grabbing';
        dragElement.style.backgroundColor = bgColor;
        dragElement.style.border = computedStyle.border;
        dragElement.style.borderRadius = borderRadius;
        dragElement.style.padding = computedStyle.padding;
        dragElement.style.fontSize = computedStyle.fontSize;
        dragElement.style.color = computedStyle.color;
        dragElement.style.boxSizing = 'border-box';
        dragElement.style.transformOrigin = 'top left';
        dragElement.classList.add('drag-floating-element');
        
        // Create content structure
        const content = document.createElement('div');
        content.className = 'booking-content';
        content.style.padding = '2px 4px';
        content.style.pointerEvents = 'none';
        
        // Add group name
        if (groupName) {
            const groupDiv = document.createElement('div');
            groupDiv.className = 'booking-group';
            groupDiv.textContent = groupName;
            content.appendChild(groupDiv);
        }
        
        // Add time (will be updated)
        const timeElement = document.createElement('div');
        timeElement.className = 'booking-time';
        const startTime = SchedulerMVP.formatTime(startMinutes);
        const endTime = SchedulerMVP.formatTime(endMinutes);
        timeElement.textContent = startTime + ' - ' + endTime;
        content.appendChild(timeElement);
        
        dragElement.appendChild(content);
        
        // Set width from original block
        const originalRect = bookingBlock.getBoundingClientRect();
        dragElement.style.width = originalRect.width + 'px';
        dragElement.style.height = originalRect.height + 'px';
        
        // Initially hide it (will be shown on first dragover)
        dragElement.style.display = 'none';
        dragElement.style.visibility = 'hidden';
        document.body.appendChild(dragElement);
        
        // Store reference to update it later
        SchedulerMVP.dragElement = dragElement;
        SchedulerMVP.dragElementTimeElement = timeElement;
    },
    
    // Update the floating drag element position and time
    updateDragElement: function (clientX, clientY, startMinutes, endMinutes) {
        if (SchedulerMVP.dragElement && SchedulerMVP.draggedBlock) {
            // Show the element and make it visible
            SchedulerMVP.dragElement.style.display = 'block';
            SchedulerMVP.dragElement.style.visibility = 'visible';
            SchedulerMVP.dragElement.style.opacity = '1';
            SchedulerMVP.dragElement.style.zIndex = '99999'; // Ensure highest z-index
            
            // Get the original block's computed width to maintain same width
            const originalRect = SchedulerMVP.draggedBlock.getBoundingClientRect();
            const duration = SchedulerMVP.currentDragDuration || 90;
            SchedulerMVP.dragElement.style.width = Math.round(originalRect.width) + 'px';
            SchedulerMVP.dragElement.style.height = Math.round(duration) + 'px';
            
            // Update position to follow mouse (offset slightly for better visibility)
            // Use integer pixel values and NO transforms to keep it perfectly straight
            SchedulerMVP.dragElement.style.left = Math.round(clientX + 10) + 'px';
            SchedulerMVP.dragElement.style.top = Math.round(clientY - 20) + 'px';
            // ABSOLUTELY NO rotation or transforms
            SchedulerMVP.dragElement.style.transform = 'none';
            SchedulerMVP.dragElement.style.rotate = '0deg';
            SchedulerMVP.dragElement.style.transformOrigin = 'top left';
            
            // Update time
            if (SchedulerMVP.dragElementTimeElement) {
                const startTime = SchedulerMVP.formatTime(startMinutes);
                const endTime = SchedulerMVP.formatTime(endMinutes);
                SchedulerMVP.dragElementTimeElement.textContent = startTime + ' - ' + endTime;
            }
        }
    },
    
    // Hide the floating drag element
    hideDragElement: function () {
        if (SchedulerMVP.dragElement) {
            SchedulerMVP.dragElement.style.display = 'none';
        }
    },
    
    // Clean up drag element
    cleanupDragElement: function () {
        if (SchedulerMVP.dragElement && SchedulerMVP.dragElement.parentElement) {
            SchedulerMVP.dragElement.remove();
        }
        SchedulerMVP.dragElement = null;
        SchedulerMVP.dragElementTimeElement = null;
    },
    
    removeDropPreview: function () {
        if (SchedulerMVP.currentPreview) {
            SchedulerMVP.currentPreview.remove();
            SchedulerMVP.currentPreview = null;
        }
    },
    
    // Quick client-side conflict check - checks for visual overlaps in DOM
    checkQuickConflict: function (dayContent, startMin, endMin, excludeBookingId) {
        try {
            // Get all booking blocks in this day column (any element with data-booking-id)
            // Booking blocks can have various classes, so we select by data attribute
            const bookingBlocks = dayContent.querySelectorAll('[data-booking-id]');
            let hasOverlap = false;
            
            for (const block of bookingBlocks) {
                const blockId = block.getAttribute('data-booking-id');
                // Skip the booking we're dragging
                if (blockId === excludeBookingId) {
                    continue;
                }
                
                // Get booking times from data attributes (preferred) or computed style
                let blockStartMin = parseInt(block.getAttribute('data-start-min'));
                if (isNaN(blockStartMin)) {
                    // Fallback to style.top (remove 'px' if present)
                    const topStyle = block.style.top || window.getComputedStyle(block).top;
                    blockStartMin = parseInt(topStyle) || 0;
                }
                
                let blockHeight = parseInt(block.style.height);
                if (isNaN(blockHeight)) {
                    // Fallback to computed height
                    const heightStyle = block.style.height || window.getComputedStyle(block).height;
                    blockHeight = parseInt(heightStyle) || 0;
                }
                
                // Also check data-end-min if available
                let blockEndMin = parseInt(block.getAttribute('data-end-min'));
                if (isNaN(blockEndMin)) {
                    blockEndMin = blockStartMin + blockHeight;
                }
                
                // Check for time overlap
                if (startMin < blockEndMin && endMin > blockStartMin) {
                    hasOverlap = true;
                    if (SchedulerMVP.DEBUG) {
                        console.log('⚠️ Quick conflict detected with booking', blockId, 'at', blockStartMin, '-', blockEndMin);
                    }
                    break;
                }
            }
            
            return hasOverlap;
        } catch (err) {
            if (SchedulerMVP.DEBUG) {
                console.error('Error in quick conflict check:', err);
            }
            return false;
        }
    },

    handleDrop: function (e) {
        e.preventDefault();
        e.stopPropagation();
        const dayContent = e.currentTarget;
        
        // Clear any pending conflict check
        if (SchedulerMVP.pendingConflictCheck) {
            clearTimeout(SchedulerMVP.pendingConflictCheck);
            SchedulerMVP.pendingConflictCheck = null;
        }
        
        // If there's a known conflict, don't drop
        if (SchedulerMVP.hasConflict) {
            console.log('Drop blocked due to conflict');
            // Remove preview immediately - no red background on column
            SchedulerMVP.removeDropPreview();
            SchedulerMVP.hasConflict = false;
            SchedulerMVP.currentDragData = null;
            SchedulerMVP.lastConflictKey = null;
            return;
        }
        
        // Remove preview before processing drop
        SchedulerMVP.removeDropPreview();

        try {
            const data = JSON.parse(e.dataTransfer.getData('text/plain'));
            const dayIndexAttr = dayContent.getAttribute('data-day-index');
            const dayIndex = dayIndexAttr ? parseInt(dayIndexAttr) : null;
            
            if (dayIndex === null || isNaN(dayIndex)) {
                console.error('Invalid day index');
                return;
            }
            
            if (!SchedulerMVP.currentDragData) {
                console.error('No drag data available');
                return;
            }
            
            // Calculate new time based on drop position
            // Use the preview position if available (more accurate)
            let newStartMin;
            if (SchedulerMVP.currentPreview && SchedulerMVP.currentPreview.parentElement === dayContent) {
                // Get the top position from the preview element (already snapped to 15 min)
                const previewTop = parseInt(SchedulerMVP.currentPreview.style.top) || 0;
                newStartMin = Math.max(0, Math.min(1439, previewTop));
            } else {
                // Fallback: calculate from mouse position (same logic as handleDragOver)
                const weekGrid = document.querySelector('.week-grid');
                if (!weekGrid) {
                    console.error('Week grid not found');
                    return;
                }
                
                // Get headers (both are sticky and don't scroll)
                const timeHeader = document.querySelector('.time-header');
                const timeHeaderHeight = timeHeader ? timeHeader.getBoundingClientRect().height : 0;
                
                // Get day-header for this column
                const dayColumn = dayContent.parentElement;
                const dayHeader = dayColumn ? dayColumn.querySelector('.day-header') : null;
                const dayHeaderHeight = dayHeader ? dayHeader.getBoundingClientRect().height : 0;
                
                // Total header height (both headers are sticky)
                const totalHeaderHeight = Math.max(timeHeaderHeight, dayHeaderHeight);
                
                // Get week-grid's bounding rect
                const weekGridRect = weekGrid.getBoundingClientRect();
                
                // Calculate offset from top of week-grid
                const offsetFromWeekGridTop = e.clientY - weekGridRect.top;
                
                // Subtract header height
                const offsetFromContentStart = offsetFromWeekGridTop - totalHeaderHeight;
                
                // Get scroll position
                const scrollTop = weekGrid.scrollTop;
                
                // Calculate total offset
                const totalOffsetY = offsetFromContentStart + scrollTop;
                
                // Round to nearest 15 minutes (1px = 1 minute)
                newStartMin = Math.max(0, Math.min(1439, Math.round(totalOffsetY / 15) * 15));
            }
            const duration = data.duration || 90; // Default 90 minutes
            const newEndMin = Math.min(1440, newStartMin + duration);

            // Final conflict check before dropping (synchronous wait)
            if (SchedulerMVP.dotNetHelper) {
                // Wait for conflict check to complete before dropping
                SchedulerMVP.dotNetHelper.invokeMethodAsync('CheckBookingConflict', {
                    bookingId: SchedulerMVP.currentDragData.bookingId,
                    bookingType: SchedulerMVP.currentDragData.bookingType,
                    newDay: dayIndex,
                    newStartMin: newStartMin,
                    newEndMin: newEndMin
                }).then(hasConflict => {
                    if (hasConflict) {
                        console.log('Drop blocked due to conflict detected at drop time');
                        // No visual feedback on column - just block the drop
                        // Clean up drag state
                        SchedulerMVP.currentDragData = null;
                        SchedulerMVP.hasConflict = false;
                        return;
                    }
                    
                    // No conflict - proceed with drop
                    // Check if dotNetHelper is still valid before calling
                    if (!SchedulerMVP.dotNetHelper) {
                        console.error('DotNetHelper is no longer available for HandleBookingDrop');
                        SchedulerMVP.currentDragData = null;
                        SchedulerMVP.hasConflict = false;
                        return;
                    }
                    
                    SchedulerMVP.dotNetHelper.invokeMethodAsync('HandleBookingDrop', {
                        bookingId: data.bookingId,
                        bookingType: data.bookingType,
                        newDay: dayIndex,
                        newStartMin: newStartMin,
                        newEndMin: newEndMin
                    }).then(() => {
                        // Mark as dropped so time won't be restored in dragEnd
                        SchedulerMVP.wasDropped = true;
                        // Clean up drag state after successful drop
                        // Server will update the booking time, so we don't need to restore original time
                        SchedulerMVP.currentDragData = null;
                        SchedulerMVP.hasConflict = false;
                    }).catch(err => {
                        console.error('Error calling HandleBookingDrop:', err);
                        // Clean up drag state even on error
                        SchedulerMVP.currentDragData = null;
                        SchedulerMVP.hasConflict = false;
                    });
                }).catch(err => {
                    console.error('Error checking conflict at drop time:', err);
                    // On error checking conflict, don't allow the drop to be unsafe
                    // Just clean up and show error
                    SchedulerMVP.currentDragData = null;
                    SchedulerMVP.hasConflict = false;
                    console.log('Drop cancelled due to error checking conflict');
                });
            } else {
                console.error('DotNetHelper not available for drop');
                // Clean up even if DotNetHelper is not available
                SchedulerMVP.currentDragData = null;
                SchedulerMVP.hasConflict = false;
            }
        } catch (error) {
            console.error('Error handling drop:', error);
            // Clean up on error
            SchedulerMVP.currentDragData = null;
            SchedulerMVP.hasConflict = false;
        }
        
        // Clean up other state
        SchedulerMVP.currentDragDuration = null;
        SchedulerMVP.lastConflictKey = null;
    },

    // Re-initialize after DOM updates
    refreshDragDrop: function (dotNetHelper) {
        // CRITICAL: Don't refresh if drag is in progress - this would remove event listeners!
        if (SchedulerMVP.isDragging) {
            // Just update the helper reference, don't reinitialize
            SchedulerMVP.dotNetHelper = dotNetHelper;
            return;
        }
        
        // Update the DotNetHelper reference immediately
        SchedulerMVP.dotNetHelper = dotNetHelper;
        
        // Small delay to ensure DOM is updated
        setTimeout(() => {
            // Double-check drag is not in progress before refreshing
            if (!SchedulerMVP.isDragging) {
                SchedulerMVP.initDragDrop(dotNetHelper);
                SchedulerMVP.refreshResize(dotNetHelper); // Enable resize functionality
            }
        }, 100);
    },
    
    // Initialize drag drop state
    currentDragDuration: null,
    currentPreview: null,
    currentDragData: null, // Store original position for conflict rollback
    hasConflict: false,
    pendingConflictCheck: null, // Timeout ID for debouncing conflict checks
    lastConflictKey: null, // Last conflict check key to avoid duplicate checks
    isDragging: false, // Track if drag operation is in progress
    
    // Check if drag operation is currently in progress
    isDragging: function() {
        return SchedulerMVP.isDragging === true;
    },
    
    // Resize functionality
    initResize: function (dotNetHelper) {
        try {
            // Store dotNetHelper for resize operations
            SchedulerMVP.dotNetHelper = dotNetHelper;
            
            // Remove existing listeners from all resize handles (handles are created by Blazor)
            document.querySelectorAll('.resize-handle').forEach(handle => {
                handle.removeEventListener('mousedown', SchedulerMVP.handleResizeStart, true);
            });
            
            // Find all booking blocks (handles are already created by Blazor)
            const bookingBlocks = document.querySelectorAll('.booking-block');
            if (SchedulerMVP.DEBUG) {
                console.log('initResize: Found', bookingBlocks.length, 'booking blocks');
            }
            
            // Add event listeners to existing resize handles (created by Blazor)
            bookingBlocks.forEach(block => {
                // Get handles that are already in the DOM (created by Blazor)
                const topHandle = block.querySelector('.resize-handle-top');
                const bottomHandle = block.querySelector('.resize-handle-bottom');
                
                if (topHandle) {
                    // Make sure handle is not draggable
                    topHandle.setAttribute('draggable', 'false');
                    // Add event listener
                    topHandle.addEventListener('mousedown', SchedulerMVP.handleResizeStart, true);
                    // Prevent drag and drop on resize handles
                    topHandle.addEventListener('dragstart', function(e) {
                        e.preventDefault();
                        e.stopPropagation();
                        return false;
                    }, true);
                }
                
                if (bottomHandle) {
                    // Make sure handle is not draggable
                    bottomHandle.setAttribute('draggable', 'false');
                    // Add event listener
                    bottomHandle.addEventListener('mousedown', SchedulerMVP.handleResizeStart, true);
                    // Prevent drag and drop on resize handles
                    bottomHandle.addEventListener('dragstart', function(e) {
                        e.preventDefault();
                        e.stopPropagation();
                        return false;
                    }, true);
                }
            });
            
        } catch (error) {
            console.error('Error initializing resize:', error);
        }
    },
    
    handleResizeStart: function (e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        
        // Clear the justFinishedResize flag when starting a new resize
        // This ensures that if user starts a new resize immediately after finishing one,
        // the flag won't interfere with normal resize operation
        SchedulerMVP.justFinishedResize = false;
        
        const handle = e.target;
        if (!handle.classList.contains('resize-handle')) {
            return;
        }
        
        const bookingBlock = handle.closest('.booking-block');
        if (!bookingBlock) {
            return;
        }
        
        // Don't allow resize on ghost bookings (they're from other areas)
        if (bookingBlock.classList.contains('booking--ghost')) {
            return;
        }
        
        // Only allow resize on primary bookings
        if (!bookingBlock.classList.contains('booking--primary')) {
            return;
        }
        
        const resizeType = handle.getAttribute('data-resize-type'); // 'start' or 'end'
        const bookingId = bookingBlock.getAttribute('data-booking-id');
        const bookingType = bookingBlock.getAttribute('data-booking-type');
        const startMin = parseInt(bookingBlock.getAttribute('data-start-min')) || 0;
        const endMin = parseInt(bookingBlock.getAttribute('data-end-min')) || 0;
        const dayAttr = bookingBlock.getAttribute('data-day'); // For calendar: date string (yyyy-MM-dd), for template: day index (1-7)
        
        // Use the actual stored times from the booking block, not calculated from handle position
        // This ensures we start from the actual booking times, not from any potentially adjusted position
        const initialHandleMinutes = resizeType === 'start' ? startMin : endMin;
        
        // Store resize state
        SchedulerMVP.resizeState = {
            bookingId: bookingId,
            bookingType: bookingType,
            resizeType: resizeType,
            startMin: startMin,
            endMin: endMin,
            initialY: e.clientY,
            initialHandleMinutes: initialHandleMinutes, // Store the initial handle position in minutes
            bookingBlock: bookingBlock,
            handle: handle,
            lastValidStartMin: startMin, // Store last valid position (no conflict)
            lastValidEndMin: endMin,
            hasConflict: false,
            pendingConflictCheck: null,
            lastConflictKey: null,
            dayAttr: dayAttr // Store day attribute for calendar bookings
        };
        
        // Add visual feedback
        bookingBlock.classList.add('resizing');
        handle.classList.add('resizing');
        
        // Add global mouse event listeners
        document.addEventListener('mousemove', SchedulerMVP.handleResizeMove, true);
        document.addEventListener('mouseup', SchedulerMVP.handleResizeEnd, true);
    },
    
    handleResizeMove: function (e) {
        if (!SchedulerMVP.resizeState) return;
        
        e.preventDefault();
        
        const state = SchedulerMVP.resizeState;
        const bookingBlock = state.bookingBlock;
        const dayContent = bookingBlock.closest('.day-content');
        
        if (!dayContent) return;
        
        // Get week-grid and headers (same calculation as drag and drop)
        const weekGrid = document.querySelector('.week-grid');
        if (!weekGrid) return;
        
        const timeHeader = document.querySelector('.time-header');
        const timeHeaderHeight = timeHeader ? timeHeader.getBoundingClientRect().height : 0;
        const dayColumn = dayContent.parentElement;
        const dayHeader = dayColumn ? dayColumn.querySelector('.day-header') : null;
        const dayHeaderHeight = dayHeader ? dayHeader.getBoundingClientRect().height : 0;
        const totalHeaderHeight = Math.max(timeHeaderHeight, dayHeaderHeight);
        
        const weekGridRect = weekGrid.getBoundingClientRect();
        const mouseY = e.clientY;
        const offsetFromWeekGridTop = mouseY - weekGridRect.top;
        const offsetFromContentStart = offsetFromWeekGridTop - totalHeaderHeight;
        const scrollTop = weekGrid.scrollTop;
        const totalOffsetY = offsetFromContentStart + scrollTop;
        
        // Round to nearest 15 minutes
        const snappedMinutes = Math.max(0, Math.min(1439, Math.round(totalOffsetY / 15) * 15));
        
        // Calculate new start or end time directly from snapped position
        // Don't use delta - use absolute position to avoid issues with initial offset
        let newStartMin = state.startMin;
        let newEndMin = state.endMin;
        
        if (state.resizeType === 'start') {
            // Resizing start time (top handle) - only change start
            newStartMin = snappedMinutes;
            // Ensure start is before end and minimum duration
            if (newStartMin >= state.endMin) {
                newStartMin = Math.max(0, state.endMin - 15); // Minimum 15 minutes
            }
            if (newStartMin < 0) {
                newStartMin = 0;
            }
            // Keep end time unchanged
            newEndMin = state.endMin;
        } else {
            // Resizing end time (bottom handle) - only change end
            newEndMin = snappedMinutes;
            // Ensure end is after start and minimum duration
            if (newEndMin <= state.startMin) {
                newEndMin = Math.min(1440, state.startMin + 15); // Minimum 15 minutes
            }
            if (newEndMin > 1440) {
                newEndMin = 1440;
            }
            // Keep start time unchanged
            newStartMin = state.startMin;
        }
        
        // Get day index for conflict check
        const dayIndexAttr = dayContent.getAttribute('data-day-index');
        const dayIndex = dayIndexAttr ? parseInt(dayIndexAttr) : null;
        
        // QUICK CLIENT-SIDE CONFLICT CHECK: Check for visual overlaps in DOM for immediate feedback
        const quickConflict = SchedulerMVP.checkQuickConflict(dayContent, newStartMin, newEndMin, state.bookingId);
        
        // Create conflict key for server check
        const conflictKey = `${state.bookingId}-${dayIndex}-${newStartMin}-${newEndMin}`;
        const positionChanged = state.lastConflictKey !== conflictKey;
        
        // Clear any pending conflict check
        if (state.pendingConflictCheck) {
            clearTimeout(state.pendingConflictCheck);
            state.pendingConflictCheck = null;
        }
        
        // If quick conflict detected, block resize and keep last valid position
        if (quickConflict) {
            // Don't update position - keep last valid position
            newStartMin = state.lastValidStartMin;
            newEndMin = state.lastValidEndMin;
            state.hasConflict = true;
            
            // Add visual feedback - red border/background
            bookingBlock.classList.add('resize-conflict');
            
        } else {
            // No quick conflict - this position might be valid
            // Update visual position first
            state.hasConflict = false;
            bookingBlock.classList.remove('resize-conflict');
            
            // Check with server for accurate conflict detection (debounced)
            if (dayIndex !== null && SchedulerMVP.dotNetHelper && positionChanged) {
                state.pendingConflictCheck = setTimeout(() => {
                    const checkKey = conflictKey;
                    const checkStartMin = newStartMin;
                    const checkEndMin = newEndMin;
                    
                    if (!SchedulerMVP.dotNetHelper || !state.bookingBlock) {
                        return;
                    }
                    
                    // Perform server-side conflict check
                    // For calendar bookings, use the date from data-day attribute
                    // For templates, use dayIndex
                    const conflictCheckData = {
                        bookingId: state.bookingId,
                        bookingType: state.bookingType,
                        newDay: dayIndex || 0,
                        newStartMin: checkStartMin,
                        newEndMin: checkEndMin
                    };
                    
                    // For calendar bookings, include the date from data-day attribute
                    if (state.bookingType === 'calendar' && state.dayAttr) {
                        // Check if dayAttr is a date string (yyyy-MM-dd) or day index
                        // Date strings contain dashes, day indices are just numbers
                        if (state.dayAttr.includes('-')) {
                            conflictCheckData.date = state.dayAttr; // It's a date string
                        }
                    }
                    
                    SchedulerMVP.dotNetHelper.invokeMethodAsync('CheckBookingConflict', conflictCheckData).then(hasConflict => {
                        // Only process if this is still the current check
                        if (state.lastConflictKey === checkKey && state.resizeState) {
                            if (hasConflict) {
                                // Server confirms conflict - revert to last valid position
                                state.hasConflict = true;
                                const validStart = state.lastValidStartMin;
                                const validEnd = state.lastValidEndMin;
                                
                                // Update visual position to valid position
                                bookingBlock.style.top = validStart + 'px';
                                bookingBlock.style.height = (validEnd - validStart) + 'px';
                                
                                // Update time display
                                const timeElement = bookingBlock.querySelector('.booking-time');
                                if (timeElement) {
                                    const startTime = SchedulerMVP.formatTime(validStart);
                                    const endTime = SchedulerMVP.formatTime(validEnd);
                                    timeElement.textContent = startTime + ' - ' + endTime;
                                }
                                
                                // Store valid values
                                state.newStartMin = validStart;
                                state.newEndMin = validEnd;
                                
                                // Add conflict styling
                                bookingBlock.classList.add('resize-conflict');
                            } else {
                                // No conflict - this position is valid, update last valid position
                                state.hasConflict = false;
                                state.lastValidStartMin = checkStartMin;
                                state.lastValidEndMin = checkEndMin;
                                bookingBlock.classList.remove('resize-conflict');
                                
                                // Ensure visual position matches (in case it was reverted)
                                bookingBlock.style.top = checkStartMin + 'px';
                                bookingBlock.style.height = (checkEndMin - checkStartMin) + 'px';
                                
                                // Update time display
                                const timeElement = bookingBlock.querySelector('.booking-time');
                                if (timeElement) {
                                    const startTime = SchedulerMVP.formatTime(checkStartMin);
                                    const endTime = SchedulerMVP.formatTime(checkEndMin);
                                    timeElement.textContent = startTime + ' - ' + endTime;
                                }
                                
                                // Store values
                                state.newStartMin = checkStartMin;
                                state.newEndMin = checkEndMin;
                            }
                        }
                    }).catch(err => {
                        console.error('Error checking resize conflict:', err);
                    });
                    
                    state.lastConflictKey = checkKey;
                }, 50); // Debounce server checks
            } else {
                // No server check needed - assume valid for now and update last valid position
                state.lastValidStartMin = newStartMin;
                state.lastValidEndMin = newEndMin;
            }
        }
        
        // Update visual position (always update, but may be reverted by server check)
        bookingBlock.style.top = newStartMin + 'px';
        bookingBlock.style.height = (newEndMin - newStartMin) + 'px';
        
        // Update time display in the booking block if it exists
        const timeElement = bookingBlock.querySelector('.booking-time');
        if (timeElement) {
            const startTime = SchedulerMVP.formatTime(newStartMin);
            const endTime = SchedulerMVP.formatTime(newEndMin);
            timeElement.textContent = startTime + ' - ' + endTime;
        }
        
        // Store new values
        state.newStartMin = newStartMin;
        state.newEndMin = newEndMin;
    },
    
    handleResizeEnd: function (e) {
        if (!SchedulerMVP.resizeState) return;
        
        e.preventDefault();
        
        const state = SchedulerMVP.resizeState;
        const bookingBlock = state.bookingBlock;
        const handle = state.handle;
        
        // Clear any pending conflict check
        if (state.pendingConflictCheck) {
            clearTimeout(state.pendingConflictCheck);
            state.pendingConflictCheck = null;
        }
        
        // Remove visual feedback
        bookingBlock.classList.remove('resizing');
        bookingBlock.classList.remove('resize-conflict');
        handle.classList.remove('resizing');
        
        // Remove global listeners
        document.removeEventListener('mousemove', SchedulerMVP.handleResizeMove, true);
        document.removeEventListener('mouseup', SchedulerMVP.handleResizeEnd, true);
        
        // Use last valid position (no conflict) - this is the position we should save
        // If there was a conflict, lastValid will be the original position or last valid move
        const finalStartMin = state.lastValidStartMin !== undefined ? state.lastValidStartMin : state.startMin;
        const finalEndMin = state.lastValidEndMin !== undefined ? state.lastValidEndMin : state.endMin;
        
        // Only save if values actually changed and there's no conflict
        const hasChanged = (finalStartMin !== state.startMin || finalEndMin !== state.endMin);
        const hasConflict = state.hasConflict;
        
        // Save if values changed, no conflict, and dotNetHelper is available
        if (hasChanged && !hasConflict && SchedulerMVP.dotNetHelper) {
            SchedulerMVP.dotNetHelper.invokeMethodAsync('HandleBookingResize', {
                bookingId: state.bookingId,
                bookingType: state.bookingType,
                newStartMin: finalStartMin,
                newEndMin: finalEndMin
            }).then(() => {
                // Success - booking updated in database
            }).catch(err => {
                console.error('Error calling HandleBookingResize:', err);
            });
        } else if (hasConflict) {
            // Restore last valid position visually (which should be original if no valid move was made)
            bookingBlock.style.top = finalStartMin + 'px';
            bookingBlock.style.height = (finalEndMin - finalStartMin) + 'px';
            
            // Restore time display
            const timeElement = bookingBlock.querySelector('.booking-time');
            if (timeElement) {
                const startTime = SchedulerMVP.formatTime(finalStartMin);
                const endTime = SchedulerMVP.formatTime(finalEndMin);
                timeElement.textContent = startTime + ' - ' + endTime;
            }
        } else if (!SchedulerMVP.dotNetHelper) {
            console.error('No dotNetHelper available for resize!');
        }
        
        // Remove resizing marker
        bookingBlock.removeAttribute('data-resizing');
        
        // CRITICAL: Set flag IMMEDIATELY (synchronously) to prevent click event
        // The click event can fire almost instantly after mouseup, so set flag FIRST
        SchedulerMVP.justFinishedResize = true;
        bookingBlock.setAttribute('data-just-resized', 'true');
        
        // Store timestamp of when resize ended (for additional safety check)
        const resizeEndTime = Date.now();
        bookingBlock.setAttribute('data-resize-end-time', resizeEndTime.toString());
        
        // Clear resize state
        SchedulerMVP.resizeState = null;
        
        // IMPORTANT: Stop propagation of mouseup event
        // This may help prevent the click event from firing
        if (e) {
            e.stopPropagation();
            e.stopImmediatePropagation();
            e.preventDefault();
        }
        
        // Clear flag after delay (500ms to be safe - covers slow click events)
        setTimeout(() => {
            SchedulerMVP.justFinishedResize = false;
            bookingBlock.removeAttribute('data-just-resized');
            bookingBlock.removeAttribute('data-resize-end-time');
        }, 500);
    },
    
    // Re-initialize resize after DOM updates
    refreshResize: function (dotNetHelper) {
        // Try multiple times with increasing delays to ensure DOM is ready
        setTimeout(() => {
            SchedulerMVP.initResize(dotNetHelper);
        }, 50);
        setTimeout(() => {
            SchedulerMVP.initResize(dotNetHelper);
        }, 200);
        setTimeout(() => {
            SchedulerMVP.initResize(dotNetHelper);
        }, 500);
    },
    
    // Check if a resize operation just finished (to prevent popover from opening)
    checkJustFinishedResize: function () {
        return SchedulerMVP.justFinishedResize === true;
    },
    
    // Resize state
    resizeState: null,
    
    // Flag to track if resize just finished (prevents popover from opening)
    justFinishedResize: false
};


