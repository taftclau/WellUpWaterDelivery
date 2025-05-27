/**
 * WellUp Admin Dashboard JavaScript
 */

document.addEventListener('DOMContentLoaded', function () {
    // Elements
    const sidebar = document.getElementById('sidebar');
    const sidebarToggler = document.getElementById('sidebar-toggler');
    const sidebarClose = document.getElementById('sidebar-close');
    const overlay = document.querySelector('.overlay');

    // Function to manage sidebar tooltips
    const updateTooltips = function () {
        if (window.innerWidth <= 1200 || sidebar.classList.contains('sidebar-collapsed')) {
            document.querySelectorAll('.sidebar-menu li a, .sidebar-logout').forEach(item => {
                const text = item.querySelector('span')?.textContent || '';
                if (text) {
                    item.setAttribute('title', text);
                    if (typeof bootstrap !== 'undefined' && typeof bootstrap.Tooltip !== 'undefined') {
                        // Check if tooltip already exists
                        if (!item._tooltip) {
                            item._tooltip = new bootstrap.Tooltip(item, {
                                placement: 'right',
                                trigger: 'hover'
                            });
                        }
                    }
                }
            });
        } else {
            document.querySelectorAll('.sidebar-menu li a, .sidebar-logout').forEach(item => {
                item.removeAttribute('title');
                if (typeof bootstrap !== 'undefined' && typeof bootstrap.Tooltip !== 'undefined') {
                    if (item._tooltip) {
                        item._tooltip.dispose();
                        item._tooltip = null;
                    }
                }
            });
        }
    };

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
            sidebar.classList.remove('sidebar-collapsed');
            document.querySelector('.main-content').classList.remove('main-content-expanded');
        }

        // Update tooltips on resize
        updateTooltips();
    });

    // Handle manual sidebar collapse toggle (if you want to add this feature)
    const sidebarCollapseBtn = document.getElementById('sidebar-collapse');
    if (sidebarCollapseBtn) {
        sidebarCollapseBtn.addEventListener('click', function () {
            sidebar.classList.toggle('sidebar-collapsed');
            document.querySelector('.main-content').classList.toggle('main-content-expanded');
            updateTooltips();
        });
    }

    // Initialize sidebar state on page load
    if (window.innerWidth <= 1200) {
        sidebar.classList.add('sidebar-collapsed');
        document.querySelector('.main-content').classList.add('main-content-expanded');
    }

    // Initialize tooltips for collapsed sidebar
    updateTooltips();

    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Animate activity items on scroll
    const animateOnScroll = function () {
        const elements = document.querySelectorAll('.activity-item');

        elements.forEach(function (element, index) {
            setTimeout(function () {
                element.classList.add('show');
            }, index * 100);
        });
    };

    // Run once on page load
    animateOnScroll();

    // Add jQuery number animation plugin
    if (typeof jQuery !== 'undefined') {
        (function ($) {
            $.fn.animateNumber = function (options) {
                var settings = $.extend({
                    number: 0,
                    duration: 1000,
                    easing: 'swing',
                    numberStep: function (now, tween) {
                        var formatted = now.toFixed(0);
                        $(tween.elem).text(formatted);
                    }
                }, options);

                return this.each(function () {
                    var $this = $(this);
                    var start = parseInt($this.text().replace(/[^\d.-]/g, '')) || 0;

                    $({
                        value: start
                    }).animate({
                        value: settings.number
                    }, {
                        duration: settings.duration,
                        easing: settings.easing,
                        step: function (now) {
                            settings.numberStep(now, {
                                elem: $this[0],
                                start: start,
                                end: settings.number
                            });
                        }
                    });
                });
            };

            // Number step factories
            $.animateNumber = {
                numberStepFactories: {
                    append: function (suffix) {
                        return function (now, tween) {
                            $(tween.elem).text(now.toFixed(0) + suffix);
                        };
                    },
                    separator: function (separator) {
                        separator = separator || ',';
                        return function (now, tween) {
                            var floor = Math.floor(now);
                            var text = floor.toString().replace(/\B(?=(\d{3})+(?!\d))/g, separator);
                            $(tween.elem).text(text);
                        };
                    }
                }
            };
        }(jQuery));
    }
});