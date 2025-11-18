// Review page functionality

$(document).ready(function() {
    // Star rating functionality
    let currentRating = 0;
    
    $('.star-rating i').on('click', function() {
        currentRating = parseInt($(this).data('rating'));
        updateStarDisplay(currentRating);
        $('#rating').val(currentRating);
    });
    
    $('.star-rating i').on('mouseenter', function() {
        const hoverRating = parseInt($(this).data('rating'));
        updateStarDisplay(hoverRating);
    });
    
    $('.star-rating').on('mouseleave', function() {
        updateStarDisplay(currentRating);
    });
    
    function updateStarDisplay(rating) {
        $('.star-rating i').each(function() {
            const starRating = parseInt($(this).data('rating'));
            if (starRating <= rating) {
                $(this).removeClass('far').addClass('fas active');
            } else {
                $(this).removeClass('fas active').addClass('far');
            }
        });
    }
    
    // Form submission
    $('#reviewForm').on('submit', function(e) {
        e.preventDefault();
        
        // Clear previous validation messages
        $('.is-invalid').removeClass('is-invalid');
        $('.invalid-feedback').remove();
        
        let isValid = true;
        const rating = parseInt($('#rating').val());
        const content = $('#content').val().trim();
        const userName = $('#userName').val().trim();
        const userEmail = $('#userEmail').val().trim();
        
        // Validate rating
        if (rating === 0) {
            $.toast({
                heading: 'Lỗi',
                text: 'Vui lòng chọn số sao đánh giá',
                showHideTransition: 'slide',
                icon: 'error',
                position: 'top-right',
                hideAfter: 3000
            });
            isValid = false;
        }
        
        // Validate content
        if (!content) {
            $('#content').addClass('is-invalid').after('<div class="invalid-feedback">Vui lòng nhập nội dung đánh giá</div>');
            isValid = false;
        } else if (content.length > 1000) {
            $('#content').addClass('is-invalid').after('<div class="invalid-feedback">Nội dung không được vượt quá 1000 ký tự</div>');
            isValid = false;
        }
        
        // Validate user info (if not authenticated)
        if (!isAuth) {
            if (!userName) {
                $('#userName').addClass('is-invalid').after('<div class="invalid-feedback">Vui lòng nhập họ tên</div>');
                isValid = false;
            } else if (userName.length > 100) {
                $('#userName').addClass('is-invalid').after('<div class="invalid-feedback">Tên không được vượt quá 100 ký tự</div>');
                isValid = false;
            }
            
            if (!userEmail) {
                $('#userEmail').addClass('is-invalid').after('<div class="invalid-feedback">Vui lòng nhập email</div>');
                isValid = false;
            } else if (!isValidEmail(userEmail)) {
                $('#userEmail').addClass('is-invalid').after('<div class="invalid-feedback">Email không đúng định dạng</div>');
                isValid = false;
            } else if (userEmail.length > 100) {
                $('#userEmail').addClass('is-invalid').after('<div class="invalid-feedback">Email không được vượt quá 100 ký tự</div>');
                isValid = false;
            }
        }
        
        if (!isValid) {
            return;
        }
        
        const formData = {
            Rating: rating,
            Content: content,
            UserName: userName,
            UserEmail: userEmail,
            ProductId: null // Đánh giá chung
        };
        
        // Disable submit button and show loading
        const submitBtn = $(this).find('button[type="submit"]');
        const originalText = submitBtn.html();
        submitBtn.prop('disabled', true).addClass('btn-loading').html('<i class="fas fa-spinner fa-spin me-2"></i>Đang gửi...');
        
        $.ajax({
            url: '/reviews/submit',
            type: 'POST',
            headers: {
                'RequestVerificationToken': $('meta[name="X-CSRF-TOKEN"]').attr('content')
            },
            data: formData,
            success: function(response) {
                if (response.success) {
                    $.toast({
                        heading: 'Thành công',
                        text: response.message,
                        showHideTransition: 'slide',
                        icon: 'success',
                        position: 'top-right',
                        hideAfter: 5000
                    });
                    
                    // Reset form
                    $('#reviewForm')[0].reset();
                    currentRating = 0;
                    updateStarDisplay(0);
                    $('#rating').val(0);
                    
                    // Reload page after 2 seconds to show new review
                    setTimeout(function() {
                        location.reload();
                    }, 2000);
                } else {
                    $.toast({
                        heading: 'Lỗi',
                        text: response.message,
                        showHideTransition: 'slide',
                        icon: 'error',
                        position: 'top-right',
                        hideAfter: 5000
                    });
                }
            },
            error: function(xhr, status, error) {
                let errorMessage = 'Có lỗi xảy ra khi gửi đánh giá. Vui lòng thử lại sau.';
                
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại thông tin.';
                }
                
                $.toast({
                    heading: 'Lỗi',
                    text: errorMessage,
                    showHideTransition: 'slide',
                    icon: 'error',
                    position: 'top-right',
                    hideAfter: 5000
                });
            },
            complete: function() {
                // Re-enable submit button
                submitBtn.prop('disabled', false).removeClass('btn-loading').html(originalText);
            }
        });
    });
    
    // Load more reviews functionality (if needed in future)
    let currentPage = 1;
    const pageSize = 10;
    
    function loadMoreReviews() {
        // Implementation for pagination if needed
    }
    
    // Smooth scroll to review form
    $('.scroll-to-review').on('click', function(e) {
        e.preventDefault();
        $('html, body').animate({
            scrollTop: $('.review-form-card').offset().top - 100
        }, 800);
    });
});

// Utility function to format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    });
}

// Function to generate star HTML
function generateStars(rating, maxStars = 5) {
    let stars = '';
    for (let i = 1; i <= maxStars; i++) {
        if (i <= rating) {
            stars += '<i class="fas fa-star text-warning"></i>';
        } else {
            stars += '<i class="far fa-star text-muted-light"></i>';
        }
    }
    return stars;
}

// Function to validate email format
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}