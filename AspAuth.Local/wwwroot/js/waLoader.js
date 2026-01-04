const modulesToLoad = [
    "/components/button/button.js",      // wa-button
    "/components/icon/icon.js",          // wa-icon
    "/components/tooltip/tooltip.js",    // wa-tooltip
    "/components/checkbox/checkbox.js",   // wa-checkbox
    "/components/popover/popover.js",    // wa-popover
    "/components/avatar/avatar.js",      // wa-avatar
    "/components/input/input.js",        // wa-input
    "/components/dialog/dialog.js",      // wa-dialog 
    "/components/callout/callout.js"     // wa-callout
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