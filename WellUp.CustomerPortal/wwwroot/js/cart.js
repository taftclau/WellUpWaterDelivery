/**
 * Cart page functionality
 * cart.js
 */

$(document).ready(function () {
    // Anti-forgery token for AJAX requests
    var antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();

    // === QUANTITY CONTROL FUNCTIONALITY ===
    // Ensure all event handlers are removed
    $('.qty-btn').off('click');

    // Handle quantity adjustments with AJAX
    $('.qty-btn').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation(); // Prevent event bubbling

        // Disable button to prevent double-clicking
        $(this).prop('disabled', true);

        var $button = $(this);
        var productId = $button.data('product-id');
        var action = $button.data('action');
        var $input = $('.qty-input[data-product-id="' + productId + '"]');
        var currentValue = parseInt($input.val()) || 1;
        var maxValue = parseInt($input.attr('max')) || 10;
        var minValue = parseInt($input.attr('min')) || 1;

        // Calculate new quantity
        var newValue;
        if (action === 'increase' && currentValue < maxValue) {
            newValue = currentValue + 1;
        } else if (action === 'decrease' && currentValue > minValue) {
            newValue = currentValue - 1;
        } else {
            // No change needed, re-enable button and exit
            $button.prop('disabled', false);
            return;
        }

        // AJAX request to update quantity
        $.ajax({
            url: '/Cart/UpdateQuantity',
            type: 'POST',
            data: {
                productId: productId,
                quantity: newValue,
                __RequestVerificationToken: antiForgeryToken
            },
            success: function (response) {
                // Redirect to refresh the page with updated cart
                window.location.reload();
            },
            error: function (xhr, status, error) {
                console.error("Error updating quantity:", error);
                alert("Failed to update quantity. Please try again.");
                $button.prop('disabled', false);
            }
        });
    });

    // === ORDER FORM FUNCTIONALITY ===
    // Combine date and time inputs into the hidden datetime input
    function updateDateTime() {
        var dateInput = $('#deliveryDate').val();
        var timeInput = $('#deliveryTime').val();

        if (dateInput && timeInput) {
            var datetimeString = dateInput + 'T' + timeInput + ':00';
            $('#preferredDeliveryTime').val(datetimeString);
        }
    }

    // Update initially
    updateDateTime();

    // Update on change
    $('#deliveryDate, #deliveryTime').on('change', updateDateTime);

    // Handle the main order form
    var $orderForm = $('#placeOrderForm');
    if ($orderForm.length) {
        $orderForm.on('submit', function (e) {
            // Prevent the default form submission
            e.preventDefault();

            var isValid = true;
            var $submitButton = $("#placeOrderBtn");

            // Validate required fields
            if ($('#preferredDeliveryTime').val() === '') {
                isValid = false;
                alert('Please select a delivery time');
            }

            // Validate refill service notes
            $('.cart-item').each(function () {
                var isRefill = $(this).find('textarea[required]').length > 0;
                if (isRefill) {
                    var notes = $(this).find('textarea').val().trim();
                    if (notes === '') {
                        isValid = false;
                        $(this).find('textarea').addClass('is-invalid');
                        alert('Please provide container details for refill service');
                    } else {
                        $(this).find('textarea').removeClass('is-invalid');
                    }
                }
            });

            // Validate address
            if ($('#AddressId').val() === '' || $('#AddressId').val() === '0') {
                isValid = false;
                alert('Please select a delivery address');
            }

            // If the form is valid, disable button and submit
            if (isValid) {
                $submitButton.prop("disabled", true);
                $submitButton.html('<i class="fas fa-spinner fa-spin me-2"></i>PROCESSING...');

                // Submit the form after validation passes
                this.submit();
            } else {
                // If validation fails, ensure button is enabled and properly labeled
                $submitButton.prop("disabled", false);
                $submitButton.html('<i class="fas fa-check-circle me-2"></i>PLACE ORDER');
            }
        });
    }
});