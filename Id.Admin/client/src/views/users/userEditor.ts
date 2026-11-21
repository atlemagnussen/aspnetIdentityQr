import type { PasswordLinkDto, RoleDto, UserDetailsDto, UserLoginDto, PasskeyDto } from "@db/api"
import { html, nothing } from "lit"
import { customElement, property, state } from "lit/decorators.js"
import { AdminElement, checkedOf, valueOf } from "@db/client/components/adminElement.js"
import { adminStyles } from "@db/client/components/sharedStyles.js"
import { confirmDialog } from "@db/client/components/appDialog.js"
import { KeyValue } from "@db/client/components/listEditors.js"
import { formatDateTime } from "@db/client/components/dateTimeFormattingLocale.js"
import "@db/client/components/listEditors.js"
import "@db/client/components/oneTimeValue.js"
import * as users from "@db/client/views/users/usersService.js"
import * as rolesApi from "@db/client/views/roles/rolesService.js"

type LockChoice = "forever" | "1h" | "1d" | "7d" | "30d"

const lockDurations: Record<Exclude<LockChoice, "forever">, number> = {
    "1h": 3600e3, "1d": 86400e3, "7d": 7 * 86400e3, "30d": 30 * 86400e3,
}

@customElement("user-editor")
export class UserEditor extends AdminElement {
    static styles = adminStyles

    @property()
    userId = ""

    @state()
    private user?: UserDetailsDto

    @state()
    private allRoles: RoleDto[] = []

    // profile form (copy of user, edited locally until saved)
    @state()
    private form = { userName: "", email: "", emailConfirmed: false, fullName: "", phoneNumber: "", lockoutEnabled: true }

    @state()
    private passwordLink?: PasswordLinkDto

    @state()
    private lockChoice: LockChoice = "forever"

    connectedCallback(): void {
        super.connectedCallback()
        this.load()
    }

    private async load() {
        const result = await this.run(async () => {
            const [user, roles] = await Promise.all([users.getUser(this.userId), rolesApi.getRoles()])
            return { user, roles }
        })
        if (result) {
            this.allRoles = result.roles
            this.setUser(result.user, false)
        }
    }

    private setUser(user: UserDetailsDto, changed = true) {
        this.user = user
        this.form = {
            userName: user.userName,
            email: user.email ?? "",
            emailConfirmed: user.emailConfirmed,
            fullName: user.fullName ?? "",
            phoneNumber: user.phoneNumber ?? "",
            lockoutEnabled: user.lockoutEnabled,
        }
        if (changed) this.dispatchEvent(new CustomEvent("user-changed"))
    }

    /** Run an API call that returns the updated user */
    private async act(action: () => Promise<UserDetailsDto>, success: string) {
        const result = await this.run(action, success)
        if (result) this.setUser(result)
    }

    private get profileDirty() {
        const u = this.user!
        const f = this.form
        return f.userName !== u.userName || f.email !== (u.email ?? "") || f.emailConfirmed !== u.emailConfirmed
            || f.fullName !== (u.fullName ?? "") || f.phoneNumber !== (u.phoneNumber ?? "") || f.lockoutEnabled !== u.lockoutEnabled
    }

    private saveProfile = () => this.act(() => users.updateUser(this.userId, {
        userName: this.form.userName,
        email: this.form.email || null,
        emailConfirmed: this.form.emailConfirmed,
        fullName: this.form.fullName || null,
        phoneNumber: this.form.phoneNumber || null,
        lockoutEnabled: this.form.lockoutEnabled,
    }), "Profile saved.")

    private toggleRole(role: string, on: boolean) {
        return this.act(
            () => on ? users.addRole(this.userId, role) : users.removeRole(this.userId, role),
            on ? `Added to ${role}.` : `Removed from ${role}.`)
    }

    private async claimsChanged(items: KeyValue[]) {
        const current = this.user!.claims
        const same = (a: { type: string, value: string }, b: KeyValue) => a.type === b.key && a.value === b.value
        const added = items.filter(i => !current.some(c => same(c, i)))
        const removed = current.filter(c => !items.some(i => same(c, i)))
        for (const a of added) await this.act(() => users.addClaim(this.userId, { type: a.key, value: a.value }), "Claim added.")
        for (const r of removed) await this.act(() => users.removeClaim(this.userId, r), "Claim removed.")
        if (this.error) this.requestUpdate() // re-render editor from server state
    }

