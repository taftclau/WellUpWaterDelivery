/**
 * customer-dashboard.js
 * WellUp Customer Dashboard JavaScript
 */

document.addEventListener('DOMContentLoaded', function () {
    // Elements
    const sidebar = document.getElementById('sidebar');
    const sidebarToggler = document.getElementById('sidebar-toggler');
    const sidebarClose = document.getElementById('sidebar-close');
    const overlay = document.querySelector('.overlay');

    // Make the water icon clickable to toggle sidebar
    const sidebarLogo = document.querySelector('.sidebar-logo');
    if (sidebarLogo) {
        sidebarLogo.addEventListener('click', function () {
            toggleSidebar();
        });
        sidebarLogo.style.cursor = 'pointer'; // Add pointer cursor
    }

    // Function to toggle sidebar state
    function toggleSidebar() {
        sidebar.classList.toggle('sidebar-collapsed');
        document.querySelector('.main-content').classList.toggle('main-content-expanded');

        // Update icon
        const icon = document.querySelector('#sidebar-collapse i');
        if (sidebar.classList.contains('sidebar-collapsed')) {
            if (icon) {
                icon.classList.remove('fa-chevron-left');
                icon.classList.add('fa-chevron-right');
            }
        } else {
            if (icon) {
                icon.classList.remove('fa-chevron-right');
                icon.classList.add('fa-chevron-left');
            }
        }

        // Save state to localStorage
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('sidebar-collapsed'));
    }

    // Toggle sidebar on mobile
    if (sidebarToggler) {
        sidebarToggler.addEventListener('click', function () {
            sidebar.classList.add('active');
            overlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        });
    }

    // Close sidebar on mobile
    if (sidebarClose) {
        sidebarClose.addEventListener('click', function () {
            sidebar.classList.remove('active');
            overlay.classList.remove('active');
            document.body.style.overflow = '';
        });
    }

    // Close sidebar when clicking overlay
    if (overlay) {
        overlay.addEventListener('click', function () {
            sidebar.classList.remove('active');
            overlay.classList.remove('active');
            document.body.style.overflow = '';
        });
    }

    // Handle window resize to adjust sidebar and collapsed state
    window.addEventListener('resize', function () {
        // Original mobile sidebar logic
        if (window.innerWidth >= 768) {
            sidebar.classList.remove('active');
            overlay.classList.remove('active');
            document.body.style.overflow = '';
        }

        // Auto collapse/expand sidebar based on screen width
        if (window.innerWidth <= 1200) {
            sidebar.classList.add('sidebar-collapsed');
            document.querySelector('.main-content').classList.add('main-content-expanded');
        } else {
            // Only expand if not manually collapsed (saved in localStorage)
            if (localStorage.getItem('sidebarCollapsed') !== 'true') {
                sidebar.classList.remove('sidebar-collapsed');
                document.querySelector('.main-content').classList.remove('main-content-expanded');
            }
        }
    });

    // Handle manual sidebar collapse toggle (with localStorage persistence)
    const sidebarCollapseBtn = document.getElementById('sidebar-collapse');
    if (sidebarCollapseBtn) {
        sidebarCollapseBtn.addEventListener('click', function () {
            toggleSidebar(); // Use the toggleSidebar function
        });
    }

    // Restore sidebar state from localStorage
    if (localStorage.getItem('sidebarCollapsed') === 'true') {
        sidebar.classList.add('sidebar-collapsed');
        document.querySelector('.main-content').classList.add('main-content-expanded');

        if (sidebarCollapseBtn) {
            const icon = sidebarCollapseBtn.querySelector('i');
            if (icon) {
                icon.classList.remove('fa-chevron-left');
                icon.classList.add('fa-chevron-right');
            }
        }
    }

    // Initialize sidebar state on page load
    if (window.innerWidth <= 1200 && localStorage.getItem('sidebarCollapsed') !== 'false') {
        sidebar.classList.add('sidebar-collapsed');
        document.querySelector('.main-content').classList.add('main-content-expanded');
    }

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        const alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function (alert) {
            if (bootstrap && bootstrap.Alert) {
                const bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            } else {
                alert.classList.add('fade');
                setTimeout(() => {
                    alert.remove();
                }, 150);
            }
        });
    }, 5000);

    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
});