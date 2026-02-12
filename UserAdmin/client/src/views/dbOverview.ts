import {LitElement, css, html} from "lit"
import {customElement, state} from "lit/decorators.js"

import * as dbService from "@db/client/services/dbService.js"
import { Database } from "@db/client/models/Database.js"
import { DatabaseType } from "@db/client/models/DatabaseType.js"
import { openMigrationDialog } from "./migration/dialogOpener.js"

@customElement("db-overview")
export class DbOverview extends LitElement {

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
	result?: Array<Database>

	@state()
	showStorage = false

	toggleShowStorage() {
		this.showStorage = !this.showStorage
	}

	get = async () => {
		try {
			const dbs = await dbService.getDatabases()
			this.result = dbs
		} catch (err: any) {
			this.result = err.message
		}
	}

	openMigration(db: Database) {
		openMigrationDialog(db)
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

		let databases = structuredClone(this.result)
		if (!this.showStorage)
			databases = databases.filter(d => d.type !== DatabaseType.StorageAccount)

		return html`
			<section>
				<h3>Customer databases</h3>
				<label>
					Show Storage
					<input type="checkbox" @change=${this.toggleShowStorage}>
				</label>
				<table>
					<thead>
						<tr>
							<th>Name</th>
							<th>Type</th>
							<th>ConnectionString</th>
							<th>Server</th>
							<th>Replica</th>
							<th>OnPrem</th>
							<th>CustomerId</th>
							<th>Created</th>
							<th>Version</th>
							<th>Comment</th>
							<th>Updated</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						${databases.map(r => {
							return html`
								<tr>
									<td>${r.name}</td>
									<td>${r.type}</td>
									<td>${r.connectionStringName}</td>
									<td>${r.server}</td>
									<td>${r.serverReplica}</td>
									<td>${r.isOnPremise}</td>
									<td>${r.customerId}</td>
									<td>
										<date-viewer .date=${r.created!}></date-viewer>
									</td>
									<td>${r.version}</td>
									<td>${r.comment}</td>
									<td>
										<date-viewer .date=${r.updated!}></date-viewer>
									</td>
									<td>
										<wa-button variant="neutral" appearance="filled" @click=${()=>this.openMigration(r)}>
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