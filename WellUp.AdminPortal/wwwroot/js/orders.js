/**
 * WellUp Admin - Orders Management JavaScript 
 * orders.js
 */

// Function to update delivery status
function updateDeliveryStatus(orderId, deliveryId, status) {
    document.getElementById('statusOrderId').value = orderId;
    document.getElementById('statusDeliveryId').value = deliveryId;
    document.getElementById('statusDeliveryStatus').value = status;

    if (status === 'failed' && !confirm('This will cancel the order and return items to inventory. Continue?')) {
        return;
    }

    document.getElementById('deliveryStatusForm').submit();
}

// Document ready function
document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        var alerts = document.querySelectorAll('.alert-success, .alert-danger');
        alerts.forEach(function (alert) {
            var bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);

    // Clear Orders Data button confirmation
    var clearOrdersBtn = document.getElementById('clearOrdersBtn');
    if (clearOrdersBtn) {
        clearOrdersBtn.addEventListener('click', function () {
            if (confirm('WARNING: This will permanently delete ALL orders data. This action cannot be undone!\n\nAre you absolutely sure you want to continue?')) {
                document.getElementById('clearOrdersForm').submit();
            }
        });
    }

    // Make tables responsive for mobile
    function handleResponsiveness() {
        if (window.innerWidth < 768) {
            var tableResponsiveElements = document.querySelectorAll('.table-responsive');
            tableResponsiveElements.forEach(function (element) {
                element.classList.add('mobile-view');
            });
        } else {
            var tableResponsiveElements = document.querySelectorAll('.table-responsive');
            tableResponsiveElements.forEach(function (element) {
                element.classList.remove('mobile-view');
            });
        }
    }

    // Run on load
    handleResponsiveness();

    // And on resize
    window.addEventListener('resize', handleResponsiveness);
});