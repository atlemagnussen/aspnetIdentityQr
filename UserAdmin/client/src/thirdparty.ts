const modulesToLoad = [
    "/components/button/button.js",      // wa-button
    "/components/icon/icon.js",          // wa-icon
    "/components/tooltip/tooltip.js",    // wa-tooltip
    "/components/checkbox/checkbox.js",   // wa-checkbox
    "/components/popover/popover.js",    // wa-popover
    "/components/avatar/avatar.js",      // wa-avatar
    "/components/input/input.js",        // wa-input
    "/components/dialog/dialog.js",      // wa-dialog 
    "/components/callout/callout.js",     // wa-callout
    "/components/dialog/dialog.js",      // wa-dialog
    "/components/select/select.js",      // wa-select
]

const baseUrlWa = "https://static.internal.digilean.no/webawesome/3.1.0"
const mainCssWa = "/styles/themes/default.css"

const syntaxHighlighterJs = "https://cdn.jsdelivr.net/npm/syntax-highlight-element@1/+esm"
const syntaxHighlighterCss = "https://cdn.jsdelivr.net/npm/syntax-highlight-element@1/dist/themes/prettylights.min.css"

function createCss(url: string) {
    const styleCss = document.createElement("link")
    styleCss.rel = "stylesheet"
    styleCss.href = url
    document.head.appendChild(styleCss)
}

async function importWaComponents() {
    createCss(`${baseUrlWa}${mainCssWa}`)

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