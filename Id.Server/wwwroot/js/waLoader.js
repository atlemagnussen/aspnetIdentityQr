const modulesToLoad = [
    "/components/button/button.js",      // wa-button
    "/components/icon/icon.js",          // wa-icon
    "/components/tooltip/tooltip.js",    // wa-tooltip
    "/components/checkbox/checkbox.js",   // wa-checkbox
    "/components/input/input.js",        // wa-input
    "/components/dialog/dialog.js",      // wa-dialog 
    "/components/callout/callout.js",    // wa-callout
    "/components/tag/tag.js",            // wa-tag
    "/components/copy-button/copy-button.js", // wa-copy-button
]

const baseUrl = "https://static.logout.work/webawesome/3.2.1/dist"

// Theme + native CSS are linked in _Layout.cshtml <head> (no flash of unstyled content).

async function importComponents() {
    const promises = modulesToLoad.map(m => import(`${baseUrl}${m}`))

    await Promise.all(promises)
    return true
}

export async function doImportWa() {
    const success = await importComponents();
    return success
}