(function ($) {
    "use strict";

    // Spinner
    var spinner = function () {
        setTimeout(function () {
            if ($('#spinner').length > 0) {
                $('#spinner').removeClass('show');
            }
        }, 1);
    };
    spinner();

    // Fixed Navbar
    $(window).scroll(function () {
        if ($(window).width() < 992) {
            if ($(this).scrollTop() > 55) {
                $('.fixed-top').addClass('shadow');
            } else {
                $('.fixed-top').removeClass('shadow');
            }
        } else {
            if ($(this).scrollTop() > 55) {
                $('.fixed-top').addClass('shadow').css('top', 0);
            } else {
                $('.fixed-top').removeClass('shadow').css('top', 0);
            }
        }
    });

    $(window).on('scroll', function () {
        if ($(this).scrollTop() > 300) {
            $('.back-to-top').css('display', 'flex');
        } else {
            $('.back-to-top').css('display', 'none');
        }
    });
    function ffScrollTop() {
        var start = window.pageYOffset || document.documentElement.scrollTop;
        if (start <= 10) { window.scrollTo(0, 0); return; }
        var total = 180;
        var startTime = performance.now();
        function ease(t) { return 1 - Math.pow(1 - t, 3); }
        function step(now) {
            var progress = Math.min((now - startTime) / total, 1);
            var eased = ease(progress);
            var y = Math.round(start * (1 - eased));
            window.scrollTo(0, y);
            if (progress < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }
    $(document).on('click', '.back-to-top', function (e) {
        e.preventDefault();
        ffScrollTop();
        return false;
    });

    // Testimonial carousel
    $(".testimonial-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 2000,
        dots: true,
        loop: true,
        margin: 25,
        nav: true,
        navText: ['<i class="bi bi-arrow-left"></i>', '<i class="bi bi-arrow-right"></i>'],
        responsive: { 0: { items: 1 }, 768: { items: 2 }, 992: { items: 2 } }
    });

    // Vegetable carousel
    $(".vegetable-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 1500,
        dots: true,
        loop: true,
        margin: 25,
        nav: true,
        navText: ['<i class="bi bi-arrow-left"></i>', '<i class="bi bi-arrow-right"></i>'],
        responsive: { 0: { items: 1 }, 768: { items: 2 }, 992: { items: 3 }, 1200: { items: 4 } }
    });

    // Modal Video
    $(document).ready(function () {
        let videoSrc;
        $('.btn-play').click(function () {
            videoSrc = $(this).data("src");
        });

        $('#videoModal').on('shown.bs.modal', function () {
            $("#video").attr('src', videoSrc + "?autoplay=1");
        }).on('hide.bs.modal', function () {
            $("#video").attr('src', '');
        });

        // Add active class to header link
        const currentUrl = window.location.pathname;
        $('#navbarCollapse .nav-link').each(function () {
            if ($(this).attr('href') === currentUrl) {
                $(this).addClass('active');
            }
        });
    });

    // Product Quantity
    $('.quantity button').on('click', function () {
        const button = $(this);
        const input = button.closest('.quantity').find('input');
        let oldValue = parseFloat(input.val());
        const price = parseFloat(input.attr('data-cart-detail-price'));
        const id = input.attr('data-cart-detail-id');
        let newVal = button.hasClass('btn-plus') ? oldValue + 1 : Math.max(1, oldValue - 1);

        input.val(newVal);
        $(`p[data-cart-detail-id='${id}']`).text(formatCurrency((price * newVal).toFixed(2)) + " đ");

        // Update total cart price
        const totalPriceElement = $(`p[data-cart-total-price]`);
        const currentTotal = parseFloat(totalPriceElement.attr("data-cart-total-price"));
        const difference = (newVal - oldValue) * price;
        const newTotal = currentTotal + difference;

        totalPriceElement.text(formatCurrency(newTotal.toFixed(2)) + " đ").attr("data-cart-total-price", newTotal);
    });

    // Format currency
    function formatCurrency(value) {
        return new Intl.NumberFormat('vi-VN', { style: 'decimal' }).format(value).replace(/\./g, ',');
    }

    // Handle product filter
    $('#btnFilter').click(function (event) {
        event.preventDefault();
        let factoryArr = [], targetArr = [], priceArr = [];
        $("#factoryFilter .form-check-input:checked").each(function () {
            factoryArr.push($(this).val());
        });
        $("#targetFilter .form-check-input:checked").each(function () {
            targetArr.push($(this).val());
        });
        $("#priceFilter .form-check-input:checked").each(function () {
            priceArr.push($(this).val());
        });

        let sortValue = $('input[name="radio-sort"]:checked').val();
        const currentUrl = new URL(window.location.href);
        const searchParams = currentUrl.searchParams;
        searchParams.set('page', '1');
        searchParams.set('sort', sortValue);

        searchParams.delete('factory');
        searchParams.delete('target');
        searchParams.delete('price');
        if (factoryArr.length > 0) searchParams.set('factory', factoryArr.join(','));
        if (targetArr.length > 0) searchParams.set('target', targetArr.join(','));
        if (priceArr.length > 0) searchParams.set('price', priceArr.join(','));

        window.location.href = currentUrl.toString();
    });

    // Auto-check filter options
    const params = new URLSearchParams(window.location.search);
    ['factory', 'target', 'price'].forEach(param => {
        if (params.has(param)) {
            params.get(param).split(',').forEach(value => {
                $(`#${param}Filter .form-check-input[value="${value}"]`).prop('checked', true);
            });
        }
    });
    if (params.has('sort')) {
        $(`input[name="radio-sort"][value="${params.get('sort')}"]`).prop('checked', true);
    }

    // Add to cart (Ajax) - Only for detail page, homepage uses variant selection modal
    $(document).on('click', '.btnAddToCartDetail', function (event) {
        event.preventDefault();

        if (typeof isLogin === 'function' && !isLogin()) {
            showErrorToast('Bạn cần đăng nhập tài khoản');
            return;
        }

        const $btn = $(this).prop('disabled', true);

        const productId = $btn.data('product-id');
        const quantity = Math.max(1, parseInt($("#cartDetails0\\.quantity").val() || "1", 10));

        if (!productId) {
            showErrorToast('Không xác định được sản phẩm.');
            $btn.prop('disabled', false);
            return;
        }

        // Lấy CSRF token (nếu dùng anti-forgery)
        const csrf = $('meta[name="X-CSRF-TOKEN"]').attr('content');

        $.ajax({
            url: '/api/cart/add',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ productId, quantity }),
            headers: csrf ? { 'RequestVerificationToken': csrf } : {},
            success: function (distinctCount) {
                if (Number.isFinite(distinctCount)) {
                    var $b = $('#sumCart');
                    if ($b.length) {
                        $b.text(distinctCount);
                        $b.css('display', distinctCount > 0 ? 'inline-block' : 'none');
                    }
                }
                showSuccessToast('Thêm sản phẩm vào giỏ hàng thành công');
            },
            error: function (xhr) {
                console.error(xhr.status, xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    showErrorToast('Bạn cần đăng nhập để thực hiện thao tác này.');
                } else {
                    showErrorToast('Có lỗi xảy ra, vui lòng thử lại.');
                }
            },
            complete: function () {
                $btn.prop('disabled', false);
            }
        });
    });
    $(function () {
        function showCartBadge(n) {
            var $b = $('#sumCart');
            if (!$b.length) return;
            if (Number.isFinite(n) && n > 0) {
                $b.text(n).css('display', 'inline-block');
            } else {
                $b.text('0').css('display', 'none');
            }
        }
        $.get('/cart/count')
            .done(function (data) {
                var n = Number(data && typeof data === 'object' ? data.count : data);
                showCartBadge(Number.isFinite(n) ? n : 0);
            })
            .fail(function () {
                showCartBadge(0);
            });
    });





    function isLogin() {
        return $("#navbarCollapse .a-login").length === 0;
    }

    function showErrorToast(message) {
        $.toast({ heading: 'Lỗi thao tác', text: message, position: 'top-right', icon: 'error' });
    }

    function showSuccessToast(message) {
        $.toast({ heading: 'Giỏ hàng', text: message, position: 'top-right', icon: 'success' });
    }

})(jQuery);
