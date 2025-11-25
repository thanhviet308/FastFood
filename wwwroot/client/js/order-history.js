// Order History JavaScript
$(document).ready(function() {
    let orders = [];
    let filteredOrders = [];

    // Initialize after ensuring DOM is ready
    setTimeout(function() {
        loadOrders();
        setupEventListeners();
    }, 100); // Small delay to ensure DOM is fully ready

    function setupEventListeners() {
        // Status filter
        $('#statusFilter').change(function() {
            filterOrders();
        });

        // Sort filter
        $('#sortFilter').change(function() {
            sortOrders();
        });

        // Search functionality
        $('#searchBtn').click(function() {
            searchOrders();
        });

        $('#searchInput').keypress(function(e) {
            if (e.which === 13) {
                searchOrders();
            }
        });

        // View details buttons
        $(document).on('click', '.view-details-btn', function() {
            const orderId = $(this).data('order-id');
            showOrderDetails(orderId);
        });

        // Cancel order buttons
        $(document).on('click', '.cancel-order-btn', function() {
            const orderId = $(this).data('order-id');
            cancelOrder(orderId);
        });
    }

    function loadOrders() {
        // Orders are already loaded server-side, just initialize
        const orderCards = $('.order-card');
        
        if (orderCards.length === 0) {
            console.log('No order cards found');
            return;
        }
        
        orders = orderCards.map(function() {
            return {
                element: $(this),
                status: $(this).data('status'), // trạng thái đơn (PENDING, DELIVERED,...)
                paymentStatus: $(this).data('payment-status'), // trạng thái thanh toán (UNPAID, PAID)
                orderNumber: $(this).data('order-number')
            };
        }).get();

        filteredOrders = [...orders];
        console.log(`Loaded ${orders.length} orders`);
    }

    function filterOrders() {
        const selectedStatus = $('#statusFilter').val();
        
        if (selectedStatus === '') {
            filteredOrders = [...orders];
        } else {
            // Lọc theo trạng thái thanh toán
            filteredOrders = orders.filter(order => {
                switch (selectedStatus) {
                    case 'unpaid': // Chưa thanh toán
                        return order.paymentStatus === 'UNPAID';
                    case 'paid': // Thanh toán thành công
                        return order.paymentStatus === 'PAID';
                    case 'cancelled': // Đã hủy
                        return order.status === 'CANCELLED';
                    default:
                        return false;
                }
            });
        }
        
        displayFilteredOrders();
    }

    function sortOrders() {
        const sortOrder = $('#sortFilter').val();
        
        filteredOrders.sort((a, b) => {
            const dateA = new Date(a.element.find('.order-date').text());
            const dateB = new Date(b.element.find('.order-date').text());
            
            return sortOrder === 'asc' ? dateA - dateB : dateB - dateA;
        });
        
        displayFilteredOrders();
    }

    function searchOrders() {
        const searchTerm = $('#searchInput').val().toLowerCase().trim();
        
        if (searchTerm === '') {
            filteredOrders = [...orders];
        } else {
            filteredOrders = orders.filter(order => 
                order.orderNumber.toLowerCase().includes(searchTerm)
            );
        }
        
        displayFilteredOrders();
    }

    function displayFilteredOrders() {
        // Hide all orders first
        orders.forEach(order => order.element.hide());
        
        // Show filtered orders
        filteredOrders.forEach(order => order.element.show());
        
        // Update empty state
        updateEmptyState();
    }

    function updateEmptyState() {
        const visibleOrders = filteredOrders.filter(order => order.element.is(':visible'));
        
        if (visibleOrders.length === 0) {
            if ($('.empty-search').length === 0) {
                $('.orders-list').append(`
                    <div class="empty-search text-center py-5">
                        <i class="fas fa-search fa-3x text-muted mb-3"></i>
                        <h4 class="text-muted mb-2">Không tìm thấy đơn hàng</h4>
                        <p class="text-muted mb-4">Thử điều chỉnh bộ lọc hoặc tìm kiếm khác</p>
                        <button class="btn btn-outline-primary" onclick="clearFilters()">
                            <i class="fas fa-times me-2"></i> Xóa bộ lọc
                        </button>
                    </div>
                `);
            }
            $('.empty-orders').hide();
        } else {
            $('.empty-search').remove();
            $('.empty-orders').show();
        }
    }

    function showOrderDetails(orderId) {
        const modal = $('#orderDetailsModal');
        const content = $('#orderDetailsContent');
        
        // Show loading
        content.html(`
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-3 text-muted">Đang tải chi tiết đơn hàng...</p>
            </div>
        `);
        
        modal.modal('show');

        $.ajax({
            url: `/orders/details/${orderId}`,
            method: 'GET',
            success: function(response) {
                if (response.success) {
                    displayOrderDetails(response.data);
                } else {
                    content.html(`
                        <div class="text-center py-5">
                            <i class="fas fa-exclamation-triangle fa-3x text-warning mb-3"></i>
                            <h4 class="text-warning">Không thể tải chi tiết</h4>
                            <p class="text-muted">${response.message}</p>
                        </div>
                    `);
                }
            },
            error: function() {
                content.html(`
                    <div class="text-center py-5">
                        <i class="fas fa-exclamation-circle fa-3x text-danger mb-3"></i>
                        <h4 class="text-danger">Lỗi kết nối</h4>
                        <p class="text-muted">Không thể kết nối đến máy chủ</p>
                    </div>
                `);
            }
        });
    }

    function displayOrderDetails(data) {
        const content = $('#orderDetailsContent');
        
        let detailsHtml = `
            <div class="order-details-header">
                <div class="row">
                    <div class="col-md-6">
                        <h6><strong>Mã đơn hàng:</strong> #${data.orderNumber}</h6>
                        <p><strong>Ngày đặt:</strong> ${data.createdAt}</p>
                        <p><strong>Trạng thái:</strong> <span class="status-badge status-${data.status.toLowerCase()}">${getStatusText(data.status)}</span></p>
                    </div>
                    <div class="col-md-6">
                        <h6><strong>Thông tin nhận hàng:</strong></h6>
                        <p><strong>Người nhận:</strong> ${data.receiverName}</p>
                        <p><strong>SĐT:</strong> ${data.receiverPhone}</p>
                        <p><strong>Địa chỉ:</strong> ${data.receiverAddress}</p>
                        ${data.note ? `<p><strong>Ghi chú:</strong> ${data.note}</p>` : ''}
                    </div>
                </div>
            </div>
            
            <h6 class="mb-3"><strong>Chi tiết sản phẩm:</strong></h6>
        `;

        data.orderDetails.forEach(item => {
            detailsHtml += `
                <div class="order-details-item">
                    <div class="item-info">
                        <div class="item-name">${item.productName}</div>
                        <div class="item-quantity">Số lượng: ${item.quantity}</div>
                    </div>
                    <div class="item-price">
                        <div class="item-unit-price">${formatCurrency(item.price)}</div>
                        <div class="item-total-price">${formatCurrency(item.subtotal)}</div>
                    </div>
                </div>
            `;
        });

        detailsHtml += `
            <div class="order-details-summary">
                <div class="summary-row">
                    <span>Tổng tiền hàng:</span>
                    <span>${formatCurrency(data.totalAmount)}</span>
                </div>
                <div class="summary-row">
                    <span>Phí vận chuyển:</span>
                    <span>${formatCurrency(0)}</span>
                </div>
                <div class="summary-row">
                    <span>Tổng cộng:</span>
                    <span>${formatCurrency(data.totalAmount)}</span>
                </div>
            </div>
        `;

        content.html(detailsHtml);
    }

    function cancelOrder(orderId) {
        if (confirm('Bạn có chắc chắn muốn hủy đơn hàng này?')) {
            $.ajax({
                url: `/orders/cancel/${orderId}`,
                method: 'POST',
                headers: {
                    'RequestVerificationToken': $('meta[name="X-CSRF-TOKEN"]').attr('content')
                },
                success: function(response) {
                    if (response.success) {
                        showToast('success', 'Thành công', response.message);
                        setTimeout(() => location.reload(), 1500);
                    } else {
                        showToast('error', 'Lỗi', response.message);
                    }
                },
                error: function() {
                    showToast('error', 'Lỗi', 'Không thể hủy đơn hàng');
                }
            });
        }
    }

    function formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    }

    function getStatusText(status) {
        const statusMap = {
            'Pending': 'Chờ xác nhận',
            'Confirmed': 'Đã xác nhận',
            'Shipping': 'Đang giao',
            'Delivered': 'Đã giao',
            'Cancelled': 'Đã hủy'
        };
        return statusMap[status] || status;
    }

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

    // Global function for clear filters button
    window.clearFilters = function() {
        $('#statusFilter').val('');
        $('#sortFilter').val('desc');
        $('#searchInput').val('');
        filterOrders();
    };

    // Loading animation for page
    $(window).on('load', function() {
        $('#spinner').fadeOut('slow', function() {
            $(this).remove();
        });
    });

    // Add some interactive animations
    $('.order-card').hover(
        function() {
            $(this).css('transform', 'translateY(-2px)');
        },
        function() {
            $(this).css('transform', 'translateY(0)');
        }
    );
});