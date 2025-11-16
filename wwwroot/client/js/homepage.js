// Homepage JavaScript - FastFood Shop
let currentProductId = null;
let selectedVariantId = null;
let isProcessingCart = false; // Flag to prevent duplicate processing

// Debug: Check initial cart count on page load
console.log('Homepage loaded, checking initial cart count...');

// Wait for DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM loaded, binding cart button events');

    // Update cart count on page load
    updateCartCount();

    // Handle cart button click
    const cartButtons = document.querySelectorAll('.btnAddToCartHomepage');
    console.log('Found cart buttons:', cartButtons.length);

    cartButtons.forEach(button => {
        console.log('Binding click event to button:', button);
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            e.stopPropagation();

            // Prevent duplicate processing
            if (isProcessingCart) {
                console.log('Already processing cart, ignoring duplicate click');
                return;
            }

            const productId = this.dataset.productId;
            currentProductId = productId;

            console.log('Cart button clicked for product:', productId);

            try {
                isProcessingCart = true; // Set flag

                // Fetch product variants
                console.log('Fetching variants for product:', productId);
                const response = await fetch(`/api/products/${productId}/variants`);
                console.log('Response status:', response.status);

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const data = await response.json();
                console.log('Variants data received:', data);

                if (data.variants && data.variants.length > 0) {
                    console.log('Variants found:', data.variants.length);
                    // Always show modal to let user confirm, even for single variant
                    console.log('Showing modal for variant selection/confirmation');
                    showVariantModal(data.variants, data.productName);
                } else {
                    console.log('No variants found for product');
                    alert('Sản phẩm này hiện không có sẵn để bán.');
                }
            } catch (error) {
                console.error('Error fetching variants:', error);
                alert('Có lỗi xảy ra khi tải thông tin sản phẩm.');
            } finally {
                isProcessingCart = false; // Reset flag
            }
        });
    });
});

function showVariantModal(variants, productName) {
    const modalLabel = document.getElementById('variantModalLabel');
    const variantOptions = document.getElementById('variantOptions');

    modalLabel.textContent = `Chọn loại cho ${productName}`;
    variantOptions.innerHTML = '';

    variants.forEach((variant, index) => {
        const optionDiv = document.createElement('div');
        optionDiv.className = 'form-check mb-3';
        optionDiv.innerHTML = `
            <input class="form-check-input" type="radio" name="variantOption" id="variant_${variant.id}" value="${variant.id}" ${index === 0 ? 'checked' : ''}>
            <label class="form-check-label d-flex justify-content-between align-items-center" for="variant_${variant.id}">
                <span class="fw-bold">${variant.variantName}</span>
                <span class="text-primary fw-bold">${parseInt(variant.price).toLocaleString('vi-VN')}₫</span>
            </label>
        `;
        variantOptions.appendChild(optionDiv);
    });

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('variantModal'));
    modal.show();

    // Set default selection
    selectedVariantId = variants[0].id;

    // Handle radio button change
    document.querySelectorAll('input[name="variantOption"]').forEach(radio => {
        radio.addEventListener('change', function () {
            selectedVariantId = this.value;
        });
    });

    // Handle confirm button
    document.getElementById('confirmAddToCart').onclick = function () {
        modal.hide();
        addToCart(currentProductId, selectedVariantId, 1);
    };
}

function addToCart(productId, variantId, quantity) {
    const csrfToken = document.querySelector('meta[name="X-CSRF-TOKEN"]')?.getAttribute('content');

    fetch('/add-product-from-view-detail', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': csrfToken
        },
        body: `id=${productId}&variantId=${variantId}&quantity=${quantity}`
    })
        .then(response => {
            console.log('Response status:', response.status);
            console.log('Response headers:', response.headers.get('content-type'));

            // Check if response is ok
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Try to parse as JSON, but handle text response too
            return response.text().then(text => {
                console.log('Raw response:', text);
                try {
                    return JSON.parse(text);
                } catch (e) {
                    // If not JSON, treat as success with the text as message
                    return {
                        success: response.ok,
                        message: text || 'Thêm sản phẩm vào giỏ hàng thành công!',
                        isTextResponse: true
                    };
                }
            });
        })
        .then(data => {
            console.log('Cart add response:', data);

            // Check for success indicators
            const isSuccess = data.success === true ||
                (data.message && data.message.includes('thành công')) ||
                (data.status && data.status === 'success') ||
                response.ok;

            if (isSuccess) {
                // Show success message using jQuery Toast
                $.toast({
                    heading: 'Thành công',
                    text: data.message || 'Sản phẩm đã được thêm vào giỏ hàng!',
                    showHideTransition: 'slide',
                    icon: 'success',
                    position: 'top-right',
                    stack: 5,
                    hideAfter: 3000
                });

                // Update cart count in header
                updateCartCount();
            } else {
                // Show error message
                const errorMessage = data.message || 'Không thể thêm sản phẩm vào giỏ hàng.';
                $.toast({
                    heading: 'Lỗi',
                    text: errorMessage,
                    showHideTransition: 'slide',
                    icon: 'error',
                    position: 'top-right',
                    stack: 5,
                    hideAfter: 5000
                });
            }
        })
        .catch(error => {
            console.error('Error adding to cart:', error);
            $.toast({
                heading: 'Lỗi',
                text: 'Có lỗi xảy ra khi thêm vào giỏ hàng. Vui lòng thử lại.',
                showHideTransition: 'slide',
                icon: 'error',
                position: 'top-right',
                stack: 5,
                hideAfter: 5000
            });
        });
}

function updateCartCount() {
    console.log('updateCartCount called');
    fetch('/cart/count', {
        credentials: 'same-origin'  // Include cookies for authentication
    })
        .then(response => response.json())
        .then(data => {
            console.log('Cart count response:', data);
            var el = document.getElementById('sumCart');
            if (!el) {
                console.log('sumCart element not found');
                return;
            }
            var n = Number(data?.count ?? 0);
            console.log('Setting cart count to:', n);
            el.textContent = n;
            el.style.display = n > 0 ? 'inline-block' : 'none';
        })
        .catch(error => {
            console.error('Error updating cart count:', error);
        });
}