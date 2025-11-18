// Modern Account Management JavaScript
$(document).ready(function() {
    // Edit button functionality
    $('#editBtn').click(function() {
        toggleEditMode(true);
    });

    // Cancel button functionality
    $('#cancelBtn').click(function() {
        toggleEditMode(false);
        // Reset form to original values
        $('#accountForm')[0].reset();
        // Don't reload page, just reset form
    });

    // Toggle edit mode
    function toggleEditMode(isEditing) {
        const form = $('#accountForm');
        const inputs = form.find('input, textarea');
        const formActions = $('#formActions');
        const editBtn = $('#editBtn');

        if (isEditing) {
            inputs.prop('readonly', false);
            inputs.removeClass('form-control-modern').addClass('form-control-modern');
            formActions.removeClass('d-none');
            editBtn.addClass('d-none');
            inputs.first().focus();
            
            // Add editing animation
            inputs.each(function(index) {
                $(this).css('animation', 'fadeInUp 0.3s ease-out');
                $(this).css('animation-delay', (index * 0.1) + 's');
            });
        } else {
            inputs.prop('readonly', true);
            formActions.addClass('d-none');
            editBtn.removeClass('d-none');
        }
    }

    // Account form submission
    $('#accountForm').submit(function(e) {
        e.preventDefault();
        
        const formData = new FormData(this);
        const submitBtn = $(this).find('button[type="submit"]');
        const originalText = submitBtn.html();
        
        // Show loading state
        submitBtn.html('<i class="fas fa-spinner fa-spin me-2"></i>Đang lưu...').prop('disabled', true);

        $.ajax({
            url: '/account/update',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': $('meta[name="X-CSRF-TOKEN"]').attr('content')
            },
            success: function(response) {
                if (response.success) {
                    showToast('success', 'Thành công', response.message);
                    setTimeout(function() {
                        location.reload();
                    }, 1500);
                } else {
                    showToast('error', 'Lỗi', response.message);
                    submitBtn.html(originalText).prop('disabled', false);
                }
            },
            error: function(xhr) {
                let message = 'Có lỗi xảy ra khi cập nhật thông tin';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                }
                showToast('error', 'Lỗi', message);
                submitBtn.html(originalText).prop('disabled', false);
            }
        });
    });

    // Password form submission
    $('#passwordForm').submit(function(e) {
        e.preventDefault();
        
        const currentPassword = $('input[name="currentPassword"]').val();
        const newPassword = $('input[name="newPassword"]').val();
        const confirmPassword = $('input[name="confirmPassword"]').val();
        const submitBtn = $(this).find('button[type="submit"]');
        const originalText = submitBtn.html();

        // Validation
        if (!currentPassword || !newPassword || !confirmPassword) {
            showToast('warning', 'Cảnh báo', 'Vui lòng nhập đầy đủ thông tin');
            return;
        }

        if (newPassword.length < 6) {
            showToast('warning', 'Cảnh báo', 'Mật khẩu mới phải có ít nhất 6 ký tự');
            return;
        }

        if (newPassword !== confirmPassword) {
            showToast('warning', 'Cảnh báo', 'Mật khẩu mới và xác nhận không khớp');
            return;
        }

        if (newPassword === currentPassword) {
            showToast('warning', 'Cảnh báo', 'Mật khẩu mới phải khác mật khẩu hiện tại');
            return;
        }

        // Show loading state
        submitBtn.html('<i class="fas fa-spinner fa-spin me-2"></i>Đang đổi mật khẩu...').prop('disabled', true);

        const formData = new FormData();
        formData.append('currentPassword', currentPassword);
        formData.append('newPassword', newPassword);
        formData.append('confirmPassword', confirmPassword);

        $.ajax({
            url: '/account/change-password',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': $('meta[name="X-CSRF-TOKEN"]').attr('content')
            },
            success: function(response) {
                if (response.success) {
                    showToast('success', 'Thành công', response.message);
                    $('#passwordForm')[0].reset();
                } else {
                    showToast('error', 'Lỗi', response.message);
                }
                submitBtn.html(originalText).prop('disabled', false);
            },
            error: function(xhr) {
                let message = 'Có lỗi xảy ra khi đổi mật khẩu';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                }
                showToast('error', 'Lỗi', message);
                submitBtn.html(originalText).prop('disabled', false);
            }
        });
    });

    // Form validation
    $('input, textarea').on('input', function() {
        const $this = $(this);
        const value = $this.val().trim();
        
        // Remove previous validation classes
        $this.removeClass('is-valid is-invalid');
        
        // Simple validation based on field type
        if ($this.attr('type') === 'email') {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (emailRegex.test(value)) {
                $this.addClass('is-valid');
            } else if (value.length > 0) {
                $this.addClass('is-invalid');
            }
        } else if ($this.attr('name') === 'Phone') {
            const phoneRegex = /^[0-9\s\-\+]{10,}$/;
            if (phoneRegex.test(value)) {
                $this.addClass('is-valid');
            } else if (value.length > 0) {
                $this.addClass('is-invalid');
            }
        } else if (value.length > 0) {
            $this.addClass('is-valid');
        }
    });

    // Toast notification function
    function showToast(type, title, message) {
        $.toast({
            heading: title,
            text: message,
            showHideTransition: 'slide',
            icon: type,
            position: 'top-right',
            hideAfter: 3000,
            stack: 5
        });
    }

    // Enhanced form interactions
    $('.form-control').focus(function() {
        $(this).parent().find('.form-label').addClass('text-primary fw-bold');
    }).blur(function() {
        $(this).parent().find('.form-label').removeClass('text-primary fw-bold');
    });

    // Password strength indicator
    $('input[name="newPassword"]').on('input', function() {
        const password = $(this).val();
        const strength = calculatePasswordStrength(password);
        updatePasswordStrength(strength);
    });

    function calculatePasswordStrength(password) {
        let strength = 0;
        if (password.length >= 8) strength++;
        if (password.match(/[a-z]/)) strength++;
        if (password.match(/[A-Z]/)) strength++;
        if (password.match(/[0-9]/)) strength++;
        if (password.match(/[^a-zA-Z0-9]/)) strength++;
        return strength;
    }

    function updatePasswordStrength(strength) {
        const strengthText = ['Rất yếu', 'Yếu', 'Trung bình', 'Mạnh', 'Rất mạnh'];
        const strengthColors = ['#dc3545', '#fd7e14', '#ffc107', '#28a745', '#20c997'];
        
        if (strength > 0) {
            let indicator = $('#passwordStrength');
            if (indicator.length === 0) {
                $('input[name="newPassword"]').after('<div id="passwordStrength" class="password-strength"></div>');
                indicator = $('#passwordStrength');
            }
            
            indicator.html(`
                <div class="d-flex align-items-center gap-2">
                    <div class="strength-bar" style="background-color: ${strengthColors[strength-1]}"></div>
                    <small class="text-muted">${strengthText[strength-1]}</small>
                </div>
            `);
        }
    }

    // Avatar upload functionality
    $('#uploadAvatarBtn').click(function() {
        $('#avatarInput').click();
    });

    $('#avatarInput').change(function(e) {
        const file = e.target.files[0];
        if (file) {
            // Validate file type
            const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
            if (!validTypes.includes(file.type)) {
                showToast('error', 'Lỗi', 'Vui lòng chọn file ảnh (JPEG, PNG, GIF)');
                return;
            }

            // Validate file size (max 5MB)
            if (file.size > 5 * 1024 * 1024) {
                showToast('error', 'Lỗi', 'Kích thước ảnh không được vượt quá 5MB');
                return;
            }

            uploadAvatar(file);
        }
    });

    function uploadAvatar(file) {
        const formData = new FormData();
        formData.append('avatar', file);
        const uploadBtn = $('#uploadAvatarBtn');
        const originalText = uploadBtn.html();

        // Show loading state
        uploadBtn.html('<i class="fas fa-spinner fa-spin me-2"></i>Đang upload...').prop('disabled', true);

        $.ajax({
            url: '/account/upload-avatar',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': $('meta[name="X-CSRF-TOKEN"]').attr('content')
            },
            success: function(response) {
                if (response.success) {
                    showToast('success', 'Thành công', response.message);
                    // Update avatar image
                    if (response.avatarUrl) {
                        $('.avatar-modern').html(`<img src="${response.avatarUrl}" alt="Avatar" class="avatar-img" />`);
                    }
                } else {
                    showToast('error', 'Lỗi', response.message);
                }
                uploadBtn.html(originalText).prop('disabled', false);
            },
            error: function(xhr) {
                let message = 'Có lỗi xảy ra khi upload ảnh';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                }
                showToast('error', 'Lỗi', message);
                uploadBtn.html(originalText).prop('disabled', false);
            }
        });
    }
});