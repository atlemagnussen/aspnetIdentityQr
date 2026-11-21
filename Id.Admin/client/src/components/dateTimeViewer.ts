import {LitElement, html, css} from "lit"
import {customElement, property} from "lit/decorators.js"
import { formatDateTime, formatDateTimeLong, formatDateTimeShort } from "./dateTimeFormattingLocale.js"

export type DateTimeViewMode = "regular" | "short" | "long"

@customElement('datetime-viewer')
export class DateTimeViewer extends LitElement {
    static styles = css`
        :host {
            display: inline;
        }
    `

    @property({attribute: true})
    date = ""

    @property()
    mode: DateTimeViewMode = "regular"

    render() {
        let dateFormatted = formatDateTime(this.date)

        if (this.mode == "short")
            dateFormatted = formatDateTimeShort(this.date)
        else if (this.mode == "long")
            dateFormatted = formatDateTimeLong(this.date)

        return html`<time datetime="${this.date}">${dateFormatted}</time>`
    }
}