    private async createPasswordLink() {
        const link = await this.run(() => users.createPasswordLink(this.userId))
        if (link) this.passwordLink = link
    }

    private lock() {
        const until = this.lockChoice === "forever"
            ? null
            : new Date(Date.now() + lockDurations[this.lockChoice]).toISOString()
        return this.act(() => users.lockUser(this.userId, until), "User locked and signed out.")
    }

    private async resetTwoFactor() {
        if (await confirmDialog({
            title: "Reset two-factor",
            message: "Disables two-factor authentication and resets the authenticator key. The user can set it up again after signing in.",
            confirmLabel: "Reset 2FA", danger: true,
        }))
            await this.act(() => users.resetTwoFactor(this.userId), "Two-factor authentication reset.")
    }

    private async signOutEverywhere() {
        if (await confirmDialog({
            title: "Sign out everywhere",
            message: "Changes the user's security stamp. ID server sessions end at the next validation (within 30 minutes); tokens already issued to apps stay valid until they expire.",
            confirmLabel: "Sign out everywhere",
        }))
            await this.act(() => users.signOutEverywhere(this.userId), "Security stamp updated.")
    }

    private async removeLogin(login: UserLoginDto) {
        const name = login.providerDisplayName ?? login.loginProvider
        if (await confirmDialog({
            title: "Remove external login",
            message: `Unlink ${name} from this user? They can no longer sign in with it (unless it's linked again).`,
            confirmLabel: "Remove", danger: true,
        }))
            await this.act(() => users.removeLogin(this.userId, login.loginProvider, login.providerKey), `${name} login removed.`)
    }

    private async removePasskey(passkey: PasskeyDto) {
        if (await confirmDialog({
            title: "Remove passkey",
            message: `Remove the passkey "${passkey.name ?? "unnamed"}"?`,
            confirmLabel: "Remove", danger: true,
        }))
            await this.act(() => users.removePasskey(this.userId, passkey.credentialId), "Passkey removed.")
    }

    private async deleteUser() {
        const u = this.user!
        if (!await confirmDialog({
            title: "Delete user",
            message: `Permanently delete ${u.userName}? Their profile, roles, claims, external logins and passkeys are removed. This can't be undone.`,
            confirmLabel: "Delete user", danger: true, requireText: u.userName,
        }))
            return
        const ok = await this.run(async () => { await users.deleteUser(this.userId); return true })
        if (ok) {
            this.dispatchEvent(new CustomEvent("user-deleted"))
            this.dispatchEvent(new CustomEvent("close-dialog"))
        }
    }

    // ---- render ----

    render() {
        if (!this.user)
            return html`<error-viewer .error=${this.error}></error-viewer>${this.busy ? html`<wa-spinner></wa-spinner>` : nothing}`

        return html`
            <div class="stack">
                <error-viewer .error=${this.error}></error-viewer>
                ${this.notice ? html`<div class="notice">${this.notice}</div>` : nothing}
                <wa-tab-group>
                    <wa-tab panel="profile">Profile</wa-tab>
                    <wa-tab panel="roles">Roles (${this.user.roles.length})</wa-tab>
                    <wa-tab panel="claims">Claims (${this.user.claims.length})</wa-tab>
                    <wa-tab panel="security">Security</wa-tab>
                    <wa-tab panel="danger">Delete</wa-tab>

                    <wa-tab-panel name="profile">${this.renderProfile()}</wa-tab-panel>
                    <wa-tab-panel name="roles">${this.renderRoles()}</wa-tab-panel>
                    <wa-tab-panel name="claims">${this.renderClaims()}</wa-tab-panel>
                    <wa-tab-panel name="security">${this.renderSecurity()}</wa-tab-panel>
                    <wa-tab-panel name="danger">${this.renderDanger()}</wa-tab-panel>
                </wa-tab-group>
            </div>
        `
    }

