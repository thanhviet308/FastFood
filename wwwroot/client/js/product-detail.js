/*******************************
 * FastFood Product Detail Scripts
 *******************************/

// Debug: Check if script is loading
console.log('🍔 Product Detail Script Loaded!');
console.log('📍 Current URL:', window.location.href);
console.log('📦 DOM Content:', document.readyState);

// Quantity control - wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Product detail JS loaded');
    
    const quantityInput = document.getElementById('quantity');
    const btnMinus = document.querySelector('.btn-minus');
    const btnPlus = document.querySelector('.btn-plus');

    console.log('Quantity input:', quantityInput);
    console.log('Btn minus:', btnMinus);
    console.log('Btn plus:', btnPlus);

    // Real-time validation for quantity input
    if (quantityInput) {
        quantityInput.addEventListener('input', function() {
            validateQuantityInput(this);
        });
        
        quantityInput.addEventListener('blur', function() {
            validateQuantityInput(this);
        });
    }

    if (btnMinus && quantityInput) {
        btnMinus.addEventListener('click', function () {
            let currentValue = parseInt(quantityInput.value) || 1;
            if (currentValue > 1) {
                quantityInput.value = currentValue - 1;
                validateQuantityInput(quantityInput);
            }
        });
    }

    if (btnPlus && quantityInput) {
        btnPlus.addEventListener('click', function () {
            let currentValue = parseInt(quantityInput.value) || 1;
            if (currentValue < 99) {
                quantityInput.value = currentValue + 1;
                validateQuantityInput(quantityInput);
            }
        });
    }

    // Variant selection - also wait for DOM
    const variantOptions = document.querySelectorAll('.variant-option');
    console.log('Variant options found:', variantOptions.length);
    
    document.querySelectorAll('.variant-option').forEach(option => {
        option.addEventListener('click', function () {
            // Kiểm tra xem variant này có đang active không
            if (this.classList.contains('disabled')) {
                console.log('Variant disabled, ignoring click');
                return;
            }
            
            console.log('Variant clicked:', this.dataset.variantId);
            // Remove active class from all options
            document.querySelectorAll('.variant-option:not(.disabled)').forEach(opt => {
                opt.classList.remove('active', 'is-valid');
                opt.classList.remove('invalid');
            });
            // Add active class to clicked option
            this.classList.add('active', 'is-valid');
            // Update radio button
            this.querySelector('input[type="radio"]').checked = true;
            // Update selected variant ID
            document.getElementById('selectedVariantId').value = this.dataset.variantId;
            // Update price display
            const price = parseInt(this.dataset.price);
            document.querySelector('.product-price-large').textContent = price.toLocaleString('vi-VN') + '₫';
            
            // Clear any variant validation errors
            clearVariantError();
            
            // Show success feedback briefly
            this.style.transform = 'scale(0.95)';
            setTimeout(() => {
                this.style.transform = '';
            }, 150);
        });
    });
});

// Real-time quantity validation
function validateQuantityInput(input) {
    let value = input.value.trim();
    
    // Remove non-numeric characters
    value = value.replace(/[^0-9]/g, '');
    
    // Convert to number
    let numValue = parseInt(value) || 1;
    
    // Enforce min/max limits
    if (numValue < 1) {
        numValue = 1;
        showQuantityError('Số lượng tối thiểu là 1');
        input.classList.add('is-invalid');
        input.classList.remove('is-valid');
    } else if (numValue > 99) {
        numValue = 99;
        showQuantityError('Số lượng tối đa là 99');
        input.classList.add('is-invalid');
        input.classList.remove('is-valid');
    } else {
        clearQuantityError();
        input.classList.remove('is-invalid');
        input.classList.add('is-valid');
    }
    
    // Update input value
    input.value = numValue;
}

// Show quantity validation error
function showQuantityError(message) {
    clearQuantityError(); // Clear existing error first
    
    const quantityControl = document.querySelector('.quantity-control');
    if (quantityControl) {
        const errorDiv = document.createElement('div');
        errorDiv.className = 'quantity-error text-danger small mt-1';
        errorDiv.innerHTML = `<i class="fas fa-exclamation-circle me-1"></i>${message}`;
        quantityControl.parentNode.appendChild(errorDiv);
        
        // Auto hide after 3 seconds
        setTimeout(() => {
            clearQuantityError();
        }, 3000);
    }
}

// Clear quantity validation error
function clearQuantityError() {
    const existingError = document.querySelector('.quantity-error');
    if (existingError) {
        existingError.remove();
    }
}

