/*******************************
 * FastFood Cart Scripts
 *******************************/

// Cart functionality
document.addEventListener('DOMContentLoaded', function() {
    initializeCart();
});

function initializeCart() {
    // Initialize spinner
    initializeSpinner();
    
    // Initialize back to top button
    initializeBackToTop();
    
    // Initialize quantity controls
    initializeQuantityControls();
    
    // Initialize promo code functionality
    initializePromoCode();
}

function initializeSpinner() {
    const spinner = document.getElementById('spinner');
    if (spinner) {
        setTimeout(function() {
            spinner.classList.remove('show');
        }, 1);
    }
}

function initializeBackToTop() {
    const backToTopButton = document.querySelector('.back-to-top');
    if (!backToTopButton) return;
    
    window.addEventListener('scroll', function() {
        if (window.pageYOffset > 300) {
            backToTopButton.style.display = 'block';
            backToTopButton.style.opacity = '1';
        } else {
            backToTopButton.style.display = 'none';
            backToTopButton.style.opacity = '0';
        }
    });
    
    backToTopButton.addEventListener('click', function(e) {
        e.preventDefault();
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

function initializeQuantityControls() {
    // Quantity button clicks
    document.querySelectorAll('.ff-quantity-btn').forEach(button => {
        button.addEventListener('click', function() {
            const cartDetailId = this.dataset.cartDetailId;
            const action = this.dataset.action;
            const input = document.querySelector(`.ff-quantity-input[data-cart-detail-id="${cartDetailId}"]`);
            
            if (!input) return;
            
            let currentValue = parseInt(input.value);
            
            if (action === 'increase' && currentValue < 99) {
                input.value = currentValue + 1;
            } else if (action === 'decrease' && currentValue > 1) {
                input.value = currentValue - 1;
            }
            
            // Update quantity via AJAX
            updateQuantity(cartDetailId, parseInt(input.value));
        });
    });
    
    // Quantity input changes
    document.querySelectorAll('.ff-quantity-input').forEach(input => {
        input.addEventListener('change', function() {
            const cartDetailId = this.dataset.cartDetailId;
            let value = parseInt(this.value);
            
            // Validate input
            if (value < 1) {
                this.value = 1;
                value = 1;
            } else if (value > 99) {
                this.value = 99;
                value = 99;
            }
            
            // Update quantity via AJAX
            updateQuantity(cartDetailId, value);
        });
    });
}

function initializePromoCode() {
    const promoButton = document.querySelector('.ff-btn-promo');
    const promoInput = document.querySelector('.ff-promo-input');
    
    if (!promoButton || !promoInput) return;
    
    promoButton.addEventListener('click', function() {
        const promoCode = promoInput.value.trim();
        if (promoCode) {
            applyPromoCode(promoCode);
        }
    });
    
    promoInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            const promoCode = this.value.trim();
            if (promoCode) {
                applyPromoCode(promoCode);
            }
        }
    });
}

