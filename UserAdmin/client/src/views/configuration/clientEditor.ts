import { AccessTokenType, TokenExpiration, TokenUsage, type ClientDto, type ScopeDto } from "@db/api"
import { css, html, nothing } from "lit"
import { customElement, property, state } from "lit/decorators.js"
import { AdminElement, checkedOf, valueOf } from "@db/client/components/adminElement.js"
import { adminStyles } from "@db/client/components/sharedStyles.js"
import { confirmDialog } from "@db/client/components/appDialog.js"
import { KeyValue } from "@db/client/components/listEditors.js"
import { formatDateTime } from "@db/client/components/dateTimeFormattingLocale.js"
import "@db/client/components/listEditors.js"
import "@db/client/views/configuration/clientSecrets.js"
import * as clients from "@db/client/views/configuration/clientsService.js"
import { clientTemplates } from "@db/client/views/configuration/clientTemplates.js"

type BoolKey = { [K in keyof ClientDto]-?: ClientDto[K] extends boolean ? K : never }[keyof ClientDto]
type NumKey = { [K in keyof ClientDto]-?: NonNullable<ClientDto[K]> extends number ? K : never }[keyof ClientDto]
type TextKey = { [K in keyof ClientDto]-?: NonNullable<ClientDto[K]> extends string ? K : never }[keyof ClientDto]
type ListKey = { [K in keyof ClientDto]-?: ClientDto[K] extends string[] ? K : never }[keyof ClientDto]

/**
 * Create/edit a client. The whole client is edited locally and saved with one PUT (or POST when new);
 * secrets are the exception and are saved immediately.
 */
@customElement("client-editor")
export class ClientEditor extends AdminElement {
    static styles = [adminStyles, css`
        :host {
            display: flex;
            flex-direction: column;
        }
        .savebar {
            margin-top: auto;
            position: sticky;
            bottom: calc(-1 * var(--wa-space-m));
            display: flex;
            align-items: center;
            justify-content: flex-end;
            gap: var(--wa-space-s);
            padding: var(--wa-space-s) 0;
            background: var(--wa-color-surface-default);
            border-top: var(--wa-border-width-s) solid var(--wa-color-surface-border);
            z-index: 1;
        }
        .savebar .dirty {
            margin-right: auto;
            color: var(--wa-color-warning-on-quiet);
            font-size: var(--wa-font-size-s);
        }
        .templates {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
            gap: var(--wa-space-xs);
        }
        .template {
            text-align: left;
            padding: var(--wa-space-s);
            border: var(--wa-border-width-s) solid var(--wa-color-surface-border);
            border-radius: var(--wa-border-radius-m);
            background: var(--wa-color-surface-lowered);
            color: inherit;
            font: inherit;
            cursor: pointer;
        }
        .template[aria-pressed="true"] {
            border-color: var(--wa-color-brand-border-loud);
            background: var(--wa-color-brand-fill-quiet);
        }
        .template strong {
            display: block;
        }
        .scopes {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr));
            gap: var(--wa-space-xs);
        }
    `]

    /** Existing client id to edit; empty = create */
    @property()
    clientId = ""

    @state()
    private original?: ClientDto

    @state()
    private draft?: ClientDto

    @state()
    private scopes: ScopeDto[] = []

    @state()
    private template = "web"

    private get isNew() {
        return !this.original
    }

    connectedCallback(): void {
        super.connectedCallback()
        this.load()
    }

    private async load() {
        const result = await this.run(async () => {
            const [scopes, client] = await Promise.all([
                clients.getScopes(),
                this.clientId ? clients.getClient(this.clientId) : Promise.resolve(undefined),
            ])
            return { scopes, client }
        })
        if (!result) return
        this.scopes = result.scopes
        if (result.client) {
            this.original = result.client
            this.draft = structuredClone(result.client)
        } else {
            this.draft = clientTemplates.find(t => t.key === this.template)!.create("")
        }
    }

    private applyTemplate(key: string) {
        this.template = key
        const tpl = clientTemplates.find(t => t.key === key)!
        const keep = this.draft!
        this.draft = { ...tpl.create(keep.clientId), clientName: keep.clientName, description: keep.description }
    }