// Show variant validation error
function showVariantError(message) {
    clearVariantError(); // Clear existing error first
    
    const variantSection = document.querySelector('.variant-selection-section') || document.querySelector('.variant-option').parentNode;
    if (variantSection) {
        const errorDiv = document.createElement('div');
        errorDiv.className = 'variant-error text-danger small mt-2';
        errorDiv.innerHTML = `<i class="fas fa-exclamation-circle me-1"></i>${message}`;
        variantSection.parentNode.insertBefore(errorDiv, variantSection.nextSibling);
        
        // Auto hide after 3 seconds
        setTimeout(() => {
            clearVariantError();
        }, 3000);
    }
}

// Clear variant validation error
function clearVariantError() {
    const existingError = document.querySelector('.variant-error');
    if (existingError) {
        existingError.remove();
    }
}

// Form submission - wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
    // Prevent duplicate initialization
    if (window.productDetailInitialized) {
        console.log('Product detail already initialized, skipping...');
        return;
    }
    window.productDetailInitialized = true;
    
    console.log('Form submission setup starting...');
    console.log('DOM fully loaded, checking for form...');
    
    // Find form by action URL instead of asp attributes - specifically in the main content area
    let addToCartForm = document.querySelector('main form[action*="add-product-from-view-detail"], .product-detail-content form[action*="add-product-from-view-detail"], form[action*="add-product-from-view-detail"]');
    
    // If not found, try to find any form with the specific action
    if (!addToCartForm) {
        const allForms = document.querySelectorAll('form');
        console.log('Searching through all forms:', allForms.length);
        allForms.forEach((form, index) => {
            console.log(`Form ${index}: action="${form.action}", contains target: ${form.action.includes('add-product-from-view-detail')}`);
            if (form.action.includes('add-product-from-view-detail')) {
                addToCartForm = form;
                console.log('Found target form at index:', index);
            }
        });
    }
    
    console.log('Add to cart form:', addToCartForm);
    
    // Also check for submit button directly - specifically the add to cart button
    const submitButton = addToCartForm?.querySelector('button[type="submit"]:not(.dropdown-item)');
    console.log('Submit button found:', submitButton);
    console.log('Submit button text:', submitButton?.textContent);
    console.log('Submit button classes:', submitButton?.className);
    
    // Check if button is clickable
    if (submitButton) {
        console.log('Button disabled:', submitButton.disabled);
        console.log('Button style:', submitButton.style.cssText);
        console.log('Button computed style:', window.getComputedStyle(submitButton));
    }
    
    if (addToCartForm) {
        console.log('Form found, adding submit listener');
        
        // Also add click listener to submit button for extra safety
        const submitButton = addToCartForm.querySelector('button[type="submit"]:not(.dropdown-item)');
        if (submitButton) {
            submitButton.addEventListener('click', function(e) {
                console.log('🎯 Submit button clicked!');
                console.log('Click event details:', e);
                console.log('Button element:', this);
                console.log('Button position:', this.getBoundingClientRect());
                console.log('Form will be submitted automatically...');
                
                // Don't prevent the natural form submission - let it flow naturally
                // The form submit handler will take care of the rest
                
                // Also trigger the form submit event programmatically as backup
                setTimeout(() => {
                    console.log('🔄 Backup: Triggering form submit event...');
                    const event = new Event('submit', { bubbles: true, cancelable: true });
                    addToCartForm.dispatchEvent(event);
                }, 50);
            });
            
            // Add mouse events for debugging
            submitButton.addEventListener('mousedown', function(e) {
                console.log('Mouse down on button');
            });
            
            submitButton.addEventListener('mouseup', function(e) {
                console.log('Mouse up on button');
            });
        }
        
        // Remove any existing submit handlers to prevent conflicts
        addToCartForm.addEventListener('submit', function (e) {
            console.log('🚀 Form submit event triggered');
            console.log('📋 Form action:', this.action);
            console.log('👤 User authenticated:', document.getElementById('isUserAuthenticated')?.value);
            console.log('🔍 Form method:', this.method);
            console.log('📄 Form target:', this.target);
            
            try {
                e.preventDefault(); // Prevent default form submission
                e.stopPropagation(); // Stop event bubbling
                console.log('✅ Prevented default form submission');
                console.log('🛑 Stopped event propagation');
                
                const button = this.querySelector('button[type="submit"]:not(.dropdown-item)');
                console.log('Submit button:', button);
                
                if (!button) {
                    console.error('❌ Submit button not found');
                    return;
                }
                
                // Validation trước khi submit
                console.log('🔍 Starting validation...');
                if (!validateAddToCart()) {
                    console.log('❌ Validation failed');
                    return;
                }
                console.log('✅ Validation passed');
                
                const originalText = button.innerHTML;
                const originalClasses = button.className;
                button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Đang thêm...';
                button.disabled = true;
                button.classList.add('btn-loading');
                
                // Add loading class to form for visual feedback
                this.classList.add('form-loading');

                // Get form data
                const formData = new FormData(this);
                console.log('📤 Form data prepared');
                
                // Debug: Log all form data
                console.log('📋 Form data contents:');
                for (let [key, value] of formData.entries()) {
                    console.log(`  - ${key}: ${value}`);
                }
                
                // Send AJAX request
                console.log('📡 Sending AJAX request...');
                console.log('🌐 Request URL:', this.action);
                console.log('📋 Form data:');
                for (let [key, value] of formData.entries()) {
                    console.log(`  ${key}: ${value}`);
                }
                
                // Ensure we have the correct URL
                const requestUrl = this.action || '/add-product-from-view-detail';
                console.log('🌐 Final request URL:', requestUrl);
                
                fetch(requestUrl, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                })
                .then(response => {
                    console.log('📨 Response received:', response.status, response.statusText);
                    console.log('📨 Response headers:', response.headers.get('content-type'));
                    
                    // Check if response is successful
                    if (!response.ok) {
                        console.log('❌ HTTP Error:', response.status);
                        return response.text().then(text => {
                            console.log('📄 Error response text:', text);
                            try {
                                const errorData = JSON.parse(text);
                                return { success: false, message: errorData.message || `Lỗi ${response.status}: ${response.statusText}` };
                            } catch (e) {
                                return { success: false, message: text || `Lỗi ${response.status}: ${response.statusText}` };
                            }
                        });
                    }
                    
                    // Check if response is JSON
                    const contentType = response.headers.get('content-type');
                    if (!contentType || !contentType.includes('application/json')) {
                        console.log('⚠️ Response is not JSON, treating as text');
                        return response.text().then(text => {
                            console.log('📄 Response text:', text);
                            // Try to parse as JSON if possible
                            try {
                                return JSON.parse(text);
                            } catch (e) {
                                return { success: false, message: text || 'Có lỗi xảy ra khi thêm vào giỏ hàng' };
                            }
                        });
                    }
                    
                    return response.json();
                })
                .then(data => {
                    console.log('📊 Response data:', data);
                    if (data.success) {
                        console.log('✅ Success! Showing toast...');
                        // Show success message
                        showToast(data.message, 'success');
                        
                        // Add success animation to button
                        button.classList.add('success-animation');
                        setTimeout(() => {
                            button.classList.remove('success-animation');
                        }, 600);
                        
                        // Update cart badge if needed
                        updateCartBadge();
                        
                        // Reset button
                        button.innerHTML = originalText;
                        button.disabled = false;
                        button.classList.remove('btn-loading');
                        this.classList.remove('form-loading');
                        
                        // Optionally reset quantity
                        document.getElementById('quantity').value = 1;
                    } else {
                        console.log('❌ Server returned error:', data.message);
                        
                        // Kiểm tra nếu là lỗi yêu cầu đăng nhập
                        const errorMessage = data.message || 'Không thể thêm sản phẩm vào giỏ hàng.';
                        const isLoginRequired = errorMessage.includes('đăng nhập');
                        
                        showToast(errorMessage, isLoginRequired ? 'warning' : 'error');
                        
                        // Nếu yêu cầu đăng nhập, redirect sau 1.5 giây để nhanh hơn
                        if (isLoginRequired) {
                            console.log('Redirecting to login from product detail AJAX response in 1.5 seconds...');
                            setTimeout(() => {
                                window.location.href = '/login';
                            }, 1500);
                        }
                        
                        button.innerHTML = originalText;
                        button.disabled = false;
                        button.classList.remove('btn-loading');
                        this.classList.remove('form-loading');
                    }
                })
                .catch(error => {
                    console.error('💥 Network/JSON Error:', error);
                    console.error('Error name:', error.name);
                    console.error('Error message:', error.message);
                    console.error('Error stack:', error.stack);
                    
                    let errorMessage = 'Có lỗi xảy ra khi thêm vào giỏ hàng';
                    if (error.name === 'TypeError' && error.message.includes('fetch')) {
                        errorMessage = 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối internet.';
                    } else if (error.name === 'SyntaxError') {
                        errorMessage = 'Lỗi định dạng dữ liệu từ máy chủ.';
                    }
                    
                    showToast(errorMessage, 'error');
                    button.innerHTML = originalText;
                    button.disabled = false;
                    button.classList.remove('btn-loading');
                    this.classList.remove('form-loading');
                });
            } catch (error) {
                console.error('💥 Form submission error:', error);
                e.preventDefault(); // Double safety
                showToast('Có lỗi xảy ra khi xử lý form', 'error');
            }
        });
        
        console.log('✅ Submit listener added successfully');
    } else {
        console.log('❌ Add to cart form not found, trying alternative selectors...');
        
        // Try to find form by class or other attributes
        const forms = document.querySelectorAll('form');
        console.log('All forms found:', forms.length);
        forms.forEach((form, index) => {
            console.log(`Form ${index}: action=${form.action}, method=${form.method}`);
        });
    }
});

