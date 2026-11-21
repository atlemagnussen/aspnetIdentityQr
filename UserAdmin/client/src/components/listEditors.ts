import { LitElement, css, html, nothing } from "lit"
import { customElement, property, state } from "lit/decorators.js"
import { valueOf } from "@db/client/components/adminElement.js"

const listStyles = css`
    :host {
        display: grid;
        gap: var(--wa-space-xs);
    }
    .label {
        font-weight: var(--wa-font-weight-semibold);
        font-size: var(--wa-font-size-s);
    }
    .hint {
        color: var(--wa-color-text-quiet);
        font-size: var(--wa-font-size-s);
        margin: 0;
    }
    ul {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: var(--wa-space-3xs);
    }
    li {
        display: flex;
        align-items: center;
        gap: var(--wa-space-xs);
        padding: var(--wa-space-3xs) var(--wa-space-xs);
        background: var(--wa-color-surface-lowered);
        border-radius: var(--wa-border-radius-s);
    }
    li .text {
        flex: 1;
        font-family: var(--wa-font-family-code);
        font-size: var(--wa-font-size-s);
        word-break: break-all;
    }
    li .kv {
        flex: 1;
        display: grid;
        grid-template-columns: minmax(6rem, 1fr) 2fr;
        gap: var(--wa-space-xs);
        font-family: var(--wa-font-family-code);
        font-size: var(--wa-font-size-s);
        word-break: break-all;
    }
    .add {
        display: flex;
        gap: var(--wa-space-xs);
        align-items: start;
    }
    .add wa-input {
        flex: 1;
    }
    .quick {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--wa-space-2xs);
    }
    .error {
        color: var(--wa-color-danger-on-quiet);
        font-size: var(--wa-font-size-s);
        margin: 0;
    }
    .empty {
        color: var(--wa-color-text-quiet);
        font-size: var(--wa-font-size-s);
        font-style: italic;
    }
`

/**
 * Editable list of strings. Fires `change` (detail: string[]) whenever the list changes.
 * `validate` returns an error message or null; `normalize` cleans a value before adding.
 */
@customElement("string-list-editor")
export class StringListEditor extends LitElement {
    static styles = listStyles

    @property()
    label = ""

    @property()
    hint = ""

    @property()
    placeholder = ""

    @property({ attribute: false })
    items: string[] = []

    @property({ attribute: false })
    suggestions: string[] = []

    @property({ attribute: false })
    validate?: (value: string) => string | null

    @property({ type: Boolean })
    disabled = false

    @state()
    private draft = ""

    @state()
    private error = ""

    private emit(items: string[]) {
        this.items = items
        this.dispatchEvent(new CustomEvent("change", { detail: items, bubbles: true, composed: true }))
    }

    private add() {
        const value = this.draft.trim()
        if (!value) return
        const error = this.validate?.(value) ?? null
        if (error) {
            this.error = error
            return
        }
        if (this.items.includes(value)) {
            this.error = "Already in the list."
            return
        }
        this.emit([...this.items, value])
        this.draft = ""
        this.error = ""
    }

    private removeItem(value: string) {
        this.emit(this.items.filter(i => i !== value))
    }

