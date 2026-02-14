import {LitElement, css, html} from "lit"
import {customElement, query, state} from "lit/decorators.js"
import { DivMovable } from "@db/client/components/divMovable.js"

type consoleType = "info" | "error"

@customElement("db-console")
export class DbConsole extends LitElement {

    static styles = css`
        :host {
            position: fixed;
            display: block;
            height: 40vh;
            width: 60vw;
        }
        #terminal {
            background-color: #1a1a1a;
            color: var(--wa-color-success-fill-loud);;
            font-family: 'Courier New', Courier, monospace;
            font-size: 0.8rem;
            padding: 1rem;
            height: 100%;
            overflow-y: auto;
            border-radius: 5px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.5);
            line-height: 1;
        }
        header {
            background: var(--wa-color-overlay-modal);
            cursor: move;
        }
        .line {
            margin-bottom: 4px;
            white-space: pre-wrap;
            word-break: break-all;
            &.error {
                color: var(--wa-color-danger-fill-loud);
            }
        }
    `

    @query("#terminal")
    terminal?: HTMLDivElement

    @query("header")
    header?: HTMLDivElement

    errorToTerminal(error?: Error) {
        let msg = "Error"

        if (error) {
            if (error.name)
                msg += ` ${error.name}`
            if (error.message)
                msg += ` ${error.message}`
            if (error.cause)
                msg += ` ${error.cause}`
        }
        this.appendToTerminal(msg, "error")
    }
    appendToTerminal(message: string, type: consoleType = "info") {
        if (!this.terminal)
            return

        const output = this.terminal.querySelector("#output") as unknown as HTMLDivElement

        const line = document.createElement('div')
        line.className = "line"
        if (type == "error")
            line.classList.add("error")
        
        const timestamp = new Date().toLocaleTimeString()
        line.textContent = `[${timestamp}] ${message}`;
        
        output.appendChild(line);

        if (output.childNodes.length > 100) {
            output.removeChild(output.firstChild!);
        }

        this.terminal.scrollTop = this.terminal.scrollHeight;
    }
    render() {
        return html`
            <header>
                <wa-icon name="terminal"></wa-icon>
            </header>
            <div id="terminal">
                <div id="output"></div>
                <div id="anchor"></div>
            </div>
        `
    }

    formatDateTime(date: Date) {
        const year = date.getFullYear();
        // getMonth() is zero-based (0=Jan, 11=Dec), so add 1
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        const seconds = String(date.getSeconds()).padStart(2, '0');

        return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
    }
}