// Validation function for add to cart
function validateAddToCart() {
    console.log('🔍 Starting validateAddToCart function...');
    
    const quantityInput = document.getElementById('quantity');
    const selectedVariantId = document.getElementById('selectedVariantId');
    const variantOptions = document.querySelectorAll('.variant-option');
    const isUserAuthenticated = document.getElementById('isUserAuthenticated');
    let isValid = true;
    
    console.log('📊 Validation inputs:');
    console.log('  - quantityInput:', quantityInput?.value);
    console.log('  - selectedVariantId:', selectedVariantId?.value);
    console.log('  - variantOptions.length:', variantOptions.length);
    console.log('  - isUserAuthenticated:', isUserAuthenticated?.value);
    
    // Clear previous errors
    clearQuantityError();
    clearVariantError();
    
    // Không cần kiểm tra đăng nhập nữa - cho phép anonymous users
    console.log('✅ Continuing validation (anonymous users allowed)...');
    
    // Validate quantity
    if (!quantityInput || !quantityInput.value) {
        showQuantityError('Vui lòng nhập số lượng');
        quantityInput.focus();
        isValid = false;
    } else {
        const quantity = parseInt(quantityInput.value);
        if (isNaN(quantity) || quantity <= 0) {
            showQuantityError('Số lượng phải lớn hơn 0');
            quantityInput.focus();
            isValid = false;
        } else if (quantity > 99) {
            showQuantityError('Số lượng tối đa là 99');
            quantityInput.focus();
            isValid = false;
        }
    }
    
    // Validate variant selection (only if variants exist)
    if (variantOptions.length > 0) {
        const selectedVariant = document.querySelector('.variant-option.active');
        if (!selectedVariant) {
            showVariantError('Vui lòng chọn loại sản phẩm');
            // Scroll to variant section
            document.querySelector('.variant-option').scrollIntoView({ behavior: 'smooth', block: 'center' });
            isValid = false;
        } else if (!selectedVariantId || !selectedVariantId.value || selectedVariantId.value === '0') {
            showVariantError('Vui lòng chọn loại sản phẩm hợp lệ');
            isValid = false;
        }
    }
    
    // Additional validation for stock could be added here
    // For now, we'll let the server handle stock validation
    
    console.log('🔍 Validation result:', isValid);
    return isValid;
}

