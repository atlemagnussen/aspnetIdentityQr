import type { UserListItemDto } from "@db/api"
import { html, nothing } from "lit"
import { customElement, state } from "lit/decorators.js"
import { AdminElement, valueOf } from "@db/client/components/adminElement.js"
import { adminStyles } from "@db/client/components/sharedStyles.js"
import { openDialog } from "@db/client/components/appDialog.js"
import * as users from "@db/client/views/users/usersService.js"
import "@db/client/views/users/userEditor.js"
import "@db/client/views/users/userCreate.js"
import { UserEditor } from "@db/client/views/users/userEditor.js"
import { UserCreate } from "@db/client/views/users/userCreate.js"

@customElement("users-list")
export class UsersList extends AdminElement {
    static styles = adminStyles

    @state()
    private users?: UserListItemDto[]

    @state()
    private filter = ""

    connectedCallback(): void {
        super.connectedCallback()
        this.load()
    }

    private load = async () => {
        const result = await this.run(() => users.getUsers())
        if (result) this.users = result
    }

    private open(user: UserListItemDto) {
        const editor = new UserEditor()
        editor.userId = user.id
        editor.addEventListener("user-changed", this.load)
        editor.addEventListener("user-deleted", this.load)
        openDialog({ title: user.userName, content: editor, size: "wide" })
    }

    private create() {
        const form = new UserCreate()
        form.addEventListener("user-changed", this.load)
        openDialog({ title: "New user", content: form })
    }

    private get filtered() {
        const q = this.filter.trim().toLowerCase()
        if (!q) return this.users ?? []
        return (this.users ?? []).filter(u =>
            [u.userName, u.email, u.fullName, ...u.roles, ...u.externalLogins]
                .some(v => v?.toLowerCase().includes(q)))
    }

    private status(u: UserListItemDto) {
        return html`
            ${u.isLockedOut
                ? html`<wa-badge variant="danger">${users.isIndefinite(u.lockoutEnd) ? "Locked" : "Locked (temp)"}</wa-badge>`
                : nothing}
            ${u.email && !u.emailConfirmed ? html`<wa-badge variant="warning">Unconfirmed</wa-badge>` : nothing}
            ${u.twoFactorEnabled ? html`<wa-badge variant="success">2FA</wa-badge>` : nothing}
            ${!u.hasPassword && u.externalLogins.length === 0
                ? html`<wa-badge variant="neutral" title="No password and no external login">No sign-in</wa-badge>`
                : nothing}
        `
    }

    render() {
        const list = this.filtered
        return html`
            <div class="header">
                <h2>Users</h2>
                <wa-button variant="brand" @click=${this.create}>
                    <wa-icon slot="start" name="user-plus"></wa-icon>New user
                </wa-button>
            </div>
            <div class="toolbar">
                <wa-input class="grow" placeholder="Search name, email, role, login provider" with-clear
                    .value=${this.filter} @input=${(e: Event) => (this.filter = valueOf(e))}>
                    <wa-icon slot="start" name="magnifying-glass"></wa-icon>
                </wa-input>
                <wa-button appearance="outlined" ?loading=${this.busy} @click=${this.load}>
                    <wa-icon slot="start" name="rotate"></wa-icon>Refresh
                </wa-button>
            </div>
            <error-viewer .error=${this.error}></error-viewer>
            ${!this.users
                ? (this.busy ? html`<div class="empty"><wa-spinner></wa-spinner></div>` : nothing)
                : list.length === 0
                    ? html`<div class="empty">${this.filter ? "No users match the search." : "No users yet."}</div>`
                    : html`
                <table>
                    <thead>
                        <tr>
                            <th>User</th>
                            <th class="hide-sm">Name</th>
                            <th>Roles</th>
                            <th class="hide-sm">Sign-in</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${list.map(u => html`
                            <tr class="clickable" @click=${() => this.open(u)}>
                                <td>
                                    <div>${u.userName}</div>
                                    ${u.email && u.email !== u.userName ? html`<div class="muted">${u.email}</div>` : nothing}
                                </td>
                                <td class="hide-sm">${u.fullName ?? html`<span class="muted">-</span>`}</td>
                                <td><div class="tags">${u.roles.map(r => html`<wa-tag size="small">${r}</wa-tag>`)}</div></td>
                                <td class="hide-sm muted">
                                    ${[u.hasPassword ? "Password" : null, ...u.externalLogins].filter(Boolean).join(", ") || "-"}
                                </td>
                                <td><div class="tags">${this.status(u)}</div></td>
                            </tr>`)}
                    </tbody>
                </table>
                <p class="muted">${list.length} of ${this.users.length} users</p>`}
        `
    }
}
