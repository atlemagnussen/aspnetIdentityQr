// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const systemDark = window.matchMedia("(prefers-color-scheme: dark)").matches;

if (systemDark)
    document.body.classList.add("dark")
else
    document.body.classList.add("light")