/*******************************
 * FastFood Product Detail Scripts
 *******************************/

// Variant selection
document.querySelectorAll('.variant-option').forEach(option => {
    option.addEventListener('click', function () {
        // Remove active class from all options
        document.querySelectorAll('.variant-option').forEach(opt => opt.classList.remove('active'));
        // Add active class to clicked option
        this.classList.add('active');
        // Update radio button
        this.querySelector('input[type="radio"]').checked = true;
        // Update selected variant ID
        document.getElementById('selectedVariantId').value = this.dataset.variantId;
        // Update price display
        const price = parseInt(this.dataset.price);
        document.querySelector('.product-price-large').textContent = price.toLocaleString('vi-VN') + '₫';
    });
});

// Quantity control
const quantityInput = document.getElementById('quantity');
const btnMinus = document.querySelector('.btn-minus');
const btnPlus = document.querySelector('.btn-plus');

btnMinus.addEventListener('click', function () {
    let currentValue = parseInt(quantityInput.value);
    if (currentValue > 1) {
        quantityInput.value = currentValue - 1;
    }
});

btnPlus.addEventListener('click', function () {
    let currentValue = parseInt(quantityInput.value);
    if (currentValue < 99) {
        quantityInput.value = currentValue + 1;
    }
});

// Form submission
document.querySelector('form').addEventListener('submit', function (e) {
    e.preventDefault();
    // Add to cart animation
    const button = this.querySelector('button[type="submit"]');
    const originalText = button.innerHTML;
    button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Đang thêm...';
    button.disabled = true;

    // Simulate adding to cart
    setTimeout(() => {
        this.submit();
    }, 1000);
});