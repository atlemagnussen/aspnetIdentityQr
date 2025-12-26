const modulesToLoad = [
    "/components/button/button.js",
    "/components/icon/icon.js",
    "/components/tooltip/tooltip.js",
    "/components/switch/switch.js",
    "/components/popover/popover.js",
    "/components/avatar/avatar.js",
    "/components/dropdown/dropdown.js",
    "/components/details/details.js",
    "/components/input/input.js",
    "/components/select/select.js",
    "/components/option/option.js",
    "/components/radio-group/radio-group.js",
    "/components/radio/radio.js",
    "/components/textarea/textarea.js",
    "/components/dialog/dialog.js"
]

const baseUrl = "https://static.logout.work/webawesome/3.1.0/dist"

const mainCss = "/styles/themes/default.css"

function createCss() {
    const styleCss = document.createElement("link")
    styleCss.rel = "stylesheet"
    styleCss.href = `${baseUrl}${mainCss}`
    document.head.appendChild(styleCss)
}

async function importComponents() {
    createCss()

    const promises = modulesToLoad.map(m => import(`${baseUrl}${m}`))

    await Promise.all(promises)
    return true
}

export async function doImportWa() {
    const success = await importComponents();
    return success
}