    private get dirty() {
        if (this.isNew) return true
        return JSON.stringify(this.draft) !== JSON.stringify(this.original)
    }

    private set<K extends keyof ClientDto>(key: K, value: ClientDto[K]) {
        this.draft = { ...this.draft!, [key]: value }
        this.notice = ""
    }

    private async save() {
        const draft = this.draft!
        const result = await this.run(
            () => this.isNew ? clients.createClient(draft) : clients.updateClient(this.original!.clientId, draft),
            this.isNew ? `Client ${draft.clientId} created.` : "Saved. The ID server uses the new settings immediately.")
        if (!result) return
        const wasNew = this.isNew
        this.original = result
        this.draft = structuredClone(result)
        this.clientId = result.clientId
        this.dispatchEvent(new CustomEvent("client-changed", { detail: { created: wasNew, clientId: result.clientId } }))
        if (wasNew && result.requireClientSecret)
            this.notice = `Client ${result.clientId} created. Generate a secret under Secrets so it can authenticate.`
    }

    private revert() {
        this.draft = structuredClone(this.original!)
        this.error = undefined
    }

    private async removeItem() {
        const id = this.original!.clientId
        if (!await confirmDialog({
            title: "Delete client",
            message: `Permanently delete ${id}, including its secrets, URLs and scopes? Apps using it stop working. To pause it instead, turn off "Enabled".`,
            confirmLabel: "Delete client", danger: true, requireText: id,
        }))
            return
        if (await this.run(async () => { await clients.deleteClient(id); return true })) {
            this.dispatchEvent(new CustomEvent("client-changed", { detail: { deleted: true, clientId: id } }))
            this.dispatchEvent(new CustomEvent("close-dialog"))
        }
    }

    // ---- field helpers ----

    private text(key: TextKey, label: string, hint = "", attrs: { placeholder?: string, readonly?: boolean, type?: string } = {}) {
        return html`<wa-input label=${label} hint=${hint} placeholder=${attrs.placeholder ?? ""} type=${attrs.type ?? "text"}
            ?readonly=${attrs.readonly} .value=${(this.draft![key] as string | null | undefined) ?? ""}
            @input=${(e: Event) => this.set(key, (valueOf(e) || null) as never)}></wa-input>`
    }

    private num(key: NumKey, label: string, hint = "", nullable = false) {
        const value = this.draft![key] as number | null | undefined
        return html`<wa-input type="number" min="0" label=${label}
            hint=${[hint, clients.humanSeconds(value)].filter(Boolean).join(" · ")}
            .value=${value == null ? "" : String(value)}
            @input=${(e: Event) => {
                const raw = valueOf(e)
                this.set(key, (raw === "" ? (nullable ? null : 0) : Number(raw)) as never)
            }}></wa-input>`
    }

    private check(key: BoolKey, label: string, hint = "") {
        return html`<wa-checkbox ?checked=${this.draft![key] as boolean} .checked=${this.draft![key] as boolean} hint=${hint}
            @change=${(e: Event) => this.set(key, checkedOf(e) as never)}>${label}</wa-checkbox>`
    }

    private list(key: ListKey, label: string, hint: string, placeholder: string,
        validate?: (v: string) => string | null, suggestions: string[] = []) {
        return html`<string-list-editor label=${label} hint=${hint} placeholder=${placeholder}
            .items=${this.draft![key] as string[]} .validate=${validate} .suggestions=${suggestions}
            @change=${(e: CustomEvent<string[]>) => this.set(key, e.detail as never)}></string-list-editor>`
    }

    // ---- render ----

