import WaButton from "@awesome.me/webawesome/dist/components/button/button.js"
import { Client } from "@db/api"
import { addCorsOrigin, addRedirectUrl } from "@db/client/views/configuration/configurationService.js"
import { LitElement, css, html } from "lit"
import { customElement, property, state } from "lit/decorators.js"

let dialogEl: HTMLDialogElement
let dialogTitleEl: HTMLDivElement
let dialogContentEl: HTMLDivElement

export function openClientEditDialog(client: Client, onSaved?: () => void) {
    const dialog = getDialog()
    const content = getDialogContent(client, onSaved)

    while (dialogContentEl.lastChild) {
        dialogContentEl.removeChild(dialogContentEl.lastChild)
    }
    dialogContentEl.appendChild(content)

    dialogTitleEl.innerHTML = `<h3>Edit client: ${client.clientId}</h3>`
    if (!dialog.open) {
        dialog.showModal()
    }
}

function getDialog() {
    if (!dialogEl) {
        dialogEl = document.createElement("dialog") as HTMLDialogElement
        dialogEl.id = "client-edit-dialog"
        dialogEl.setAttribute("closedby", "none")
        dialogEl.addEventListener("cancel", e => e.preventDefault())

        const dialogHeader = document.createElement("header") as HTMLDivElement

        dialogTitleEl = document.createElement("div")
        dialogTitleEl.innerHTML = "<h3>Edit client</h3>"
        dialogHeader.appendChild(dialogTitleEl)

        const closeBtn = document.createElement("wa-button") as WaButton
        closeBtn.id = "btnCloseClientDialog"
        closeBtn.appearance = "plain"
        closeBtn.setAttribute("label", "Close")
        closeBtn.innerText = "Close"
        closeBtn.addEventListener("click", closeDialog)
        dialogHeader.appendChild(closeBtn)

        dialogEl.appendChild(dialogHeader)

        dialogContentEl = document.createElement("article") as HTMLDivElement
        dialogEl.appendChild(dialogContentEl)

        document.body.appendChild(dialogEl)
    }

    return dialogEl
}

function closeDialog() {
    dialogEl.close()
}

function getDialogContent(client: Client, onSaved?: () => void) {
    const details = new ClientEditDetails()
    details.client = client
    details.onSaved = onSaved
    return details
}

@customElement("client-edit-details")
export class ClientEditDetails extends LitElement {
    static styles = css`
        :host {
            display: block;
            color: var(--wa-color-text-normal);
        }

        .layout {
            display: grid;
            grid-template-columns: 14rem 1fr;
            gap: var(--wa-space-m);
            min-width: min(56rem, 80vw);
            max-width: 80vw;
            padding: var(--wa-space-m);
        }

        nav {
            display: flex;
            flex-direction: column;
            gap: var(--wa-space-2xs);
            background: var(--wa-color-surface-lowered);
            border: var(--wa-border-width-s) var(--wa-border-style) var(--wa-color-surface-border);
            border-radius: var(--wa-border-radius-m);
            padding: var(--wa-space-s);
        }

        .section-title {
            margin: 0 0 var(--wa-space-2xs) 0;
            font-size: var(--wa-font-size-s);
            color: var(--wa-color-text-quiet);
        }

        .editor {
            background: var(--wa-color-surface-raised);
            border: var(--wa-border-width-s) var(--wa-border-style) var(--wa-color-surface-border);
            border-radius: var(--wa-border-radius-m);
            padding: var(--wa-space-m);
            display: grid;
            gap: var(--wa-space-s);
            align-content: start;
        }

        .help {
            margin: 0;
            color: var(--wa-color-text-quiet);
            font-size: var(--wa-font-size-s);
        }

        .actions {
            display: flex;
            justify-content: flex-end;
        }

        .message {
            margin: 0;
            font-size: var(--wa-font-size-s);
        }

        .message.error {
            color: var(--wa-color-danger-fill-loud);
        }

        .message.success {
            color: var(--wa-color-success-fill-loud);
        }

        @media (max-width: 840px) {
            .layout {
                grid-template-columns: 1fr;
                min-width: min(32rem, 90vw);
                max-width: 90vw;
            }

            nav {
                flex-direction: row;
                overflow-x: auto;
            }
        }
    `

    @property({ attribute: false })
    client?: Client

    @property({ attribute: false })
    onSaved?: () => void

    @state()
    private selectedSection: "redirect" | "cors" | "scopes" | "grants" = "redirect"

