(function (window, document, $) {
    $(function () {
        var sidebarNav = $(".sidebar-nav");
        if (sidebarNav.length > 0) {
            $('#sidebarNav').metisMenu();  // Assuming you're using the MetisMenu jQuery plugin
        }

        // Sidebar mobile toggle (if applicable)
        $('.js-sidebar-toggle').on('click', function () {
            // Toggle the 'collapsed' class on the sidebar
            $('.sidebar').toggleClass('collapsed');
        });

        // Prevent click propagation in mega-menu dropdown
        $(document).on('click', '.mega-menu.dropdown-menu', function (e) {
            e.stopPropagation();
        });

        // Toggle mini sidebar and navbar expansion
        $('.sidebar-toggle').on('click', function () {
            $('body').toggleClass('sidebar-mini');
            $('.app-navbar').toggleClass('expand');
        });

        // Hover effect on navbar (only when sidebar is mini)
        $('.app-navbar').hover(function () {
            if ($('body').hasClass('sidebar-mini')) {
                $('.navbar-header').toggleClass('expand');
            }
        });

        // Show search wrapper when search icon is clicked
        $('.search').on('click', function () {
            $('.search-wrapper').fadeIn(200);
        });

        // Hide search wrapper when close button is clicked
        $('.close-btn').on('click', function () {
            $('.search-wrapper').fadeOut(200);
        });

        // Check if any sidebar item has the 'active' class
        $(".sidebar-item").each(function () {
            if ($(this).hasClass("active")) {
                // Get the parent <ul> of the active <li> and add 'show' class
                $(this).closest("ul").addClass("show");
                // Add active class on the parent <li> (which is the dropdown)
                $(this).closest("li").addClass("active");
            }
        });

        // Add active class on click to the sidebar header
        $(".sidebar-header").on("click", function () {
            // Get the parent <li> of the clicked sidebar header
            var parentLi = $(this).closest("li");

            // Remove 'active' class from all sidebar headers and dropdowns
            $(".sidebar-header").removeClass("active");
            $(".nav-item").removeClass("active");  // Remove 'active' class from all <li> items
            $(".collapse").removeClass("show");    // Collapse all dropdowns

            // Add 'active' class to the clicked sidebar header
            $(this).addClass("active");

            // Show the corresponding <ul> and add the 'active' class to its parent <li>
            parentLi.find("ul").addClass("show");
            parentLi.addClass("active");
        });


        // Toggle the dropdown menu on click
        $('#userDropdown, #settingsDropdown').on('click', function (event) {
            event.preventDefault();
            $(this).next('.dropdown-menu').toggle();
            $(this).attr('aria-expanded', $(this).next('.dropdown-menu').is(':visible'));
        });

        // Close the dropdown menu when clicking outside of it
        $(document).on('click', function (event) {
            if (!$(event.target).closest('.nav-item.dropdown').length) {
                $('.dropdown-menu').hide();
                $('.nav-item.dropdown a').attr('aria-expanded', 'false');
            }
        });


    });

})(window, document, window.jQuery);
