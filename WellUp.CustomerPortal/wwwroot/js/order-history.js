/**
 * order-history.js
 * Handles order history related interactions
 */

document.addEventListener('DOMContentLoaded', function () {
    console.log("Order history script loaded");

    // Handle Cancel Order Modal
    const cancelModal = document.getElementById('cancelModal');
    if (cancelModal) {
        cancelModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            const button = event.relatedTarget;

            // Extract order ID from data-* attributes
            const orderId = button.getAttribute('data-order-id');
            console.log("Cancel modal opened for order ID:", orderId);

            // Update the hidden input value
            const cancelOrderIdInput = document.getElementById('cancelOrderId');
            if (cancelOrderIdInput) {
                cancelOrderIdInput.value = orderId;
                console.log("Set cancel order ID input to:", orderId);
            }
        });
    } else {
        console.warn("Cancel modal element not found");
    }

    // Handle Reorder Modal
    const reorderModal = document.getElementById('reorderModal');
    if (reorderModal) {
        reorderModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            const button = event.relatedTarget;

            // Extract order ID from data-* attributes
            const orderId = button.getAttribute('data-order-id');
            console.log("Reorder modal opened for order ID:", orderId);

            // Update the hidden input value
            const reorderOrderIdInput = document.getElementById('reorderOrderId');
            if (reorderOrderIdInput) {
                reorderOrderIdInput.value = orderId;
                console.log("Set reorder order ID input to:", orderId);
            }
        });
    } else {
        console.warn("Reorder modal element not found");
    }

    // Add loading state to buttons when clicked
    const forms = document.querySelectorAll('#cancelOrderForm, #reorderForm');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            console.log("Form submission:", this.id);

            const button = this.querySelector('button[type="submit"]');
            if (button) {
                // Save original button text
                const originalText = button.innerHTML;

                // Add loading spinner and change text
                if (button.classList.contains('btn-danger')) {
                    button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Cancelling...';
                } else {
                    button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Adding...';
                }

                // Disable button to prevent multiple submissions
                button.disabled = true;

                // Set a timeout to reset button if something goes wrong
                setTimeout(() => {
                    if (button.disabled) {
                        button.innerHTML = originalText;
                        button.disabled = false;
                    }
                }, 5000);
            }
        });
    });

    // Make entire table row clickable to view order details
    const orderRows = document.querySelectorAll('.order-history-table tbody tr');
    orderRows.forEach(row => {
        row.addEventListener('click', function (e) {
            // Ignore clicks on buttons or links
            if (e.target.tagName === 'BUTTON' || e.target.tagName === 'A' ||
                e.target.closest('button') || e.target.closest('a')) {
                return;
            }

            // Find the order detail link in this row and follow it
            const detailsLink = this.querySelector('.order-number a');
            if (detailsLink) {
                window.location.href = detailsLink.href;
            }
        });

        // Add pointer cursor to show the row is clickable
        row.style.cursor = 'pointer';
    });
});