    render() {
        const d = this.draft
        if (!d)
            return html`<error-viewer .error=${this.error}></error-viewer>${this.busy ? html`<wa-spinner></wa-spinner>` : nothing}`

        const readOnly = !!this.original?.nonEditable
        return html`
            ${this.original?.isProtected
                ? html`<wa-callout variant="warning"><wa-icon slot="icon" name="shield-halved"></wa-icon>
                    UserAdmin signs in with this client. It can't be disabled or deleted, and breaking its redirect URLs
                    or scopes would lock you out of this app.</wa-callout>`
                : nothing}
            ${readOnly ? html`<wa-callout variant="neutral">This client is marked non-editable.</wa-callout>` : nothing}

            <wa-tab-group>
                <wa-tab panel="general">General</wa-tab>
                <wa-tab panel="auth">Authentication</wa-tab>
                <wa-tab panel="urls">URLs</wa-tab>
                <wa-tab panel="scopes">Scopes (${d.allowedScopes.length})</wa-tab>
                <wa-tab panel="tokens">Tokens</wa-tab>
                ${this.isNew ? nothing : html`<wa-tab panel="secrets">Secrets</wa-tab>`}
                <wa-tab panel="claims">Claims & properties</wa-tab>
                ${this.isNew || this.original?.isProtected ? nothing : html`<wa-tab panel="danger">Delete</wa-tab>`}

                <wa-tab-panel name="general">${this.renderGeneral()}</wa-tab-panel>
                <wa-tab-panel name="auth">${this.renderAuth()}</wa-tab-panel>
                <wa-tab-panel name="urls">${this.renderUrls()}</wa-tab-panel>
                <wa-tab-panel name="scopes">${this.renderScopes()}</wa-tab-panel>
                <wa-tab-panel name="tokens">${this.renderTokens()}</wa-tab-panel>
                ${this.isNew ? nothing : html`<wa-tab-panel name="secrets">
                    <client-secrets clientId=${this.original!.clientId} ?requireClientSecret=${this.original!.requireClientSecret}></client-secrets>
                </wa-tab-panel>`}
                <wa-tab-panel name="claims">${this.renderClaims()}</wa-tab-panel>
                ${this.isNew || this.original?.isProtected ? nothing : html`<wa-tab-panel name="danger">${this.renderDanger()}</wa-tab-panel>`}
            </wa-tab-group>

            <div class="savebar">
                ${!this.isNew && this.dirty ? html`<span class="dirty">Unsaved changes</span>` : nothing}
                <div class="stack" style="flex: 1 1 100%">
                    <error-viewer .error=${this.error}></error-viewer>
                    ${this.notice ? html`<div class="notice">${this.notice}</div>` : nothing}
                </div>
                ${this.isNew ? nothing : html`<wa-button appearance="outlined" ?disabled=${!this.dirty} @click=${this.revert}>Revert</wa-button>`}
                <wa-button variant="brand" ?loading=${this.busy} ?disabled=${!this.dirty || readOnly || !d.clientId?.trim()}
                    @click=${this.save}>${this.isNew ? "Create client" : "Save"}</wa-button>
            </div>
        `
    }

    private renderGeneral() {
        const o = this.original
        return html`
            <div class="section">
                ${this.isNew
                    ? html`
                        <span class="help">Start from</span>
                        <div class="templates">
                            ${clientTemplates.map(t => html`
                                <button class="template" aria-pressed=${this.template === t.key ? "true" : "false"}
                                    @click=${() => this.applyTemplate(t.key)}>
                                    <strong>${t.label}</strong><span class="muted">${t.description}</span>
                                </button>`)}
                        </div>`
                    : nothing}
                <div class="grid">
                    ${this.isNew
                        ? this.text("clientId", "Client id", "Used by apps in their OIDC config. Can't be changed later.", { placeholder: "my-app" })
                        : html`<wa-input label="Client id" readonly .value=${o!.clientId}></wa-input>`}
                    ${this.text("clientName", "Display name", "Shown on consent screens")}
                    ${this.text("clientUri", "Client URI", "Homepage of the app", { placeholder: "https://app.example.com" })}
                    ${this.text("logoUri", "Logo URI")}
                </div>
                <wa-textarea label="Description" rows="2" .value=${this.draft!.description ?? ""}
                    @input=${(e: Event) => this.set("description", valueOf(e) || null)}></wa-textarea>
                <wa-switch ?checked=${this.draft!.enabled} .checked=${this.draft!.enabled} ?disabled=${!!o?.isProtected}
                    @change=${(e: Event) => this.set("enabled", checkedOf(e))}
                    hint="Disabled clients can't get tokens. Use this to pause an app without deleting it">Enabled</wa-switch>
            </div>
            ${o
                ? html`<div class="section">
                    <p class="help">
                        Created ${o.created ? formatDateTime(o.created) : "-"}
                        · Updated ${o.updated ? formatDateTime(o.updated) : "never"}
                        · Last used ${o.lastAccessed ? formatDateTime(o.lastAccessed) : "unknown"}
                    </p>
                  </div>`
                : nothing}
        `
    }

