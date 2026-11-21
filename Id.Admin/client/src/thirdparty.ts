const modulesToLoad = [
    "/components/button/button.js",          // wa-button
    "/components/icon/icon.js",              // wa-icon
    "/components/tooltip/tooltip.js",        // wa-tooltip
    "/components/checkbox/checkbox.js",      // wa-checkbox
    "/components/switch/switch.js",          // wa-switch
    "/components/popover/popover.js",        // wa-popover
    "/components/avatar/avatar.js",          // wa-avatar
    "/components/input/input.js",            // wa-input
    "/components/textarea/textarea.js",      // wa-textarea
    "/components/dialog/dialog.js",          // wa-dialog
    "/components/callout/callout.js",        // wa-callout
    "/components/select/select.js",          // wa-select
    "/components/option/option.js",          // wa-option (needed by wa-select)
    "/components/tab-group/tab-group.js",    // wa-tab-group
    "/components/tab/tab.js",                // wa-tab
    "/components/tab-panel/tab-panel.js",    // wa-tab-panel
    "/components/badge/badge.js",            // wa-badge
    "/components/tag/tag.js",                // wa-tag
    "/components/spinner/spinner.js",        // wa-spinner
    "/components/copy-button/copy-button.js",// wa-copy-button
    "/components/divider/divider.js",        // wa-divider
]

const baseUrlWa = "https://static.logout.work/webawesome/3.2.1/dist"

const syntaxHighlighterJs = "https://cdn.jsdelivr.net/npm/syntax-highlight-element@1/+esm"
const syntaxHighlighterCss = "https://cdn.jsdelivr.net/npm/syntax-highlight-element@1/dist/themes/prettylights.min.css"

function createCss(url: string) {
    const styleCss = document.createElement("link")
    styleCss.rel = "stylesheet"
    styleCss.href = url
    document.head.appendChild(styleCss)
}

async function importWaComponents() {
    // Theme + native CSS are linked in _Layout.cshtml <head> (no flash of unstyled content).
    const promises = modulesToLoad.map(m => import(`${baseUrlWa}${m}`))

    await Promise.all(promises)
    return true
}

async function importSyntaxHighlighter() {
    /// @ts-ignore
    window.she = window.she || {};
    /// @ts-ignore
    window.she.config = {
        languages: ['sql']
    }
    createCss(syntaxHighlighterCss)
    await import(syntaxHighlighterJs)
    
    /**
     * @typedef Config
     * @type {object}
     * @property {string[]} languages - List of languages to support for syntax highlighting.
     * @property {string[]} tokenTypes - Token types used during lexing/parsing.
     * @property {{[key: string]: string[]}} languageTokens - Mapping of language names to their specific tokenization rules.
     * @property {function} setup - Runs before the custom element gets defined in the registry.
     * @property {function} tokenize - Tokenize text based on the specified language grammar
     */

    /// @ts-ignore
    //window.she = window.she || {};
    /// @ts-ignore
    // window.she.config = {
    //     languages: ['sql']
    // }
}

export async function importThirdParty() {
    importWaComponents()
    // importSyntaxHighlighter()
}