    private renderProfile() {
        const f = this.form
        const set = (patch: Partial<typeof f>) => (this.form = { ...f, ...patch })
        return html`
            <div class="stack">
                <div class="grid">
                    <wa-input label="User name" required .value=${f.userName}
                        @input=${(e: Event) => set({ userName: valueOf(e) })}></wa-input>
                    <wa-input label="Full name" .value=${f.fullName}
                        @input=${(e: Event) => set({ fullName: valueOf(e) })}></wa-input>
                    <wa-input label="Email" type="email" .value=${f.email}
                        @input=${(e: Event) => set({ email: valueOf(e) })}></wa-input>
                    <wa-input label="Phone" .value=${f.phoneNumber}
                        @input=${(e: Event) => set({ phoneNumber: valueOf(e) })}></wa-input>
                </div>
                <wa-checkbox ?checked=${f.emailConfirmed} .checked=${f.emailConfirmed} ?disabled=${!f.email}
                    @change=${(e: Event) => set({ emailConfirmed: checkedOf(e) })}>Email confirmed</wa-checkbox>
                <wa-checkbox ?checked=${f.lockoutEnabled} .checked=${f.lockoutEnabled}
                    @change=${(e: Event) => set({ lockoutEnabled: checkedOf(e) })}
                    hint="Lock the account temporarily after repeated failed password attempts">Lockout on failed sign-in attempts</wa-checkbox>
                <p class="help mono">id: ${this.user!.id}</p>
                <div class="actions">
                    <wa-button appearance="outlined" ?disabled=${!this.profileDirty} @click=${() => this.setUser(this.user!, false)}>Revert</wa-button>
                    <wa-button variant="brand" ?loading=${this.busy} ?disabled=${!this.profileDirty} @click=${this.saveProfile}>Save profile</wa-button>
                </div>
            </div>
        `
    }

    private renderRoles() {
        const assigned = new Set(this.user!.roles)
        return html`
            <div class="stack">
                <p class="help">Roles are sent as <code>role</code> claims to apps requesting the <code>roles</code> scope.
                    Changes apply to tokens issued after the user next signs in. <a href="/Roles">Manage roles</a></p>
                ${this.allRoles.length === 0
                    ? html`<p class="muted">No roles exist yet.</p>`
                    : this.allRoles.map(r => html`
                        <wa-checkbox ?checked=${assigned.has(r.name)} .checked=${assigned.has(r.name)} ?disabled=${this.busy}
                            @change=${(e: Event) => this.toggleRole(r.name, checkedOf(e))}>
                            ${r.name}${r.isSystem ? html` <span class="muted">(system)</span>` : nothing}
                        </wa-checkbox>`)}
            </div>
        `
    }

    private renderClaims() {
        const items = this.user!.claims.map(c => ({ key: c.type, value: c.value }))
        return html`
            <key-value-editor
                hint="Extra user claims (AspNetUserClaims). They're included in tokens when a requested scope lists the claim type."
                keyLabel="Type" valueLabel="Value" allowDuplicateKeys
                .items=${items}
                @change=${(e: CustomEvent<KeyValue[]>) => this.claimsChanged(e.detail)}
            ></key-value-editor>
        `
    }

