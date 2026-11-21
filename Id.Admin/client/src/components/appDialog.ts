import { LitElement, css, html, nothing } from "lit"
import { customElement, property, state } from "lit/decorators.js"
import { valueOf } from "@db/client/components/adminElement.js"

export interface DialogOptions {
    title: string
    content: HTMLElement
    /** "normal" ~40rem, "wide" ~64rem */
    size?: "normal" | "wide"
    onClose?: () => void
}

export interface DialogHandle {
    close: () => void
    setTitle: (title: string) => void
}

/**
 * Modal native <dialog> with a header and close button. Removed from the DOM when closed.
 * Escape/backdrop don't close it, so half-filled forms aren't lost by accident.
 */
export function openDialog(options: DialogOptions): DialogHandle {
    const dialog = document.createElement("dialog")
    dialog.className = `app-dialog ${options.size ?? "normal"}`
    dialog.setAttribute("closedby", "none")
    dialog.addEventListener("cancel", e => e.preventDefault())

    const header = document.createElement("header")
    const title = document.createElement("h3")
    title.textContent = options.title
    header.appendChild(title)

    const closeBtn = document.createElement("wa-button")
    closeBtn.setAttribute("appearance", "plain")
    closeBtn.setAttribute("size", "small")
    closeBtn.innerHTML = `<wa-icon name="xmark" label="Close"></wa-icon>`
    header.appendChild(closeBtn)

    const article = document.createElement("article")
    article.appendChild(options.content)

    dialog.append(header, article)
    document.body.appendChild(dialog)

    const close = () => {
        dialog.close()
        dialog.remove()
        options.onClose?.()
    }
    closeBtn.addEventListener("click", close)
    // content can ask to be closed
    options.content.addEventListener("close-dialog", close)

    dialog.showModal()
    return { close, setTitle: t => (title.textContent = t) }
}

export interface ConfirmOptions {
    title: string
    message: string
    confirmLabel?: string
    danger?: boolean
    /** User must type this exact text to enable the confirm button */
    requireText?: string
}

export function confirmDialog(options: ConfirmOptions): Promise<boolean> {
    return new Promise(resolve => {
        const box = new ConfirmBox()
        box.options = options
        let answered = false
        const handle = openDialog({
            title: options.title,
            content: box,
            onClose: () => { if (!answered) resolve(false) },
        })
        box.addEventListener("answer", (e: Event) => {
            answered = true
            resolve((e as CustomEvent<boolean>).detail)
            handle.close()
        })
    })
}

export interface PromptOptions {
    title: string
    label: string
    value?: string
    confirmLabel?: string
    help?: string
}

export function promptDialog(options: PromptOptions): Promise<string | null> {
    return new Promise(resolve => {
        const box = new PromptBox()
        box.options = options
        box.value = options.value ?? ""
        let answered = false
        const handle = openDialog({
            title: options.title,
            content: box,
            onClose: () => { if (!answered) resolve(null) },
        })
        box.addEventListener("answer", (e: Event) => {
            answered = true
            resolve((e as CustomEvent<string | null>).detail)
            handle.close()
        })
    })
}

const boxStyles = css`
    :host {
        display: grid;
        gap: var(--wa-space-m);
        min-width: min(28rem, 80vw);
    }
    p {
        margin: 0;
        white-space: pre-line;
    }
    .actions {
        display: flex;
        justify-content: flex-end;
        gap: var(--wa-space-xs);
    }
    code {
        font-family: var(--wa-font-family-code);
        background: var(--wa-color-surface-lowered);
        padding: 0 var(--wa-space-3xs);
        border-radius: var(--wa-border-radius-s);
    }
`

@customElement("confirm-box")
class ConfirmBox extends LitElement {
    static styles = boxStyles

    @property({ attribute: false })
    options!: ConfirmOptions

    @state()
    private typed = ""

    private answer(value: boolean) {
        this.dispatchEvent(new CustomEvent("answer", { detail: value }))
    }

    render() {
        const o = this.options
        const blocked = !!o.requireText && this.typed !== o.requireText
        return html`
            <p>${o.message}</p>
            ${o.requireText
                ? html`
                    <wa-input
                        label="Type ${o.requireText} to confirm"
                        autocomplete="off"
                        @input=${(e: Event) => (this.typed = valueOf(e))}
                    ></wa-input>`
                : nothing}
            <div class="actions">
                <wa-button appearance="outlined" @click=${() => this.answer(false)}>Cancel</wa-button>
                <wa-button
                    variant=${o.danger ? "danger" : "brand"}
                    ?disabled=${blocked}
                    @click=${() => this.answer(true)}
                >${o.confirmLabel ?? "Confirm"}</wa-button>
            </div>
        `
    }
}

@customElement("prompt-box")
class PromptBox extends LitElement {
    static styles = boxStyles

    @property({ attribute: false })
    options!: PromptOptions

    @state()
    value = ""

    private answer(value: string | null) {
        this.dispatchEvent(new CustomEvent("answer", { detail: value }))
    }

    render() {
        const o = this.options
        return html`
            <wa-input
                label=${o.label}
                hint=${o.help ?? ""}
                .value=${this.value}
                autofocus
                @input=${(e: Event) => (this.value = valueOf(e))}
                @keydown=${(e: KeyboardEvent) => { if (e.key === "Enter" && this.value.trim()) this.answer(this.value.trim()) }}
            ></wa-input>
            <div class="actions">
                <wa-button appearance="outlined" @click=${() => this.answer(null)}>Cancel</wa-button>
                <wa-button variant="brand" ?disabled=${!this.value.trim()} @click=${() => this.answer(this.value.trim())}>
                    ${o.confirmLabel ?? "Save"}
                </wa-button>
            </div>
        `
    }
}