    private renderAuth() {
        const d = this.draft!
        const known = clients.knownGrantTypes.map(g => g.value)
        const custom = d.allowedGrantTypes.filter(g => !known.includes(g))
        const toggleGrant = (grant: string, on: boolean) =>
            this.set("allowedGrantTypes", on ? [...d.allowedGrantTypes, grant] : d.allowedGrantTypes.filter(g => g !== grant))
        const usesCode = d.allowedGrantTypes.includes("authorization_code")
        return html`
            <div class="section">
                <h4>Grant types</h4>
                <p class="help">How the app gets tokens. Some combinations aren't allowed (e.g. implicit + authorization code); the server checks on save.</p>
                ${clients.knownGrantTypes.map(g => html`
                    <wa-checkbox ?checked=${d.allowedGrantTypes.includes(g.value)} .checked=${d.allowedGrantTypes.includes(g.value)}
                        @change=${(e: Event) => toggleGrant(g.value, checkedOf(e))}>
                        ${g.label}${g.legacy ? html` <wa-badge variant="neutral">legacy</wa-badge>` : nothing}
                        <span class="mono muted">${g.value}</span>
                    </wa-checkbox>`)}
                <string-list-editor label="Custom grant types" placeholder="urn:ietf:params:oauth:grant-type:token-exchange"
                    .items=${custom}
                    @change=${(e: CustomEvent<string[]>) => this.set("allowedGrantTypes",
                        [...d.allowedGrantTypes.filter(g => known.includes(g)), ...e.detail])}></string-list-editor>
            </div>
            <div class="section">
                <h4>Client authentication</h4>
                ${this.check("requireClientSecret", "Require client secret",
                    "Confidential clients (server-side apps, services) must authenticate. Turn off for browser/native apps that can't keep a secret")}
                ${this.check("requirePkce", "Require PKCE", usesCode ? "Recommended for every authorization code client" : "")}
                ${this.check("allowPlainTextPkce", "Allow plain-text PKCE", "Not recommended; S256 should always be used")}
                ${this.check("requireDPoP", "Require DPoP", "Sender-constrained tokens")}
                ${this.check("requirePushedAuthorization", "Require pushed authorization requests (PAR)")}
                ${this.check("requireRequestObject", "Require request object (JAR)")}
            </div>
            <div class="section">
                <h4>Sign-in</h4>
                ${this.check("allowOfflineAccess", "Allow offline access (refresh tokens)", "Lets the app request the offline_access scope to get refresh tokens")}
                ${this.check("allowAccessTokensViaBrowser", "Allow access tokens via browser", "Only needed for implicit/hybrid flows that return tokens from the authorize endpoint")}
                ${this.check("enableLocalLogin", "Enable local login", "Allow username/password and passkeys on the ID server. Turn off to force external providers")}
                ${this.list("identityProviderRestrictions", "Restrict to identity providers",
                    "Leave empty to allow all. Otherwise only these external providers are offered", "Scheme name", undefined, ["Google", "Microsoft"])}
            </div>
            <div class="section">
                <h4>Consent</h4>
                ${this.check("requireConsent", "Require consent", "Show a consent screen before issuing tokens. Usually off for first-party apps")}
                ${this.check("allowRememberConsent", "Allow remembering consent")}
                ${this.num("consentLifetime", "Consent lifetime (seconds)", "Empty = forever", true)}
            </div>
        `
    }

