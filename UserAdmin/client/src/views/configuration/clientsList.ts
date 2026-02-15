import { Client } from "@db/api"
import {LitElement, css, html} from "lit"
import {customElement, state} from "lit/decorators.js"
import * as service from "@db/client/views/configuration/configurationService.js"

@customElement("clients-list")
export class ClientsList extends LitElement {

    static styles = css`
        tbody { 
            color: black;
            tr:nth-child(odd) {
                background-color: var(--digilean-blue-sky-light);
            }
            tr:nth-child(even) {
                background-color: var(--digilean-blue-light);
            }
            tr td {
                padding: 0.3rem;
            }
        }
        button {
            cursor: pointer;
        }
    `
    connectedCallback(): void {
        super.connectedCallback()
        this.get()
    }
    @state()
    clients?: Array<Client>

    @state()
    showStorage = false

    toggleShowStorage() {
        this.showStorage = !this.showStorage
    }

    error = ""
    get = async () => {
        try {
            this.clients = await service.getClients()
        } catch (err: any) {
            this.error = err.message
        }
    }

    render() {
        if (!this.clients) {
            return html`
            <div>
                <p>No result</p>
                <button @click=${this.get}>Get Dbs</button>
            </div>
            `
        }

        return html`
            <section>
                <h3>Users</h3>
                <table>
                    <thead>
                        <tr>
                            <th>Id</th>
                            <th>Name</th>
                            <th>Lifetime</th>
                            <th>Protocol</th>
                            <th>PKCE</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        ${this.clients.map(c => {
                            return html`
                                <tr>
                                    <td>${c.clientId}</td>
                                    <td>${c.clientName}</td>
                                    <td>${c.accessTokenLifetime}</td>
                                    <td>${c.protocolType}</td>
                                    <td>${c.requirePkce}</td>
                                    <td>
                                        <wa-button variant="neutral" appearance="filled"
                                            href="/api/clients/${c.clientId}">
                                            <wa-icon name="pen-to-square" variant="regular"></wa-icon>
                                        </wa-button>
                                    </td>
                                </tr>
                            `
                        })}
                    </tbody>
                </table>
            </section>
        `
    }
}