    @state()
    private redirectUrl = ""

    @state()
    private corsOrigin = ""

    @state()
    private isSaving = false

    @state()
    private error = ""

    @state()
    private success = ""

    private onInputRedirectUrl(e: Event) {
        const input = e.target as HTMLInputElement
        this.redirectUrl = input.value
        this.error = ""
        this.success = ""
    }

    private async saveRedirectUrl() {
        const clientId = this.client?.clientId
        const url = this.redirectUrl.trim()

        if (!clientId) {
            this.error = "Missing client id."
            return
        }

        if (!url) {
            this.error = "Please enter a redirect URL."
            return
        }

        try {
            new URL(url)
        } catch {
            this.error = "Please enter a valid absolute URL."
            return
        }

        this.isSaving = true
        this.error = ""
        this.success = ""

        try {
            await addRedirectUrl(clientId, url)
            this.redirectUrl = ""
            this.success = "Redirect URL saved."
            this.onSaved?.()
        } catch (err: any) {
            this.error = err?.message ?? "Unable to save redirect URL."
        } finally {
            this.isSaving = false
        }
    }

    private onInputCorsOrigin(e: Event) {
        const input = e.target as HTMLInputElement
        this.corsOrigin = input.value
        this.error = ""
        this.success = ""
    }

    private async saveCorsOrigin() {
        const clientId = this.client?.clientId
        const originInput = this.corsOrigin.trim()

        if (!clientId) {
            this.error = "Missing client id."
            return
        }

        if (!originInput) {
            this.error = "Please enter a CORS origin."
            return
        }

        try {
            new URL(originInput)
        } catch {
            this.error = "Please enter a valid absolute URL."
            return
        }

        this.isSaving = true
        this.error = ""
        this.success = ""

        try {
            await addCorsOrigin(clientId, originInput)
            this.corsOrigin = ""
            this.success = "CORS origin saved."
            this.onSaved?.()
        } catch (err: any) {
            this.error = err?.message ?? "Unable to save CORS origin."
        } finally {
            this.isSaving = false
        }
    }

    render() {
        return html`
            <section class="layout">
                <nav aria-label="Client edit sections">
                    <p class="section-title">Sections</p>
                    <wa-button
                        variant=${this.selectedSection === "redirect" ? "brand" : "neutral"}
                        appearance="filled"
                        @click=${() => (this.selectedSection = "redirect")}
                    >
                        Redirect URLs
                    </wa-button>
                    <wa-button
                        variant=${this.selectedSection === "cors" ? "brand" : "neutral"}
                        appearance="filled"
                        @click=${() => (this.selectedSection = "cors")}
                    >
                        CORS origins
                    </wa-button>
                    <wa-button variant="neutral" appearance="outlined" disabled>Scopes (soon)</wa-button>
                    <wa-button variant="neutral" appearance="outlined" disabled>Grant types (soon)</wa-button>
                </nav>

                <article class="editor">
                    ${this.selectedSection === "redirect"
                        ? html`
                              <h4>Redirect URLs</h4>
                              <p class="help">
                                  Add one redirect URL for client <strong>${this.client?.clientId ?? ""}</strong>.
                              </p>

                              <wa-input
                                  label="Redirect URL"
                                  placeholder="https://example.com/signin-oidc"
                                  .value=${this.redirectUrl}
                                  @input=${this.onInputRedirectUrl}
                              ></wa-input>
                          `
                        : html`
                              <h4>CORS origins</h4>
                              <p class="help">
                                  Add one CORS origin for client <strong>${this.client?.clientId ?? ""}</strong>.
                              </p>

                              <wa-input
                                  label="CORS origin"
                                  placeholder="https://app.example.com"
                                  .value=${this.corsOrigin}
                                  @input=${this.onInputCorsOrigin}
                              ></wa-input>
                          `}

                    ${this.error ? html`<p class="message error">${this.error}</p>` : null}
                    ${this.success ? html`<p class="message success">${this.success}</p>` : null}

                    <div class="actions">
                        <wa-button
                            variant="brand"
                            appearance="filled"
                            ?disabled=${this.isSaving}
                            @click=${this.selectedSection === "redirect" ? this.saveRedirectUrl : this.saveCorsOrigin}
                        >
                            ${this.isSaving ? "Saving..." : "Save"}
                        </wa-button>
                    </div>
                </article>
            </section>
        `
    }
}