    private renderUrls() {
        return html`
            <div class="section">
                ${this.list("redirectUris", "Redirect URIs",
                    "Where the ID server may send the user back after sign-in. Must match exactly (scheme, host, port, path)",
                    "https://app.example.com/signin-oidc", clients.validateAbsoluteUrl)}
            </div>
            <div class="section">
                ${this.list("postLogoutRedirectUris", "Post-logout redirect URIs",
                    "Where the user may be sent after signing out", "https://app.example.com/signout-callback-oidc", clients.validateAbsoluteUrl)}
            </div>
            <div class="section">
                ${this.list("allowedCorsOrigins", "Allowed CORS origins",
                    "Origins allowed to call the token/userinfo endpoints from a browser (needed for SPAs)", "https://app.example.com", clients.validateOrigin)}
            </div>
            <div class="section">
                <h4>Logout notifications</h4>
                <div class="grid">
                    ${this.text("frontChannelLogoutUri", "Front-channel logout URI", "Loaded in an iframe on sign-out")}
                    ${this.text("backChannelLogoutUri", "Back-channel logout URI", "Server-to-server sign-out notification")}
                </div>
                ${this.check("frontChannelLogoutSessionRequired", "Front-channel: include session id")}
                ${this.check("backChannelLogoutSessionRequired", "Back-channel: include session id")}
                ${this.text("initiateLoginUri", "Initiate login URI", "Third-party initiated login (rarely needed)")}
            </div>
        `
    }

    private renderScopes() {
        const d = this.draft!
        const selected = new Set(d.allowedScopes)
        const toggle = (name: string, on: boolean) =>
            this.set("allowedScopes", on ? [...d.allowedScopes, name] : d.allowedScopes.filter(s => s !== name))
        const known = new Set(this.scopes.map(s => s.name))
        const unknown = d.allowedScopes.filter(s => !known.has(s))
        const group = (kind: string, title: string, help: string) => {
            const items = this.scopes.filter(s => s.kind === kind)
            return html`
                <div class="section">
                    <h4>${title}</h4>
                    <p class="help">${help}</p>
                    ${items.length === 0 ? html`<p class="muted">None defined.</p>` : html`
                        <div class="scopes">
                            ${items.map(s => html`
                                <wa-checkbox ?checked=${selected.has(s.name)} .checked=${selected.has(s.name)} @change=${(e: Event) => toggle(s.name, checkedOf(e))}>
                                    <span class="mono">${s.name}</span>
                                    ${s.displayName && s.displayName !== s.name ? html`<span class="muted">${s.displayName}</span>` : nothing}
                                    ${!s.enabled ? html`<wa-badge variant="neutral">disabled</wa-badge>` : nothing}
                                </wa-checkbox>`)}
                        </div>`}
                </div>`
        }
        return html`
            ${group("identity", "Identity resources", "User identity data in id tokens / userinfo (openid is required for sign-in).")}
            ${group("api", "API scopes", "Access to APIs, carried in access tokens.")}
            ${unknown.length
                ? html`<div class="section">
                    <h4>Unknown scopes</h4>
                    <p class="help">Assigned but not defined on the ID server. They're ignored when tokens are issued.</p>
                    ${unknown.map(s => html`<wa-checkbox checked @change=${(e: Event) => toggle(s, checkedOf(e))}><span class="mono">${s}</span></wa-checkbox>`)}
                  </div>`
                : nothing}
            <p class="help">Refresh tokens are enabled with "Allow offline access" under Authentication, not here.</p>
        `
    }

