// FastFood Theme JavaScript
(function() {
    'use strict';

    // DOM Elements
    const header = document.querySelector('.ff-header');
    const mobileMenuToggle = document.querySelector('.ff-mobile-menu-toggle');
    const mobileMenu = document.querySelector('.ff-mobile-menu');
    const cartButtons = document.querySelectorAll('.ff-add-to-cart');
    const notification = document.getElementById('ff-notification');
    const productCards = document.querySelectorAll('.ff-product-card');
    const variantCards = document.querySelectorAll('.ff-variant-card');

    // Initialize theme
    document.addEventListener('DOMContentLoaded', function() {
        initializeAnimations();
        initializeInteractions();
        initializeScrollEffects();
    });

    // Animation initialization
    function initializeAnimations() {
        // Animate elements on scroll
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver(function(entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('ff-animate');
                }
            });
        }, observerOptions);

        // Observe product cards
        productCards.forEach(card => {
            observer.observe(card);
        });

        // Observe hero elements
        const heroElements = document.querySelectorAll('.ff-hero-content, .ff-category-card');
        heroElements.forEach(element => {
            observer.observe(element);
        });
    }

    // Interaction initialization
    function initializeInteractions() {
        // Mobile menu toggle
        if (mobileMenuToggle && mobileMenu) {
            mobileMenuToggle.addEventListener('click', function(e) {
                e.preventDefault();
                mobileMenu.classList.toggle('active');
                this.classList.toggle('active');
            });
        }

        // Add to cart animation
        cartButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                const productCard = this.closest('.ff-product-card');
                const productImage = productCard.querySelector('.ff-product-image img');
                
                if (productImage) {
                    flyToCart(productImage);
                }
                
                // Show notification
                showNotification('Đã thêm sản phẩm vào giỏ hàng!', 'success');
                
                // Button animation
                this.classList.add('added');
                setTimeout(() => {
                    this.classList.remove('added');
                }, 1000);
            });
        });

        // Product card hover effects
        productCards.forEach(card => {
            card.addEventListener('mouseenter', function() {
                this.classList.add('hovered');
            });

            card.addEventListener('mouseleave', function() {
                this.classList.remove('hovered');
            });
        });

        // Variant card selection
        const variantOptions = document.querySelectorAll('.variant-option');
        variantOptions.forEach(option => {
            option.addEventListener('click', function() {
                // Remove active class from all variants
                variantOptions.forEach(v => v.classList.remove('active'));
                // Add active class to clicked variant
                this.classList.add('active');
                
                // Update price if data-price attribute exists
                const price = this.dataset.price;
                if (price) {
                    updateProductPrice(price);
                }
                
                // Check the radio button
                const radioInput = this.querySelector('input[type="radio"]');
                if (radioInput) {
                    radioInput.checked = true;
                }
            });
        });

        // Quantity controls
        const quantityControls = document.querySelectorAll('.ff-quantity-control');
        quantityControls.forEach(control => {
            const minusBtn = control.querySelector('.ff-minus');
            const plusBtn = control.querySelector('.ff-plus');
            const input = control.querySelector('input[type="number"]');

            if (minusBtn && plusBtn && input) {
                minusBtn.addEventListener('click', function() {
                    const currentValue = parseInt(input.value) || 1;
                    if (currentValue > 1) {
                        input.value = currentValue - 1;
                        triggerInputChange(input);
                    }
                });

                plusBtn.addEventListener('click', function() {
                    const currentValue = parseInt(input.value) || 1;
                    input.value = currentValue + 1;
                    triggerInputChange(input);
                });
            }
        });

        // Newsletter form
        const newsletterForm = document.querySelector('.ff-newsletter-form');
        if (newsletterForm) {
            newsletterForm.addEventListener('submit', function(e) {
                e.preventDefault();
                const email = this.querySelector('input[type="email"]').value;
                if (email && isValidEmail(email)) {
                    showNotification('Cảm ơn bạn đã đăng ký nhận tin!', 'success');
                    this.reset();
                } else {
                    showNotification('Vui lòng nhập email hợp lệ!', 'error');
                }
            });
        }

        // Filter buttons
        const filterButtons = document.querySelectorAll('.ff-filter-btn');
        filterButtons.forEach(button => {
            button.addEventListener('click', function() {
                const filter = this.dataset.filter;
                
                // Update active state
                filterButtons.forEach(btn => btn.classList.remove('active'));
                this.classList.add('active');
                
                // Filter products
                filterProducts(filter);
            });
        });
    }

    // Scroll effects
    function initializeScrollEffects() {
        let lastScrollTop = 0;
        
        window.addEventListener('scroll', throttle(function() {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            
            // Header scroll effect
            if (header) {
                if (scrollTop > 100) {
                    header.classList.add('scrolled');
                } else {
                    header.classList.remove('scrolled');
                }
            }
            
            // Parallax effect for hero banner
            const heroBanner = document.querySelector('.ff-hero-banner');
            if (heroBanner) {
                const speed = 0.5;
                const yPos = -(scrollTop * speed);
                heroBanner.style.transform = `translateY(${yPos}px)`;
            }
            
            lastScrollTop = scrollTop;
        }, 10));
    }

    // Utility functions
    function flyToCart(imageElement) {
        const cartIcon = document.querySelector('.ff-cart-icon');
        if (!cartIcon) return;

        const imageRect = imageElement.getBoundingClientRect();
        const cartRect = cartIcon.getBoundingClientRect();

        const flyingImage = imageElement.cloneNode(true);
        flyingImage.style.position = 'fixed';
        flyingImage.style.left = imageRect.left + 'px';
        flyingImage.style.top = imageRect.top + 'px';
        flyingImage.style.width = imageRect.width + 'px';
        flyingImage.style.height = imageRect.height + 'px';
        flyingImage.style.zIndex = '9999';
        flyingImage.style.transition = 'all 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
        flyingImage.style.pointerEvents = 'none';

        document.body.appendChild(flyingImage);

        setTimeout(() => {
            flyingImage.style.left = cartRect.left + cartRect.width / 2 + 'px';
            flyingImage.style.top = cartRect.top + cartRect.height / 2 + 'px';
            flyingImage.style.width = '20px';
            flyingImage.style.height = '20px';
            flyingImage.style.opacity = '0';
        }, 10);

        setTimeout(() => {
            document.body.removeChild(flyingImage);
            cartIcon.classList.add('bounce');
            setTimeout(() => {
                cartIcon.classList.remove('bounce');
            }, 500);
        }, 800);
    }

    function showNotification(message, type = 'info') {
        if (!notification) return;

        notification.textContent = message;
        notification.className = `ff-notification ff-notification-${type} show`;

        setTimeout(() => {
            notification.classList.remove('show');
        }, 3000);
    }

    function updateProductPrice(price) {
        const priceElement = document.getElementById('product-price-display');
        if (priceElement) {
            priceElement.textContent = parseInt(price).toLocaleString('vi-VN') + '₫';
            priceElement.classList.add('pulse');
            setTimeout(() => {
                priceElement.classList.remove('pulse');
            }, 500);
        }
    }

    function filterProducts(filter) {
        const products = document.querySelectorAll('.ff-product-card');
        
        products.forEach(product => {
            const category = product.dataset.category;
            
            if (filter === 'all' || category === filter) {
                product.style.display = 'block';
                product.classList.add('fadeInUp');
            } else {
                product.style.display = 'none';
            }
        });
    }

    function triggerInputChange(input) {
        const event = new Event('change', { bubbles: true });
        input.dispatchEvent(event);
    }

    function isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    // Throttle function for performance
    function throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        }
    }

    // Debounce function for search
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Search functionality
    const searchInput = document.querySelector('.ff-search-input');
    if (searchInput) {
        searchInput.addEventListener('input', debounce(function() {
            const searchTerm = this.value.toLowerCase();
            searchProducts(searchTerm);
        }, 300));
    }

    function searchProducts(searchTerm) {
        const products = document.querySelectorAll('.ff-product-card');
        
        products.forEach(product => {
            const productName = product.querySelector('.ff-product-name').textContent.toLowerCase();
            const productCategory = product.dataset.category ? product.dataset.category.toLowerCase() : '';
            
            if (productName.includes(searchTerm) || productCategory.includes(searchTerm)) {
                product.style.display = 'block';
                product.classList.add('fadeInUp');
            } else {
                product.style.display = 'none';
            }
        });
    }

    // Add CSS animations dynamically
    const style = document.createElement('style');
    style.textContent = `
        .ff-animate {
            animation: fadeInUp 0.6s ease-out;
        }
        
        .ff-notification {
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            border-radius: 8px;
            color: white;
            font-weight: 500;
            z-index: 10000;
            transform: translateX(400px);
            transition: transform 0.3s ease;
        }
        
        .ff-notification.show {
            transform: translateX(0);
        }
        
        .ff-notification-success {
            background: linear-gradient(135deg, #4CAF50, #45a049);
        }
        
        .ff-notification-error {
            background: linear-gradient(135deg, #f44336, #da190b);
        }
        
        .ff-notification-info {
            background: linear-gradient(135deg, #2196F3, #0b7dda);
        }
        
        @keyframes fadeInUp {
            from {
                opacity: 0;
                transform: translateY(30px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        
        @keyframes bounce {
            0%, 20%, 53%, 80%, 100% {
                transform: translateY(0);
            }
            40%, 43% {
                transform: translateY(-10px);
            }
            70% {
                transform: translateY(-5px);
            }
        }
        
        .bounce {
            animation: bounce 0.5s ease;
        }
        
        .pulse {
            animation: pulse 0.5s ease;
        }
    `;
    document.head.appendChild(style);

})();