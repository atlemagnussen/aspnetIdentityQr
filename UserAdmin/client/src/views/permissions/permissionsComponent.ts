import {LitElement, css, html} from "lit"
import {customElement, property, state} from "lit/decorators.js"
import * as dbService from "@db/client/services/dbService.js"
import { UserDbRole } from "@db/client/models/UserDbRole.js"

@customElement("permissions-element")
export class DbComponent extends LitElement {

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

    @property({attribute: true})
    dbname = "DigiLeanMaster"


    @state()
    result: Array<UserDbRole> = []

    get = async () => {
        try {
            const dbs = await dbService.getRoles(this.dbname)
            this.result = dbs
        } catch (err: any) {
            this.result = err.message
        }
    }
    render() {
        if (!this.result) {
            return html`
            <div>
                <p>No result</p>
                <button @click=${this.get}>Get Dbs</button>
            </div>
            `
        }
        return html`	
            <section>
                <h4>${this.dbname}</h4>
                <table>
					<thead>
						<tr>
							<th>User</th>
							<th>Role</th>
							<th>Created</th>
						</tr>
					</thead>
					<tbody>
						${this.result.map(r => {
							return html`
								<tr>
									<td>${r.userName}</td>
									<td>${r.roleName}</td>
                                    <td>
										<date-viewer .date=${r.createDate}></date-viewer>
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