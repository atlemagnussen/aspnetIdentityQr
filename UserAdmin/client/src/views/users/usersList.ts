import { UserDto } from "@db/api"
import {LitElement, css, html} from "lit"
import {customElement, state} from "lit/decorators.js"

import * as userService from "@db/client/views/users/userService.js"

//import { openMigrationDialog } from "./migration/dialogOpener.js"

@customElement("users-list")
export class UsersList extends LitElement {

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
	result?: Array<UserDto>

	@state()
	showStorage = false

	toggleShowStorage() {
		this.showStorage = !this.showStorage
	}

	get = async () => {
		try {
			const dbs = await userService.getUsers()
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
				<h3>Users</h3>
				<table>
					<thead>
						<tr>
							<th>Id</th>
							<th>UserName</th>
							<th>Name</th>
							<th>Email</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						${this.result.map(r => {
							return html`
								<tr>
									<td>${r.id}</td>
									<td>${r.userName}</td>
									<td>${r.fullName}</td>
									<td>${r.email}</td>
									<td>
										<wa-button variant="neutral" appearance="filled">
											<wa-icon name="house" label="Home"></wa-icon>
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