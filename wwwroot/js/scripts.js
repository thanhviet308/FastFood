window.addEventListener('DOMContentLoaded', event => {
    // Toggle the side navigation
    const sidebarToggle = document.body.querySelector('#sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', event => {
            event.preventDefault();
            document.body.classList.toggle('sb-sidenav-toggled');
            localStorage.setItem('sb|sidebar-toggle', document.body.classList.contains('sb-sidenav-toggled'));
        });
    }

    // Toggle the sidebar on load if previously toggled
    if (localStorage.getItem('sb|sidebar-toggle') === 'true') {
        document.body.classList.toggle('sb-sidenav-toggled');
    }

    // Initialize Bootstrap dropdowns
    const dropdownToggle = document.querySelector('#navbarDropdown');
    if (dropdownToggle) {
        // Manual dropdown implementation for better compatibility
        dropdownToggle.addEventListener('click', function(e) {
            e.preventDefault();
            
            const dropdownMenu = this.nextElementSibling;
            if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
                // Toggle the show class
                const isShown = dropdownMenu.classList.contains('show');
                
                // Close all other dropdowns first
                document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                    menu.classList.remove('show');
                });
                
                if (!isShown) {
                    dropdownMenu.classList.add('show');
                    
                    // Position the dropdown
                    const rect = this.getBoundingClientRect();
                    dropdownMenu.style.position = 'absolute';
                    dropdownMenu.style.top = rect.bottom + 'px';
                    dropdownMenu.style.right = '0px';
                    dropdownMenu.style.left = 'auto';
                    dropdownMenu.style.zIndex = '1000';
                }
            }
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!e.target.closest('.dropdown')) {
                document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                    menu.classList.remove('show');
                });
            }
        });
    }
});