    private renderSecurity() {
        const u = this.user!
        return html`
            <div class="section">
                <h4>Password</h4>
                <p class="help">${u.hasPassword ? "The user has a password." : "The user has no password (external login or passkey only)."}
                    Create a one-time link the user can open to ${u.hasPassword ? "reset" : "set"} it.</p>
                ${this.passwordLink
                    ? html`<one-time-value heading="Set-password link" value=${this.passwordLink.url}
                        expiresAt=${this.passwordLink.expiresAt}
                        message="Send this to the user. They enter their email and choose a new password. The link stops working if you change their user name or email, lock them, or sign them out everywhere."></one-time-value>`
                    : nothing}
                <div><wa-button ?disabled=${!u.email} @click=${this.createPasswordLink}>
                    <wa-icon slot="start" name="key"></wa-icon>Create set-password link
                </wa-button></div>
                ${!u.email ? html`<p class="help">Add an email address first (the reset page asks for it).</p>` : nothing}
            </div>

            <div class="section">
                <h4>Lockout</h4>
                ${u.isLockedOut
                    ? html`
                        <p>Locked ${users.isIndefinite(u.lockoutEnd) ? "until unlocked" : `until ${formatDateTime(u.lockoutEnd!)}`}.</p>
                        <div><wa-button variant="success" @click=${() => this.act(() => users.unlockUser(this.userId), "User unlocked.")}>
                            <wa-icon slot="start" name="lock-open"></wa-icon>Unlock
                        </wa-button></div>`
                    : html`
                        <p class="help">Locking blocks sign-in and ends the user's ID server sessions.
                            ${u.accessFailedCount ? `Failed attempts: ${u.accessFailedCount}.` : ""}</p>
                        <div class="toolbar">
                            <wa-select label="Lock for" .value=${this.lockChoice}
                                @change=${(e: Event) => (this.lockChoice = valueOf(e) as LockChoice)}>
                                <wa-option value="forever">Until unlocked</wa-option>
                                <wa-option value="1h">1 hour</wa-option>
                                <wa-option value="1d">1 day</wa-option>
                                <wa-option value="7d">7 days</wa-option>
                                <wa-option value="30d">30 days</wa-option>
                            </wa-select>
                            <wa-button variant="danger" @click=${this.lock}>
                                <wa-icon slot="start" name="lock"></wa-icon>Lock
                            </wa-button>
                        </div>`}
            </div>

            <div class="section">
                <h4>Two-factor authentication</h4>
                <p class="help">${u.twoFactorEnabled ? "Enabled." : "Not enabled."}</p>
                ${u.twoFactorEnabled
                    ? html`<div><wa-button variant="danger" appearance="outlined" @click=${this.resetTwoFactor}>Reset 2FA</wa-button></div>`
                    : nothing}
            </div>

            <div class="section">
                <h4>External logins</h4>
                ${u.logins.length === 0
                    ? html`<p class="help">None.</p>`
                    : html`<table><tbody>
                        ${u.logins.map(l => html`
                            <tr>
                                <td>${l.providerDisplayName ?? l.loginProvider}</td>
                                <td class="mono muted hide-sm">${l.providerKey}</td>
                                <td class="right"><wa-button size="small" variant="danger" appearance="plain"
                                    @click=${() => this.removeLogin(l)}><wa-icon name="trash" label="Remove"></wa-icon></wa-button></td>
                            </tr>`)}
                      </tbody></table>`}
            </div>

            <div class="section">
                <h4>Passkeys</h4>
                ${u.passkeys.length === 0
                    ? html`<p class="help">None.</p>`
                    : html`<table><tbody>
                        ${u.passkeys.map(p => html`
                            <tr>
                                <td>${p.name ?? html`<span class="muted">unnamed</span>`}</td>
                                <td class="muted">${formatDateTime(p.createdAt)}</td>
                                <td class="right"><wa-button size="small" variant="danger" appearance="plain"
                                    @click=${() => this.removePasskey(p)}><wa-icon name="trash" label="Remove"></wa-icon></wa-button></td>
                            </tr>`)}
                      </tbody></table>`}
            </div>

            <div class="section">
                <h4>Sessions</h4>
                <p class="help">Force the user to sign in again on the ID server.</p>
                <div><wa-button appearance="outlined" @click=${this.signOutEverywhere}>
                    <wa-icon slot="start" name="arrow-right-from-bracket"></wa-icon>Sign out everywhere
                </wa-button></div>
            </div>
        `
    }

    private renderDanger() {
        return html`
            <div class="stack">
                <wa-callout variant="danger">
                    <wa-icon slot="icon" name="triangle-exclamation"></wa-icon>
                    Deleting a user is permanent. To block access temporarily, lock the account under Security instead.
                    You can't delete yourself.
                </wa-callout>
                <div><wa-button variant="danger" @click=${this.deleteUser}>
                    <wa-icon slot="start" name="trash"></wa-icon>Delete user
                </wa-button></div>
            </div>
        `
    }
}
