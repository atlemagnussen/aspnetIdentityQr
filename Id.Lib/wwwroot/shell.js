// Shared by Id.Server and Id.Admin, served at /_content/Id.Lib/shell.js.
// Load it as a classic (blocking) script in <head>: it sets the colour scheme before first paint.

(function () {
    // Colour scheme: follow the OS, live.
    const dark = matchMedia("(prefers-color-scheme: dark)")
    const applyScheme = () => {
        const root = document.documentElement
        root.classList.toggle("wa-dark", dark.matches)
        root.classList.toggle("wa-light", !dark.matches)
    }
    applyScheme()
    dark.addEventListener("change", applyScheme)

    // Hamburger toggle for the slide-in nav (body.app-shell, below 768px). See shell.css.
    function initNav() {
        const toggle = document.querySelector(".nav-toggle")
        const nav = document.getElementById("app-nav")
        const backdrop = document.querySelector(".nav-backdrop")
        if (!toggle || !nav) return

        const isOpen = () => document.body.classList.contains("nav-open")
        const setOpen = open => {
            document.body.classList.toggle("nav-open", open)
            toggle.setAttribute("aria-expanded", String(open))
            toggle.setAttribute("aria-label", open ? "Close menu" : "Open menu")
            if (open) nav.querySelector("a, button")?.focus()
        }

        toggle.addEventListener("click", () => setOpen(!isOpen()))
        backdrop?.addEventListener("click", () => setOpen(false))
        document.addEventListener("keydown", e => {
            if (e.key === "Escape" && isOpen()) {
                setOpen(false)
                toggle.focus()
            }
        })
        // Growing past the breakpoint with the menu open must not leave the page in the open state.
        matchMedia("(min-width: 768px)").addEventListener("change", e => { if (e.matches) setOpen(false) })
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", initNav)
    else initNav()
})()
