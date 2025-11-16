/*******************************
 * FastFood Checkout Scripts
 *******************************/

// Payment method selection
document.querySelectorAll('.payment-method').forEach(method => {
    method.addEventListener('click', function () {
        // Remove selected class from all methods
        document.querySelectorAll('.payment-method').forEach(m => m.classList.remove('selected'));
        // Add selected class to clicked method
        this.classList.add('selected');
        // Update hidden input
        document.getElementById('paymentMethod').value = this.dataset.method;
    });
});

// Form validation and submission
document.getElementById('checkoutForm').addEventListener('submit', function (e) {
    e.preventDefault();

    const submitBtn = this.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;

    // Show loading state
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Đang xử lý...';
    submitBtn.disabled = true;

    // Validate form
    let isValid = true;
    const requiredFields = ['fullName', 'phone', 'email', 'address'];

    requiredFields.forEach(field => {
        const input = document.getElementById(field);
        if (!input.value.trim()) {
            input.classList.add('is-invalid');
            isValid = false;
        } else {
            input.classList.remove('is-invalid');
            input.classList.add('is-valid');
        }
    });

    // Validate payment method
    const paymentMethod = document.getElementById('paymentMethod').value;
    if (!paymentMethod) {
        alert('Vui lòng chọn phương thức thanh toán!');
        isValid = false;
    }

    if (!isValid) {
        submitBtn.innerHTML = originalText;
        submitBtn.disabled = false;
        return;
    }

    // Simulate processing
    setTimeout(() => {
        this.submit();
    }, 2000);
});

// Remove validation classes on input
const inputs = document.querySelectorAll('.form-control');
inputs.forEach(input => {
    input.addEventListener('input', function () {
        this.classList.remove('is-valid', 'is-invalid');
    });
});

// Promo code functionality
document.getElementById('applyPromo')?.addEventListener('click', function () {
    const promoCode = document.getElementById('promoCode').value.trim();
    const promoResult = document.getElementById('promoResult');

    if (!promoCode) {
        promoResult.innerHTML = '<div class="text-danger"><i class="fas fa-exclamation-circle me-1"></i>Vui lòng nhập mã giảm giá</div>';
        return;
    }

    // Simulate promo code validation
    this.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Đang áp dụng...';
    this.disabled = true;

    setTimeout(() => {
        if (promoCode === 'FASTFOOD20' || promoCode === 'WELCOME10') {
            const discount = promoCode === 'FASTFOOD20' ? 20 : 10;
            promoResult.innerHTML = `<div class="text-success"><i class="fas fa-check-circle me-1"></i>Áp dụng thành công! Giảm ${discount}%</div>`;

            // Update total price
            const currentTotal = parseFloat(document.getElementById('totalPrice').textContent.replace(/[^0-9]/g, ''));
            const newTotal = currentTotal * (1 - discount / 100);
            document.getElementById('totalPrice').textContent = newTotal.toLocaleString('vi-VN') + '₫';
            document.getElementById('discountAmount').textContent = '-' + (currentTotal - newTotal).toLocaleString('vi-VN') + '₫';

        } else {
            promoResult.innerHTML = '<div class="text-danger"><i class="fas fa-times-circle me-1"></i>Mã giảm giá không hợp lệ</div>';
        }

        this.innerHTML = 'Áp dụng';
        this.disabled = false;
    }, 1000);
});