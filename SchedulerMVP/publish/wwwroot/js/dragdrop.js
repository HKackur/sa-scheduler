var SchedulerMVP = {
    scrollToPos: function (topPx) {
        try {
            // Scroll the entire week-grid container
            const weekGrid = document.querySelector('.week-grid');
            if (weekGrid) {
                weekGrid.scrollTop = Math.max(0, topPx);
            }
        } catch (_) { }
    }
};


