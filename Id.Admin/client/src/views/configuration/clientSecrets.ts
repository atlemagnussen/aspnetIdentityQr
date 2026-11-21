import type { ClientSecretCreatedDto, ClientSecretDto } from "@db/api"
import { html, nothing } from "lit"
import { customElement, property, state } from "lit/decorators.js"
import { AdminElement, valueOf } from "@db/client/components/adminElement.js"
import { adminStyles } from "@db/client/components/sharedStyles.js"
import { confirmDialog } from "@db/client/components/appDialog.js"
import { formatDateOnly, formatDateTime } from "@db/client/components/dateTimeFormattingLocale.js"
import "@db/client/components/oneTimeValue.js"
import * as clients from "@db/client/views/configuration/clientsService.js"

/** Client secrets are saved immediately (not part of the client form). Values are generated server-side and shown once. */
@customElement("client-secrets")
export class ClientSecrets extends AdminElement {
    static styles = adminStyles

    @property()
    clientId = ""

    @property({ type: Boolean })
    requireClientSecret = true

    @state()
    private secrets?: ClientSecretDto[]

    @state()
    private description = ""

    @state()
    private expiration = ""

    @state()
    private created?: ClientSecretCreatedDto

    connectedCallback(): void {
        super.connectedCallback()
        this.load()
    }

    private async load() {
        const result = await this.run(() => clients.getSecrets(this.clientId))
        if (result) this.secrets = result
    }

    private changed() {
        this.dispatchEvent(new CustomEvent("secrets-changed", { bubbles: true, composed: true }))
    }

    private async create() {
        const result = await this.run(() => clients.createSecret(this.clientId, {
            description: this.description.trim() || null,
            // end of the chosen day, UTC
            expiration: this.expiration ? new Date(`${this.expiration}T23:59:59Z`).toISOString() : null,
        }))
        if (result) {
            this.created = result
            this.description = ""
            this.expiration = ""
            await this.load()
            this.changed()
        }
    }

    private async removeItem(secret: ClientSecretDto) {
        if (!await confirmDialog({
            title: "Delete secret",
            message: `Delete the secret ${secret.description ? `"${secret.description}"` : `#${secret.id}`}? Apps using it can no longer authenticate as ${this.clientId}.`,
            confirmLabel: "Delete secret", danger: true,
        }))
            return
        if (await this.run(async () => { await clients.deleteSecret(this.clientId, secret.id); return true }, "Secret deleted.")) {
            if (this.created?.secret.id === secret.id) this.created = undefined
            await this.load()
            this.changed()
        }
    }

    private isExpired(s: ClientSecretDto) {
        return !!s.expiration && new Date(s.expiration) < new Date()
    }

    render() {
        const active = (this.secrets ?? []).filter(s => !this.isExpired(s))
        return html`
            <div class="stack">
                <p class="help">Secrets are stored as SHA-256 hashes, so a value can't be shown again after it's created.
                    To rotate: create a new secret, update the app, then delete the old one. Changes here are saved immediately.</p>
                ${this.requireClientSecret && this.secrets && active.length === 0
                    ? html`<wa-callout variant="warning"><wa-icon slot="icon" name="triangle-exclamation"></wa-icon>
                        This client requires a secret but has no active one, so it can't authenticate.</wa-callout>`
                    : nothing}
                ${!this.requireClientSecret
                    ? html`<p class="help">This client doesn't require a secret (public client). Secrets are optional.</p>`
                    : nothing}
                <error-viewer .error=${this.error}></error-viewer>
                ${this.notice ? html`<div class="notice">${this.notice}</div>` : nothing}
                ${this.created
                    ? html`<one-time-value heading="New client secret" value=${this.created.value}
                        message="Copy it now and store it in the app's configuration. It won't be shown again."></one-time-value>`
                    : nothing}

                ${this.secrets?.length
                    ? html`
                        <table>
                            <thead><tr><th>Description</th><th>Created</th><th>Expires</th><th></th></tr></thead>
                            <tbody>
                                ${this.secrets.map(s => html`
                                    <tr>
                                        <td>${s.description ?? html`<span class="muted">#${s.id}</span>`}
                                            ${s.type !== "SharedSecret" ? html`<wa-tag size="small">${s.type}</wa-tag>` : nothing}</td>
                                        <td class="muted">${formatDateTime(s.created)}</td>
                                        <td>${s.expiration
                                            ? html`${formatDateOnly(s.expiration)} ${this.isExpired(s) ? html`<wa-badge variant="danger">expired</wa-badge>` : nothing}`
                                            : html`<span class="muted">never</span>`}</td>
                                        <td class="right"><wa-button size="small" appearance="plain" variant="danger"
                                            @click=${() => this.removeItem(s)}><wa-icon name="trash" label="Delete"></wa-icon></wa-button></td>
                                    </tr>`)}
                            </tbody>
                        </table>`
                    : this.secrets ? html`<p class="muted">No secrets.</p>` : nothing}

                <div class="toolbar">
                    <wa-input class="grow" label="Description" placeholder="e.g. production 2026" .value=${this.description}
                        @input=${(e: Event) => (this.description = valueOf(e))}></wa-input>
                    <wa-input label="Expires (optional)" type="date" .value=${this.expiration}
                        @input=${(e: Event) => (this.expiration = valueOf(e))}></wa-input>
                    <wa-button variant="brand" ?loading=${this.busy} @click=${this.create}>
                        <wa-icon slot="start" name="key"></wa-icon>Generate secret
                    </wa-button>
                </div>
            </div>
        `
    }
}
