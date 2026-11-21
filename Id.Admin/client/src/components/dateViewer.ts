import {LitElement, html, css} from "lit"
import {customElement, property} from "lit/decorators.js"
import { formatDateOnly, formatDateTime } from "./dateTimeFormattingLocale.js"

@customElement('date-viewer')
export class DateViewer extends LitElement {
    static styles = css`
        :host {
            display: inline;
        }
    `

    @property({attribute: true})
    date = ""

    render() {
        const dateFormatted = formatDateOnly(this.date)
        let formattedDateTime = formatDateTime(this.date)
        this.title = formattedDateTime

        return html`<time datetime="${this.date}">${dateFormatted}</time>`
    }
}