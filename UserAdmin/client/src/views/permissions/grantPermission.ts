import WaDialog from "@awesome.me/webawesome/dist/components/dialog/dialog.js"
import { DatabaseRole } from "@db/client/models/DatabaseRole.js"
import { css, html, LitElement, nothing } from "lit";
import { customElement, query, state } from "lit/decorators.js";

import * as dbService from "@db/client/services/dbService.js"

type GrantPermissionForm = {
    dbName: string
    roleName: DatabaseRole
    userName: string
}

@customElement("grant-permission")
export class GrantPermissionDialog extends LitElement {

    static styles = css`
        :host {
            display: block;
        }
        form {
            display: flex;
            flex-direction: column;
            gap: var(--wa-space-m);
        }
    `

    open() {
        if (this.dialog)
            this.dialog.open = true
    }
    close() {
        if (this.dialog)
            this.dialog.open = false
    }

    @state()
    loading = false

    submitGrant(evt: SubmitEvent) {
        evt.preventDefault()
        const formEl = evt.target as HTMLFormElement

        const formData = new FormData(formEl)

        let req = Object.fromEntries(formData) as GrantPermissionForm
        console.log(req)
        this.addPermission(req)
    }

    async addPermission(formData: GrantPermissionForm) {
        this.loading = true
        this.error = undefined
        try {
            const { dbName, roleName, userName} = formData
            await dbService.assignRole(dbName, { roleName, userName});
        } catch (error: any) {
            console.log(error)
            this.error = error
        }
        finally {
            this.loading = false
            if (!this.error)
                this.close()
        }
    }

    @state()
    error?: Error

    @query("wa-dialog")
    dialog?: WaDialog

    render() {
        return html`
            <wa-dialog id="addPermissionDialog" label="Dialog" id="dialog-overview">
                <form @submit=${(evt: SubmitEvent) => this.submitGrant(evt)}>
                    <div style="display: flex; flex-direction: column; gap: 1rem;">
                        <wa-input name="dbName" label="Db Name" required></wa-input>
                        <wa-select name="roleName" with-clear value="db_datareader" label="Role" required>
                            ${this.renderDbRoles()}
                        </wa-select>
                        <wa-input name="userName" label="UserName" required></wa-input>
                    </div>
                    <error-viewer .error=${this.error}></error-viewer>
                    </div>
                    <div slot="footer">
                        <wa-button variant="brand" type="submit" required
                            loading=${this.loading || nothing} 
                            disabled=${this.loading || nothing}>
                            Add
                        </wa-button>
                        <wa-button data-dialog="close">Close</wa-button>
                    </div>
                </form>
            </wa-dialog>
        `
    }
    renderDbRoles() {
        return Object.entries(DatabaseRole).map(([key, value]) => {
            return html`<wa-option value="${value}">${key}</wa-option>`;
        })
    }
}