    render() {
        const quick = this.suggestions.filter(s => !this.items.includes(s))
        return html`
            ${this.label ? html`<span class="label">${this.label}</span>` : nothing}
            ${this.hint ? html`<p class="hint">${this.hint}</p>` : nothing}
            ${this.items.length
                ? html`<ul>
                    ${this.items.map(i => html`
                        <li>
                            <span class="text">${i}</span>
                            <wa-button size="small" appearance="plain" variant="danger" ?disabled=${this.disabled}
                                @click=${() => this.removeItem(i)}>
                                <wa-icon name="trash" label="Remove ${i}"></wa-icon>
                            </wa-button>
                        </li>`)}
                  </ul>`
                : html`<span class="empty">None</span>`}
            ${this.disabled ? nothing : html`
                <div class="add">
                    <wa-input
                        size="small"
                        placeholder=${this.placeholder}
                        .value=${this.draft}
                        @input=${(e: Event) => { this.draft = valueOf(e); this.error = "" }}
                        @keydown=${(e: KeyboardEvent) => { if (e.key === "Enter") { e.preventDefault(); this.add() } }}
                    ></wa-input>
                    <wa-button size="small" ?disabled=${!this.draft.trim()} @click=${this.add}>
                        <wa-icon slot="start" name="plus"></wa-icon>Add
                    </wa-button>
                </div>
                ${quick.length
                    ? html`<div class="quick">
                        <span class="hint">Quick add:</span>
                        ${quick.map(s => html`
                            <wa-button size="small" appearance="outlined" pill @click=${() => this.emit([...this.items, s])}>${s}</wa-button>`)}
                      </div>`
                    : nothing}
                ${this.error ? html`<p class="error">${this.error}</p>` : nothing}
            `}
        `
    }
}

export interface KeyValue {
    key: string
    value: string
}

/** Editable list of key/value pairs. Fires `change` (detail: KeyValue[]). */
@customElement("key-value-editor")
export class KeyValueEditor extends LitElement {
    static styles = listStyles

    @property()
    label = ""

    @property()
    hint = ""

    @property()
    keyLabel = "Key"

    @property()
    valueLabel = "Value"

    @property({ attribute: false })
    items: KeyValue[] = []

    /** Allow the same key more than once (claims) or not (properties) */
    @property({ type: Boolean })
    allowDuplicateKeys = false

    @state()
    private key = ""

    @state()
    private value = ""

    @state()
    private error = ""

    private emit(items: KeyValue[]) {
        this.items = items
        this.dispatchEvent(new CustomEvent("change", { detail: items, bubbles: true, composed: true }))
    }

    private add() {
        const key = this.key.trim()
        const value = this.value.trim()
        if (!key || !value) {
            this.error = `${this.keyLabel} and ${this.valueLabel.toLowerCase()} are required.`
            return
        }
        const duplicate = this.allowDuplicateKeys
            ? this.items.some(i => i.key === key && i.value === value)
            : this.items.some(i => i.key === key)
        if (duplicate) {
            this.error = this.allowDuplicateKeys ? "Already in the list." : `${this.keyLabel} '${key}' already exists.`
            return
        }
        this.emit([...this.items, { key, value }])
        this.key = ""
        this.value = ""
        this.error = ""
    }

    private removeItem(item: KeyValue) {
        this.emit(this.items.filter(i => i !== item))
    }

    render() {
        return html`
            ${this.label ? html`<span class="label">${this.label}</span>` : nothing}
            ${this.hint ? html`<p class="hint">${this.hint}</p>` : nothing}
            ${this.items.length
                ? html`<ul>
                    ${this.items.map(i => html`
                        <li>
                            <span class="kv"><span>${i.key}</span><span>${i.value}</span></span>
                            <wa-button size="small" appearance="plain" variant="danger" @click=${() => this.removeItem(i)}>
                                <wa-icon name="trash" label="Remove ${i.key}"></wa-icon>
                            </wa-button>
                        </li>`)}
                  </ul>`
                : html`<span class="empty">None</span>`}
            <div class="add">
                <wa-input size="small" placeholder=${this.keyLabel} .value=${this.key}
                    @input=${(e: Event) => { this.key = valueOf(e); this.error = "" }}></wa-input>
                <wa-input size="small" placeholder=${this.valueLabel} .value=${this.value}
                    @input=${(e: Event) => { this.value = valueOf(e); this.error = "" }}
                    @keydown=${(e: KeyboardEvent) => { if (e.key === "Enter") { e.preventDefault(); this.add() } }}></wa-input>
                <wa-button size="small" @click=${this.add}><wa-icon slot="start" name="plus"></wa-icon>Add</wa-button>
            </div>
            ${this.error ? html`<p class="error">${this.error}</p>` : nothing}
        `
    }
}
