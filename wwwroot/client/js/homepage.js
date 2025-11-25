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

    // Debug: Track product link clicks
    const productLinks = document.querySelectorAll('.ff-product-title a');
    console.log('Found product title links:', productLinks.length);
    
    productLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            console.log('Product link clicked:', this.href);
        });
    });

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
                console.log('Number of variants received:', data.variants ? data.variants.length : 0);
                
                // Debug: log each variant
                if (data.variants) {
                    data.variants.forEach((variant, index) => {
                        console.log(`Variant ${index + 1}:`, variant);
                    });
                }

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
    console.log(`🖼️ showVariantModal called with ${variants.length} variants for product: ${productName}`);
    
    const modalLabel = document.getElementById('variantModalLabel');
    const variantOptions = document.getElementById('variantOptions');

    modalLabel.textContent = `Chọn loại cho ${productName}`;
    variantOptions.innerHTML = '';

    console.log('Creating variant options in modal:');
    variants.forEach((variant, index) => {
        console.log(`  - Creating option ${index + 1}: ${variant.variantName} - ${variant.price}₫ (IsActive: ${variant.isActive})`);
        const optionDiv = document.createElement('div');
        optionDiv.className = 'form-check mb-3';
        
        // Check if variant is inactive
        const isInactive = variant.isActive === false;
        const disabledAttr = isInactive ? 'disabled' : '';
        const checkedAttr = isInactive ? '' : (index === 0 ? 'checked' : '');
        const inactiveLabel = isInactive ? ' <span class="text-muted">(Tạm hết)</span>' : '';
        
        optionDiv.innerHTML = `
            <input class="form-check-input" type="radio" name="variantOption" id="variant_${variant.id}" value="${variant.id}" ${checkedAttr} ${disabledAttr}>
            <label class="form-check-label d-flex justify-content-between align-items-center ${isInactive ? 'text-muted' : ''}" for="variant_${variant.id}">
                <span class="fw-bold">${variant.variantName}${inactiveLabel}</span>
                <span class="${isInactive ? 'text-muted' : 'text-primary'} fw-bold">${parseInt(variant.price).toLocaleString('vi-VN')}₫</span>
            </label>
        `;
        variantOptions.appendChild(optionDiv);
    });
    
    console.log(`✅ Created ${variants.length} variant options in modal`);

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('variantModal'));
    modal.show();

    // Set default selection to first active variant
    const firstActiveVariant = variants.find(v => v.isActive !== false);
    if (firstActiveVariant) {
        selectedVariantId = firstActiveVariant.id;
    }

    // Handle radio button change
    document.querySelectorAll('input[name="variantOption"]').forEach(radio => {
        radio.addEventListener('change', function () {
            selectedVariantId = this.value;
        });
    });

    // Handle confirm button - only add to cart if selected variant is active
    document.getElementById('confirmAddToCart').onclick = function () {
        const selectedRadio = document.querySelector('input[name="variantOption"]:checked');
        if (selectedRadio && selectedRadio.disabled) {
            // Don't allow adding inactive variants
            $.toast({
                heading: 'Thông báo',
                text: 'Sản phẩm này hiện đang tạm hết hàng. Vui lòng chọn loại khác.',
                showHideTransition: 'slide',
                icon: 'warning',
                position: 'top-right',
                stack: 5,
                hideAfter: 3000
            });
            return;
        }
        
        modal.hide();
        addToCart(currentProductId, selectedVariantId, 1);
    };
}

function addToCart(productId, variantId, quantity) {
    const csrfToken = document.querySelector('meta[name="X-CSRF-TOKEN"]')?.getAttribute('content');
    
    console.log('CSRF Token:', csrfToken);
    console.log('Product ID:', productId);
    console.log('Variant ID:', variantId);
    console.log('Quantity:', quantity);
    console.log('Request body:', `id=${productId}&variantId=${variantId}&quantity=${quantity}`);

    fetch('/add-product-from-view-detail', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': csrfToken || ''
        },
        body: `id=${productId}&variantId=${variantId}&quantity=${quantity}`
    })
        .then(response => {
            console.log('Response status:', response.status);
            console.log('Response headers:', response.headers.get('content-type'));
            console.log('Response ok:', response.ok);

            // Check if response is ok
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Try to parse as JSON, but handle text response too
            return response.text().then(text => {
                console.log('Raw response:', text);
                console.log('Response length:', text.length);
                
                // Try to parse as JSON
                try {
                    const jsonData = JSON.parse(text);
                    console.log('Parsed JSON:', jsonData);
                    return jsonData;
                } catch (e) {
                    console.log('Failed to parse as JSON, error:', e);
                    console.log('Treating as text response');
                    // If not JSON, treat as error
                    return {
                        success: false,
                        message: text || 'Có lỗi xảy ra khi thêm vào giỏ hàng',
                        isTextResponse: true
                    };
                }
            });
        })
        .then(data => {
            console.log('Cart add response:', data);
            console.log('Response success:', data.success);
            console.log('Response message:', data.message);

            // Check for success indicators - be more explicit
            const isSuccess = data.success === true;
            
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
                
                // Also update badge if count is returned
                if (data.count !== undefined) {
                    var $b = $('#sumCart');
                    if ($b.length) {
                        $b.text(data.count);
                        $b.css('display', data.count > 0 ? 'inline-block' : 'none');
                    }
                }
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