// Small browser helpers called from Blazor through JS interop (a file, because the CSP forbids inline scripts)
window.rmd = {
    // Starts the login background video on larger screens only. Phones, reduced-motion and
    // data-saver users keep the still image and never download the video.
    startBackgroundVideo: function (video, src) {
        if (!video || video.getAttribute("src")) return;
        if (!window.matchMedia("(min-width: 768px) and (prefers-reduced-motion: no-preference)").matches) return;
        if (navigator.connection && navigator.connection.saveData) return;

        // Blazor-created elements don't get the muted property from the attribute, and unmuted video may not autoplay
        video.muted = true;
        video.src = src;
        video.play().catch(function () { /* autoplay blocked: the poster stays */ });
    }
};
