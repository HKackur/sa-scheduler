// List view print and export functionality
window.SchedulerMVP = window.SchedulerMVP || {};

/**
 * Print the list view
 * Triggers browser print dialog
 * 
 * Note: Headers and footers (date, URL, page numbers) must be manually disabled
 * in the browser's print dialog under "More settings" -> uncheck "Headers and footers"
 * This cannot be controlled programmatically due to browser security restrictions.
 */
SchedulerMVP.printListView = function() {
    try {
        window.print();
    } catch (error) {
        console.error('[ListPrintExport] Error printing:', error);
    }
};

/**
 * Export list view to PDF
 * For now, uses browser print dialog with PDF as destination
 * Can be enhanced later with jsPDF or html2pdf.js for better control
 * 
 * Note: Headers and footers (date, URL, page numbers) must be manually disabled
 * in the browser's print dialog under "More settings" -> uncheck "Headers and footers"
 * This cannot be controlled programmatically due to browser security restrictions.
 */
SchedulerMVP.exportListToPDF = function() {
    try {
        // Use browser print dialog - user can choose "Save as PDF" as destination
        // This is a simple solution that works cross-browser
        // Future: Could use jsPDF or html2pdf.js for programmatic PDF generation
        window.print();
    } catch (error) {
        console.error('[ListPrintExport] Error exporting to PDF:', error);
    }
};

