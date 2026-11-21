import {LitElement, html, css, nothing} from "lit"
import {customElement, property} from "lit/decorators.js"

export type DateTimeViewMode = "regular" | "short" | "long"

@customElement('error-viewer')
export class ErrorPdViewer extends LitElement {
    static styles = css`
        :host {
            display: block;
        }
        div#errormsg {
            background: var(--wa-color-danger-fill-quiet);
            border: var(--wa-border-width-m) solid var(--wa-color-danger-border-normal);
            border-radius: var(--wa-border-radius-m);
            color: var(--wa-color-danger-on-quiet);
            padding: var(--wa-space-s);
            p {
                margin-block-start: 0;
                margin-block-end: 0;
            }
        }
    `

    @property({attribute: false})
    error?: Error

    render() {
        if (!this.error)
            return nothing

        const msg = this.error.message
        return html`
            <div id="errormsg">
                ${msg}
            </div>
        `
    }
}