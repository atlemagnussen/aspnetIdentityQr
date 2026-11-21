import type { RoleDto } from "@db/api"
import { html, nothing } from "lit"
import { customElement, state } from "lit/decorators.js"
import { AdminElement, valueOf } from "@db/client/components/adminElement.js"
import { adminStyles } from "@db/client/components/sharedStyles.js"
import { confirmDialog, promptDialog } from "@db/client/components/appDialog.js"
import * as rolesApi from "@db/client/views/roles/rolesService.js"

@customElement("roles-list")
export class RolesList extends AdminElement {
    static styles = adminStyles

    @state()
    private roles?: RoleDto[]

    @state()
    private newName = ""

    connectedCallback(): void {
        super.connectedCallback()
        this.load()
    }

    private load = async () => {
        const result = await this.run(() => rolesApi.getRoles())
        if (result) this.roles = result
    }

    private async create() {
        const name = this.newName.trim()
        const created = await this.run(() => rolesApi.createRole(name), `Role ${name} created.`)
        if (created) {
            this.newName = ""
            await this.reloadKeepingNotice()
        }
    }

    private async rename(role: RoleDto) {
        const name = await promptDialog({
            title: `Rename ${role.name}`,
            label: "New name",
            value: role.name,
            confirmLabel: "Rename",
            help: "Apps checking for the old role name stop matching. Users get the new name in their next token.",
        })
        if (!name || name === role.name) return
        if (await this.run(() => rolesApi.renameRole(role.id, name), `Renamed to ${name}.`))
            await this.reloadKeepingNotice()
    }

    private async removeItem(role: RoleDto) {
        if (!await confirmDialog({
            title: `Delete ${role.name}`,
            message: role.userCount
                ? `${role.userCount} user(s) have this role and will lose it. Apps checking for it stop granting access.`
                : "No users have this role.",
            confirmLabel: "Delete role", danger: true,
            requireText: role.userCount ? role.name : undefined,
        }))
            return
        if (await this.run(async () => { await rolesApi.deleteRole(role.id); return true }, `Role ${role.name} deleted.`))
            await this.reloadKeepingNotice()
    }

    private async reloadKeepingNotice() {
        const notice = this.notice
        await this.load()
        if (!this.error) this.notice = notice
    }

    render() {
        return html`
            <div class="header"><h2>Roles</h2></div>
            <p class="help">Roles are emitted as <code>role</code> claims by the ID server to clients requesting the
                <code>roles</code> scope. System roles are used by the apps themselves (Admin = access to this app).</p>
            <div class="toolbar" @keydown=${(e: KeyboardEvent) => { if (e.key === "Enter" && this.newName.trim()) this.create() }}>
                <wa-input class="grow" label="New role" placeholder="e.g. Editor" .value=${this.newName}
                    @input=${(e: Event) => (this.newName = valueOf(e))}></wa-input>
                <wa-button variant="brand" ?disabled=${!this.newName.trim()} ?loading=${this.busy} @click=${this.create}>
                    <wa-icon slot="start" name="plus"></wa-icon>Create
                </wa-button>
            </div>
            <error-viewer .error=${this.error}></error-viewer>
            ${this.notice ? html`<div class="notice">${this.notice}</div>` : nothing}
            ${!this.roles
                ? nothing
                : this.roles.length === 0
                    ? html`<div class="empty">No roles yet.</div>`
                    : html`
                <table>
                    <thead><tr><th>Role</th><th>Users</th><th></th></tr></thead>
                    <tbody>
                        ${this.roles.map(r => html`
                            <tr>
                                <td>${r.name} ${r.isSystem ? html`<wa-badge variant="neutral">system</wa-badge>` : nothing}</td>
                                <td>${r.userCount}</td>
                                <td class="right">
                                    ${r.isSystem ? nothing : html`
                                        <wa-button size="small" appearance="plain" @click=${() => this.rename(r)}>
                                            <wa-icon name="pen-to-square" label="Rename"></wa-icon>
                                        </wa-button>
                                        <wa-button size="small" appearance="plain" variant="danger" @click=${() => this.removeItem(r)}>
                                            <wa-icon name="trash" label="Delete"></wa-icon>
                                        </wa-button>`}
                                </td>
                            </tr>`)}
                    </tbody>
                </table>`}
        `
    }
}
