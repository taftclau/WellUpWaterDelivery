/**
 * WellUp Admin - Inventory Management JavaScript
 */

document.addEventListener('DOMContentLoaded', function () {
    // Initialize DataTables
    if ($.fn.DataTable) {
        initializeDataTables();
    }

    // Initialize tooltips
    if (typeof bootstrap !== 'undefined' && typeof bootstrap.Tooltip !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Stock status highlighting
    highlightStockStatus();

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        const alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(alert => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);

    // Product filtering
    const filterSelect = document.getElementById('productFilter');
    if (filterSelect) {
        filterSelect.addEventListener('change', function () {
            this.form.submit();
        });
    }

    // Search input with clear button
    const searchInput = document.getElementById('searchString');
    const clearSearch = document.getElementById('clearSearch');
    if (searchInput && clearSearch) {
        clearSearch.addEventListener('click', function () {
            searchInput.value = '';
            searchInput.form.submit();
        });

        // Show/hide clear button based on input content
        searchInput.addEventListener('input', function () {
            if (this.value.length > 0) {
                clearSearch.style.display = 'block';
            } else {
                clearSearch.style.display = 'none';
            }
        });

        // Initial state
        clearSearch.style.display = searchInput.value.length > 0 ? 'block' : 'none';
    }

    // Product image selection based on Container Type and Includes Refill
    // Only add these handlers on pages with these elements (Add/Edit product pages)
    if ($('#ContainerType').length && $('#IncludesRefill').length) {
        // When container type changes, suggest the appropriate default image
        $('#ContainerType').change(function () {
            var containerType = $(this).val();
            var includesRefill = $('#IncludesRefill').is(':checked');

            // Select appropriate default image based on container type and refill status
            if (containerType === 'slim') {
                $('#default-image-slim_container').prop('checked', true);
            } else if (containerType === 'round') {
                $('#default-image-round_container').prop('checked', true);
            } else if (containerType === '') {
                $('#default-image-refill').prop('checked', true);
            }
        });

        // When IncludesRefill changes, adjust the image if appropriate
        $('#IncludesRefill').change(function () {
            var containerType = $('#ContainerType').val();
            var includesRefill = $(this).is(':checked');

            // If it's a refill-only product, suggest the refill image
            if (containerType === '' || includesRefill && !containerType) {
                $('#default-image-refill').prop('checked', true);
            }
        });

        // Add CSS for the image selection
        $("<style>")
            .prop("type", "text/css")
            .html(`
                .image-radio .form-check-input {
                    display: none;
                }

                .image-radio .form-check-label {
                    cursor: pointer;
                    border: 2px solid transparent;
                    border-radius: 4px;
                    padding: 5px;
                    transition: all 0.2s ease;
                }

                .image-radio .form-check-input:checked + .form-check-label {
                    border-color: var(--bs-primary);
                    background-color: rgba(13, 110, 253, 0.1);
                }

                .image-radio .form-check-label:hover {
                    border-color: #ced4da;
                }
            `)
            .appendTo("head");
    }
});

/**
 * Initialize DataTables
 */
function initializeDataTables() {
    // Products table
    const productsTable = document.getElementById('productsTable');
    if (productsTable) {
        $(productsTable).DataTable({
            "pageLength": 10,
            "lengthMenu": [[10, 25, 50, -1], [10, 25, 50, "All"]],
            "responsive": true,
            "order": [], // Disable initial sorting to use server-side sort
            "language": {
                "emptyTable": "No products found",
                "zeroRecords": "No matching products found",
                "info": "Showing _START_ to _END_ of _TOTAL_ products",
                "infoEmpty": "Showing 0 to 0 of 0 products",
                "infoFiltered": "(filtered from _MAX_ total products)",
                "search": "Quick search:",
                "paginate": {
                    "first": "First",
                    "last": "Last",
                    "next": "Next",
                    "previous": "Previous"
                }
            }
        });
    }

    // Low stock table
    const lowStockTable = document.getElementById('lowStockTable');
    if (lowStockTable) {
        $(lowStockTable).DataTable({
            "pageLength": 10,
            "lengthMenu": [[10, 25, 50, -1], [10, 25, 50, "All"]],
            "responsive": true,
            "ordering": true,
            "order": [[1, "asc"]], // Order by stock quantity ascending
            "language": {
                "emptyTable": "No low stock products found",
                "info": "Showing _START_ to _END_ of _TOTAL_ low stock products",
                "infoEmpty": "Showing 0 to 0 of 0 low stock products"
            }
        });
    }

    // Inventory logs table
    const inventoryLogsTable = document.getElementById('inventoryLogsTable');
    if (inventoryLogsTable) {
        $(inventoryLogsTable).DataTable({
            "pageLength": 10,
            "lengthMenu": [[10, 25, 50, -1], [10, 25, 50, "All"]],
            "ordering": true,
            "order": [[0, "desc"]], // Sort by date descending
            "info": true,
            "autoWidth": false,
            "responsive": true,
            "language": {
                "emptyTable": "No inventory logs found"
            }
        });
    }
}

/**
 * Highlight stock status based on levels
 */
function highlightStockStatus() {
    const stockElements = document.querySelectorAll('[data-stock-status]');
    stockElements.forEach(element => {
        const status = element.getAttribute('data-stock-status');
        const quantity = parseInt(element.getAttribute('data-stock-quantity') || '0');
        const threshold = parseInt(element.getAttribute('data-stock-threshold') || '10');

        if (quantity <= 0) {
            element.classList.add('stock-out');
        } else if (quantity <= 5) {
            element.classList.add('stock-critical');
        } else if (quantity <= threshold) {
            element.classList.add('stock-low');
        } else {
            element.classList.add('stock-normal');
        }
    });
}

/**
 * Handle stock update quantity changes
 */
function updateStockSummary(currentStock) {
    const newStockInput = document.getElementById('NewStock');
    const changeAmountElement = document.getElementById('changeAmount');
    const changeAlertElement = document.getElementById('changeAlert');
    const changeMessageElement = document.getElementById('changeMessage');

    if (!newStockInput || !changeAmountElement || !changeAlertElement || !changeMessageElement) {
        return;
    }

    const newStock = parseInt(newStockInput.value) || 0;
    const change = newStock - currentStock;

    // Update change amount display
    changeAmountElement.textContent = `${change >= 0 ? '+' : ''}${change} units`;

    // Update alert message
    if (change === 0) {
        changeAlertElement.className = 'alert alert-info';
        changeMessageElement.textContent = 'Stock levels will remain the same.';
    } else if (change > 0) {
        changeAlertElement.className = 'alert alert-success';
        changeMessageElement.textContent = `Stock level will increase by ${change} units.`;
    } else {
        if (newStock === 0) {
            changeAlertElement.className = 'alert alert-danger';
            changeMessageElement.textContent = 'Stock will be reduced to zero. The product will be marked as out of stock.';
        } else if (newStock <= 5) {
            changeAlertElement.className = 'alert alert-warning';
            changeMessageElement.textContent = `Stock will be reduced by ${Math.abs(change)} units. Warning: Stock level will be critically low.`;
        } else {
            changeAlertElement.className = 'alert alert-warning';
            changeMessageElement.textContent = `Stock will be reduced by ${Math.abs(change)} units.`;
        }
    }
}

/**
 * Toggle availability status
 */
function toggleAvailability(productId, isCurrentlyAvailable) {
    const form = document.getElementById(`toggleAvailabilityForm_${productId}`);
    if (form) {
        if (confirm(`Are you sure you want to mark this product as ${isCurrentlyAvailable ? 'unavailable' : 'available'}?`)) {
            form.submit();
        }
    }
}