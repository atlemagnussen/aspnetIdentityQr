import type { ClientSummaryDto } from "@db/api"
import { html, nothing } from "lit"
import { customElement, state } from "lit/decorators.js"
import { AdminElement, valueOf } from "@db/client/components/adminElement.js"
import { adminStyles } from "@db/client/components/sharedStyles.js"
import { openDialog } from "@db/client/components/appDialog.js"
import { formatDateTime } from "@db/client/components/dateTimeFormattingLocale.js"
import * as clients from "@db/client/views/configuration/clientsService.js"
import { ClientEditor } from "@db/client/views/configuration/clientEditor.js"

const shortGrant: Record<string, string> = {
    authorization_code: "code",
    client_credentials: "client credentials",
    "urn:ietf:params:oauth:grant-type:device_code": "device",
    "urn:openid:params:grant-type:ciba": "ciba",
}

@customElement("clients-list")
export class ClientsList extends AdminElement {
    static styles = adminStyles

    @state()
    private clients?: ClientSummaryDto[]

    @state()
    private filter = ""

    connectedCallback(): void {
        super.connectedCallback()
        this.load()
    }

    private load = async () => {
        const result = await this.run(() => clients.getClients())
        if (result) this.clients = result
    }

    private open(clientId?: string) {
        const editor = new ClientEditor()
        editor.clientId = clientId ?? ""
        const handle = openDialog({ title: clientId ? `Client: ${clientId}` : "New client", content: editor, size: "wide" })
        editor.addEventListener("client-changed", (e: Event) => {
            const detail = (e as CustomEvent<{ created?: boolean, clientId: string }>).detail
            if (detail.created) handle.setTitle(`Client: ${detail.clientId}`)
            this.load()
        })
        // secret count in the list
        editor.addEventListener("secrets-changed", this.load)
    }

    private get filtered() {
        const q = this.filter.trim().toLowerCase()
        const all = this.clients ?? []
        if (!q) return all
        return all.filter(c => [c.clientId, c.clientName, ...c.allowedGrantTypes].some(v => v?.toLowerCase().includes(q)))
    }

    render() {
        const list = this.filtered
        return html`
            <div class="header">
                <h2>Clients</h2>
                <wa-button variant="brand" @click=${() => this.open()}>
                    <wa-icon slot="start" name="plus"></wa-icon>New client
                </wa-button>
            </div>
            <div class="toolbar">
                <wa-input class="grow" placeholder="Search client id, name, grant type" with-clear
                    .value=${this.filter} @input=${(e: Event) => (this.filter = valueOf(e))}>
                    <wa-icon slot="start" name="magnifying-glass"></wa-icon>
                </wa-input>
                <wa-button appearance="outlined" ?loading=${this.busy} @click=${this.load}>
                    <wa-icon slot="start" name="rotate"></wa-icon>Refresh
                </wa-button>
            </div>
            <error-viewer .error=${this.error}></error-viewer>
            ${!this.clients
                ? (this.busy ? html`<div class="empty"><wa-spinner></wa-spinner></div>` : nothing)
                : list.length === 0
                    ? html`<div class="empty">${this.filter ? "No clients match the search." : "No clients yet."}</div>`
                    : html`
                <table>
                    <thead>
                        <tr>
                            <th>Client</th>
                            <th>Grant types</th>
                            <th class="hide-sm">Secrets</th>
                            <th class="hide-sm">Redirects / scopes</th>
                            <th class="hide-sm">Last used</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${list.map(c => html`
                            <tr class="clickable" @click=${() => this.open(c.clientId)}>
                                <td>
                                    <div class="mono">${c.clientId}</div>
                                    ${c.clientName ? html`<div class="muted">${c.clientName}</div>` : nothing}
                                </td>
                                <td><div class="tags">${c.allowedGrantTypes.map(g => html`<wa-tag size="small">${shortGrant[g] ?? g}</wa-tag>`)}</div></td>
                                <td class="hide-sm">${c.requireClientSecret
                                    ? (c.secretCount ? c.secretCount : html`<wa-badge variant="warning">none</wa-badge>`)
                                    : html`<span class="muted">public</span>`}</td>
                                <td class="hide-sm muted">${c.redirectUriCount} / ${c.allowedScopeCount}</td>
                                <td class="hide-sm muted">${c.lastAccessed ? formatDateTime(c.lastAccessed) : "-"}</td>
                                <td><div class="tags">
                                    ${c.enabled ? html`<wa-badge variant="success">enabled</wa-badge>` : html`<wa-badge variant="neutral">disabled</wa-badge>`}
                                    ${c.isProtected ? html`<wa-badge variant="brand" title="This admin app signs in with this client">this app</wa-badge>` : nothing}
                                </div></td>
                            </tr>`)}
                    </tbody>
                </table>`}
        `
    }
}