    // wa-option values must be static strings: Lit sets the select's .value before bound option values,
    // and wa-select (3.2.1) then drops a value that matches no option.
    private renderTokens() {
        const d = this.draft!
        return html`
            <div class="section">
                <h4>Access token</h4>
                <div class="grid">
                    <wa-select label="Type" .value=${d.accessTokenType}
                        hint="JWT: self-contained. Reference: opaque, APIs must introspect"
                        @change=${(e: Event) => this.set("accessTokenType", valueOf(e) as AccessTokenType)}>
                        <wa-option value="Jwt">JWT</wa-option>
                        <wa-option value="Reference">Reference</wa-option>
                    </wa-select>
                    ${this.num("accessTokenLifetime", "Lifetime (seconds)")}
                </div>
                ${this.check("includeJwtId", "Include JWT id (jti)")}
            </div>
            <div class="section">
                <h4>Identity token & code</h4>
                <div class="grid">
                    ${this.num("identityTokenLifetime", "Identity token lifetime (seconds)")}
                    ${this.num("authorizationCodeLifetime", "Authorization code lifetime (seconds)")}
                </div>
                ${this.check("alwaysIncludeUserClaimsInIdToken", "Always include user claims in the identity token",
                    "Otherwise apps fetch them from the userinfo endpoint")}
                ${this.list("allowedIdentityTokenSigningAlgorithms", "Identity token signing algorithms",
                    "Empty = server default", "ES256", undefined, ["ES256", "RS256", "PS256"])}
            </div>
            <div class="section">
                <h4>Refresh token</h4>
                ${!d.allowOfflineAccess ? html`<p class="help">Refresh tokens are off for this client (enable "Allow offline access" under Authentication).</p>` : nothing}
                <div class="grid">
                    <wa-select label="Usage" .value=${d.refreshTokenUsage}
                        hint="One-time: rotated on every use (recommended)"
                        @change=${(e: Event) => this.set("refreshTokenUsage", valueOf(e) as TokenUsage)}>
                        <wa-option value="OneTimeOnly">One-time only</wa-option>
                        <wa-option value="ReUse">Reuse</wa-option>
                    </wa-select>
                    <wa-select label="Expiration" .value=${d.refreshTokenExpiration}
                        hint="Sliding: extended on use, up to the absolute lifetime"
                        @change=${(e: Event) => this.set("refreshTokenExpiration", valueOf(e) as TokenExpiration)}>
                        <wa-option value="Absolute">Absolute</wa-option>
                        <wa-option value="Sliding">Sliding</wa-option>
                    </wa-select>
                    ${this.num("absoluteRefreshTokenLifetime", "Absolute lifetime (seconds)", "0 = no limit (sliding only)")}
                    ${this.num("slidingRefreshTokenLifetime", "Sliding lifetime (seconds)")}
                </div>
                ${this.check("updateAccessTokenClaimsOnRefresh", "Update access token claims on refresh")}
            </div>
            <div class="section">
                <h4>Session</h4>
                <div class="grid">
                    ${this.num("userSsoLifetime", "User SSO lifetime (seconds)", "Max time since sign-in before re-authentication. Empty = session cookie lifetime", true)}
                </div>
                ${this.check("coordinateLifetimeWithUserSession", "Coordinate token lifetime with user session",
                    "Refresh tokens stop working when the user's server-side session ends")}
            </div>
        `
    }

    private renderClaims() {
        const d = this.draft!
        const toKv = (items: { type?: string, key?: string, value: string }[]) =>
            items.map(i => ({ key: (i.type ?? i.key)!, value: i.value }))
        return html`
            <div class="section">
                <key-value-editor label="Client claims" keyLabel="Type" valueLabel="Value" allowDuplicateKeys
                    hint="Added to access tokens for this client (prefixed with the client claims prefix)"
                    .items=${toKv(d.claims)}
                    @change=${(e: CustomEvent<KeyValue[]>) => this.set("claims", e.detail.map(i => ({ type: i.key, value: i.value })))}></key-value-editor>
                ${this.check("alwaysSendClientClaims", "Always send client claims", "Also for user-centric flows, not just client credentials")}
                <div class="grid">
                    ${this.text("clientClaimsPrefix", "Client claims prefix", "Empty = no prefix")}
                    ${this.text("pairWiseSubjectSalt", "Pairwise subject salt", "Gives this client its own subject ids per user")}
                </div>
            </div>
            <div class="section">
                <key-value-editor label="Properties" hint="Free-form key/value settings for custom code on the ID server"
                    .items=${toKv(d.properties)}
                    @change=${(e: CustomEvent<KeyValue[]>) => this.set("properties", e.detail.map(i => ({ key: i.key, value: i.value })))}></key-value-editor>
            </div>
        `
    }

    private renderDanger() {
        return html`
            <div class="stack">
                <wa-callout variant="danger">
                    <wa-icon slot="icon" name="triangle-exclamation"></wa-icon>
                    Deleting removes the client and everything attached to it. Turning off "Enabled" under General is reversible.
                </wa-callout>
                <div><wa-button variant="danger" @click=${this.removeItem}><wa-icon slot="start" name="trash"></wa-icon>Delete client</wa-button></div>
            </div>
        `
    }
}