function updateQuantity(cartDetailId, quantity) {
    const csrfToken = document.querySelector('meta[name="X-CSRF-TOKEN"]')?.getAttribute('content');
    
    fetch(`/api/cart/update-quantity/${cartDetailId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken
        },
        body: JSON.stringify({ quantity: quantity })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update item total
            const item = document.querySelector(`.ff-cart-item[data-cart-detail-id="${cartDetailId}"]`);
            if (item) {
                const totalElement = item.querySelector('.ff-item-total-price');
                if (totalElement) {
                    // Get price from the displayed price element (most reliable - what user sees)
                    let price = 0;
                    const priceElement = item.querySelector('.ff-cart-item-price');
                    
                    if (priceElement) {
                        const priceText = priceElement.textContent.trim();
                        // Remove 'đ' and all non-numeric characters (including dots), then parse
                        // Vietnamese format uses dots as thousands separator, so we remove them all
                        const cleanPrice = priceText.replace(/[^0-9]/g, "");
                        price = parseInt(cleanPrice, 10);
                    }
                    
                    // Fallback: try data attributes if price element not found
                    if (isNaN(price) || price === 0) {
                        let priceStr = item.getAttribute('data-item-price') || totalElement.getAttribute('data-price');
                        if (priceStr) {
                            // Remove all non-numeric characters (dots are thousands separator in Vietnamese)
                            const cleanPrice = priceStr.toString().replace(/[^0-9]/g, "");
                            price = parseInt(cleanPrice, 10);
                        }
                    }
                    
                    // Ensure price is valid
                    if (isNaN(price) || price <= 0) {
                        console.error('Invalid price:', price, 'from element:', priceElement?.textContent);
                        return;
                    }
                    
                    // Calculate total (price * quantity)
                    const newTotal = price * quantity; // No need to round for whole numbers
                    
                    // Format with Vietnamese number format (90.000)
                    // Convert to string and add dots every 3 digits from right
                    const totalStr = newTotal.toString();
                    const formattedTotal = totalStr.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
                    
                    totalElement.textContent = formattedTotal + 'đ';
                    totalElement.setAttribute('data-quantity', quantity);
                    totalElement.setAttribute('data-price', price);
                }
            }
            
            // Update cart summary
            const totalElement = document.querySelector('.ff-total-price');
            if (totalElement && data.newTotal !== undefined && data.newTotal !== null) {
                // Format total price with Vietnamese number format
                const totalPrice = typeof data.newTotal === 'number' ? data.newTotal : parseFloat(data.newTotal);
                if (!isNaN(totalPrice) && totalPrice >= 0) {
                    const roundedTotal = Math.round(totalPrice);
                    // Format with Vietnamese number format (55.000)
                    const formattedTotal = roundedTotal.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".");
                    totalElement.textContent = formattedTotal + 'đ';
                }
            }
            
            // Update cart count in header
            updateCartCount();
            
            // Show success message
            showToast('Thành công', 'Cập nhật số lượng thành công', 'success');
        } else {
            showToast('Lỗi', data.message || 'Có lỗi xảy ra', 'error');
        }
    })
    .catch(error => {
        console.error('Error updating quantity:', error);
        showToast('Lỗi', 'Có lỗi xảy ra khi cập nhật số lượng', 'error');
    });
}

function applyPromoCode(promoCode) {
    const csrfToken = document.querySelector('meta[name="X-CSRF-TOKEN"]')?.getAttribute('content');
    
    fetch('/api/cart/apply-promo', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken
        },
        body: JSON.stringify({ promoCode: promoCode })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showToast('Thành công', data.message, 'success');
            
            // Update total price
            const totalElement = document.querySelector('.ff-total-price');
            if (totalElement && data.newTotal) {
                totalElement.textContent = data.newTotal.toLocaleString('vi-VN') + 'đ';
            }
        } else {
            showToast('Thông báo', data.message, 'warning');
        }
    })
    .catch(error => {
        console.error('Error applying promo code:', error);
        showToast('Lỗi', 'Không thể áp dụng mã giảm giá', 'error');
    });
}

function updateCartCount() {
    fetch('/cart/count', {
        credentials: 'same-origin'
    })
    .then(response => response.json())
    .then(data => {
        // Try both old and new cart count element IDs for compatibility
        const cartCountElement = document.getElementById('cart-count') || document.getElementById('sumCart');
        if (cartCountElement) {
            const count = Number(data?.count ?? 0);
            cartCountElement.textContent = count;
            cartCountElement.style.display = count > 0 ? 'inline-block' : 'none';
        }
    })
    .catch(error => {
        console.error('Error updating cart count:', error);
    });
}

function showToast(heading, text, icon) {
    // Check if jQuery Toast is available
    if (typeof $ !== 'undefined' && $.toast) {
        $.toast({
            heading: heading,
            text: text,
            showHideTransition: 'slide',
            icon: icon,
            position: 'top-right',
            stack: 5,
            hideAfter: 3000
        });
    } else {
        // Fallback to browser notification
        console.log(`${heading}: ${text}`);
        
        // Create a simple toast notification
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${icon}`;
        toast.innerHTML = `
            <strong>${heading}</strong><br>
            ${text}
        `;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);
        
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(toast);
            }, 300);
        }, 3000);
    }
}

// Add some basic CSS for fallback toast
const style = document.createElement('style');
style.textContent = `
    .toast-notification {
        position: fixed;
        top: 20px;
        right: 20px;
        background: white;
        border-radius: 8px;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
        padding: 1rem;
        z-index: 9999;
        transform: translateX(100%);
        transition: transform 0.3s ease;
        max-width: 300px;
        font-size: 0.9rem;
    }
    
    .toast-notification.show {
        transform: translateX(0);
    }
    
    .toast-success {
        border-left: 4px solid #28a745;
        color: #155724;
    }
    
    .toast-error {
        border-left: 4px solid #dc3545;
        color: #721c24;
    }
    
    .toast-warning {
        border-left: 4px solid #ffc107;
        color: #856404;
    }
`;
document.head.appendChild(style);