
window.layoutInterop = {
    breakpoint: 768, // Standard-Breakpoint (Bootstrap md)
    dotNetHelper: null,
    throttleTimeout: null,

    initializeNavMenu: function (dotNetHelper, breakpoint) {
        this.dotNetHelper = dotNetHelper;
        if (breakpoint) {
            this.breakpoint = breakpoint;
        }
        window.addEventListener('resize', this.throttleResize.bind(this));
        this.checkViewportState(); // Initial check
        // console.log("NavMenu interop initialized with breakpoint: " + this.breakpoint);
    },

    throttleResize: function () {
        if (!this.throttleTimeout) {
            this.throttleTimeout = setTimeout(() => {
                this.checkViewportState();
                this.throttleTimeout = null;
            }, 200); // Throttle resize events
        }
    },

    checkViewportState: function () {
        if (this.dotNetHelper) {
            const isDesktop = window.innerWidth >= this.breakpoint;
            try {
                this.dotNetHelper.invokeMethodAsync('UpdateViewportState', isDesktop);
            } catch (error) {
                console.error("Error invoking .NET method UpdateViewportState: ", error);
                // Potentially dispose or re-initialize if helper is invalid
                if (error.message.includes("JavaScript interop calls cannot be issued during server-side prerendering")) {
                    // This is fine, will be called again on client
                } else if (error.message.includes("instance is already disposed")) {
                    console.warn("DotNet object reference disposed.");
                    this.disposeNavMenu(); // Clean up listener if .NET side is gone
                }
            }
        }
    },

    disposeNavMenu: function () {
        window.removeEventListener('resize', this.throttleResize.bind(this));
        // console.log("NavMenu interop disposed.");
        this.dotNetHelper = null; // Clear reference
    }
};