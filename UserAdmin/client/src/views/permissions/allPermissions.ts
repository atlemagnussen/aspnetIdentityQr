import {LitElement, css, html, nothing} from "lit"
import {customElement, query, state} from "lit/decorators.js"
import * as dbService from "@db/client/services/dbService.js"
import { Database } from "@db/client/models/Database.js"
import { DatabaseType } from "@db/client/models/DatabaseType.js"
import { GrantPermissionDialog } from "./grantPermission.js"

@customElement("all-permissions")
export class AllPermissionsComponent extends LitElement {

    static styles = css`
        :host {
            display: flex;
            flex-direction: column;
            flex-wrap: wrap;
            gap: 1rem;
        }
        button {
            cursor: pointer;
        }
        section {
            display: flex;
            gap: var(--wa-space-m);
        }
        fieldset {
            display: flex;
            flex-direction: row;
            flex-wrap: wrap;
            gap: 0.5rem;
        }
        .group-box {
            border: 1px solid #cbd5e1;
            border-radius: 6px;
            padding: 1rem;
        }
        .group-title {
            font-weight: 600;
            font-size: 1.2rem;
            padding: 0 0.5rem;
        }
    `
    connectedCallback(): void {
        super.connectedCallback()
        this.get()
    }

    @state()
    result?: Array<Database>

    @state()
    loading = false

    get = async () => {
        this.loading = true
        try {
            const dbs = await dbService.getDatabases()
            this.result = dbs
        } catch (err: any) {
            this.result = err.message
        }
        finally {
            this.loading = false
        }
    }
    render() {
        if (!this.result) {
            return this.loadMenu()
        }

        const dbsCustomer = this.result.filter(d => d.type == DatabaseType.Customer || d.type == DatabaseType.CustomerData);

        const grouped = Map.groupBy(dbsCustomer, (db) => db.server)

        console.log(grouped)

        return html`
            ${this.loadMenu()}
            ${Array.from(grouped).map(([type, entries]) => {
                return html`
                    <fieldset class="group-box">
                        <legend class="group-title">${type}</legend>
                        ${entries.map((d: Database) => {
                            return html`
                                <permissions-element dbname="${d.name}"></permissions-element>
                            `
                        })}
                    </fieldset>
               `
            })}
        `
    }

    @query("grant-permission")
    addPermissionDialog?: GrantPermissionDialog

    openAddPermission() {
        if (this.addPermissionDialog)
            this.addPermissionDialog.open()
    }

    loadMenu() {
        return html`
            <section>
                <!-- <wa-button appearance="outlined" variant="success">Add permission</wa-button> -->
                
                <wa-button id="btnOpenAddPermission" @click=${this.openAddPermission}>Add permission</wa-button>
                <grant-permission></grant-permission>
                <wa-button variant="brand" 
                    loading=${this.loading || nothing} @click=${() => this.get()}>
                    Load
                </wa-button>
            </section>
        `
    }
}