// Toast notification function
function showToast(message, type = 'info') {
    console.log(`🍞 Showing toast: ${message} (${type})`);
    
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.innerHTML = `
        <div class="toast-content">
            <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
            <span>${message}</span>
        </div>
    `;
    
    // Add to body
    document.body.appendChild(toast);
    console.log('📋 Toast element created and added to body');
    
    // Show with animation
    setTimeout(() => {
        console.log('🎬 Adding show class to toast');
        toast.classList.add('show');
    }, 100);
    
    // Remove after 3 seconds
    setTimeout(() => {
        console.log('🗑️ Removing toast');
        toast.classList.remove('show');
        setTimeout(() => {
            if (toast.parentNode) {
                document.body.removeChild(toast);
                console.log('✅ Toast removed');
            }
        }, 300);
    }, 3000);
}

// Update cart badge function
function updateCartBadge() {
    // Fetch cart count from server
    fetch('/api/cart/count')
        .then(response => response.json())
        .then(data => {
            const badge = document.getElementById('sumCart');
            if (badge && data !== undefined) {
                badge.textContent = data;
                badge.style.display = data > 0 ? 'inline-block' : 'none';
            }
        })
        .catch(error => console.error('Error updating cart badge:', error));
}

// Test toast function for debugging
function testToast() {
    console.log('🧪 Testing toast notifications...');
    showToast('🧪 Test thông báo thành công!', 'success');
    setTimeout(() => showToast('⚠️ Test cảnh báo!', 'info'), 1000);
    setTimeout(() => showToast('❌ Test lỗi!', 'error'), 2000);
}