// Fix Double Scrollbar Issue
(function() {
    'use strict';
    
    function fixScrollbars() {
        // Remove scrollbars from html and body
        var html = document.documentElement;
        var body = document.body;
        
        if (html) {
            html.style.overflow = 'hidden';
            html.style.overflowX = 'hidden';
            html.style.overflowY = 'hidden';
        }
        
        if (body) {
            body.style.overflow = 'hidden';
            body.style.overflowX = 'hidden';
            body.style.overflowY = 'hidden';
        }
        
        // Ensure only admin-content has scrollbar
        var adminContent = document.querySelector('.admin-content');
        if (adminContent) {
            adminContent.style.overflowY = 'auto';
            adminContent.style.overflowX = 'hidden';
            adminContent.style.maxHeight = '100vh';
        }
        
        // Remove overflow from admin-container
        var adminContainer = document.querySelector('.admin-container');
        if (adminContainer) {
            adminContainer.style.overflow = 'hidden';
            adminContainer.style.overflowX = 'hidden';
            adminContainer.style.overflowY = 'hidden';
        }
        
        // Ensure main has no overflow
        var main = document.querySelector('main');
        if (main) {
            main.style.overflow = 'visible';
            main.style.overflowX = 'visible';
            main.style.overflowY = 'visible';
        }
        
        console.log('✓ Scrollbar fix applied');
    }
    
    // Run immediately
    fixScrollbars();
    
    // Run after DOM is fully loaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', fixScrollbars);
    }
    
    // Run after window is fully loaded
    window.addEventListener('load', fixScrollbars);
    
    // Run periodically to catch any overrides
    setInterval(fixScrollbars, 1000);
})();
