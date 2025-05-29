// site.js

document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Quantity control buttons
    const qtyBtns = document.querySelectorAll('.qty-btn');
    if (qtyBtns) {
        qtyBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const action = this.dataset.action;
                const input = this.closest('.input-group').querySelector('.qty-input');
                let currentValue = parseInt(input.value) || 1;

                if (action === 'increase') {
                    if (currentValue < 10) {
                        input.value = currentValue + 1;
                    }
                } else if (action === 'decrease') {
                    if (currentValue > 1) {
                        input.value = currentValue - 1;
                    }
                }
            });
        });
    }

    // Quantity input validation
    const qtyInputs = document.querySelectorAll('.qty-input');
    if (qtyInputs) {
        qtyInputs.forEach(input => {
            input.addEventListener('change', function () {
                let value = parseInt(this.value) || 1;

                if (value < 1) value = 1;
                if (value > 10) value = 10;

                this.value = value;
            });
        });
    }

    // Auto-dismiss alerts after 5 seconds
    const autoAlerts = document.querySelectorAll('.alert-dismissible');
    if (autoAlerts) {
        autoAlerts.forEach(alert => {
            setTimeout(() => {
                const bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            }, 5000);
        });
    }

    // Product category filtering
    const productCategoryBtns = document.querySelectorAll('.product-category');
    if (productCategoryBtns) {
        productCategoryBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const category = this.dataset.category;

                // Remove active class from all buttons
                productCategoryBtns.forEach(b => b.classList.remove('active'));
                // Add active class to clicked button
                this.classList.add('active');

                const productItems = document.querySelectorAll('.product-item');

                if (category === 'all') {
                    productItems.forEach(item => item.style.display = 'block');
                } else {
                    productItems.forEach(item => {
                        if (item.classList.contains(category)) {
                            item.style.display = 'block';
                        } else {
                            item.style.display = 'none';
                        }
                    });
                }
            });
        });
    }
});