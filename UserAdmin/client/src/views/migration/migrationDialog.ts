import { Database } from "@db/client/models/Database.js"
import {LitElement, css, html, nothing} from "lit"
import {customElement, property, query, state} from "lit/decorators.js"
import * as dbService from "@db/client/services/dbService.js"
import { MigrateOptions } from "@db/client/models/MigrateOptions.js"

@customElement("migration-dialog")
export class MigrationDialog extends LitElement {

    static styles = css`
        :host {
            display: block;
        }
        .output {
            background-color: #1a1a1a;
            color: white;
            font-family: 'Courier New', Courier, monospace;
            font-size: 0.8rem;
            padding: 1rem;
            height: 20rem;
            overflow-y: auto;
            border-radius: 5px;
            white-space: pre-wrap;
            word-break: break-all;
            line-height: 1;
        }
        header {
            background: var(--wa-color-overlay-modal);
            cursor: move;
        }
    `
    @property({attribute: false})
    db?: Database
    
    @query(".output")
    outputEl?: HTMLDivElement

    @state()
    runningScript = false

    @state()
    generatedScript = ""

    async migrateDb() {
        if (!this.db)
            return

        try {
            this.runningScript = true
            const options: MigrateOptions = {
                dbName: this.db.name,
                version: "3.7.1"
            }
            const res = await dbService.script(options)
            this.generatedScript = res

            if (this.outputEl) {
                this.outputEl.innerText = res
                this.outputEl.normalize()
            }
        }
        catch (err) {
            console.error(err)
        }
        finally {
            this.runningScript = false
        }
    }
    render() {
        if (!this.db)
            return html`<p>No db</p>`
        return html`
            <p>${this.db?.name} (${this.db.type}) - ${this.db.version} Can be migrated to <b>3.7.1</b></p>
            <div>
                <wa-button variant="brand"
                    @click=${this.migrateDb} loading=${this.runningScript || nothing}>
                    Create script
                </wa-button>
                <wa-button variant="neutral">
                    Migrate
                </wa-button>
            </div>
            <div class="output">
            </div>
        `
    }
}