import { LitElement, css, html, nothing } from "lit"
import { customElement, property } from "lit/decorators.js"
import { formatDateTime } from "@db/client/components/dateTimeFormattingLocale.js"

/** Shows a value the admin must copy now (set-password link, client secret) with a copy button. */
@customElement("one-time-value")
export class OneTimeValue extends LitElement {
    static styles = css`
        :host {
            display: block;
        }
        wa-callout {
            display: block;
        }
        .row {
            display: flex;
            align-items: center;
            gap: var(--wa-space-xs);
            margin-top: var(--wa-space-xs);
        }
        code {
            flex: 1;
            font-family: var(--wa-font-family-code);
            font-size: var(--wa-font-size-s);
            word-break: break-all;
            background: var(--wa-color-surface-lowered);
            padding: var(--wa-space-xs);
            border-radius: var(--wa-border-radius-s);
        }
        p {
            margin: 0;
        }
    `

    @property()
    heading = ""

    @property()
    value = ""

    @property()
    message = ""

    @property()
    expiresAt?: string | null

    render() {
        return html`
            <wa-callout variant="warning">
                <wa-icon slot="icon" name="key"></wa-icon>
                <p><strong>${this.heading}</strong></p>
                ${this.message ? html`<p>${this.message}</p>` : nothing}
                <div class="row">
                    <code>${this.value}</code>
                    <wa-copy-button value=${this.value}></wa-copy-button>
                </div>
                ${this.expiresAt ? html`<p>Valid until ${formatDateTime(this.expiresAt)}.</p>` : nothing}
            </wa-callout>
        `
    }
}
