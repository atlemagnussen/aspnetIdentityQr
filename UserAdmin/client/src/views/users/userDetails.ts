import WaButton from "@awesome.me/webawesome/dist/components/button/button.js"
import { DialogMovable } from "@db/client/components/dialogMovable.js"
import { UserDto } from "api/types.gen.js"
import { customElement, property, state } from "lit/decorators.js"

import * as userService from "@db/client/views/users/userService.js"
import { css, html, LitElement } from "lit"

let dialogEl: HTMLDialogElement
let dialogTitle: HTMLDivElement
let dialogContent: HTMLDivElement

export function openUserDialog(user: UserDto) {

    const dialog = getDialog()

    const dialogActualContent = getDialogContent(user.id)

    while(dialogContent.lastChild) {
        dialogContent.removeChild(dialogContent.lastChild)
    }
    dialogContent.appendChild(dialogActualContent)

    dialog.open = true
}

function getDialog() {
    if (!dialogEl) {
        dialogEl = document.createElement("dialog") as HTMLDialogElement
        dialogEl.id = "details"

        const dialogHeader = document.createElement("header") as HTMLDivElement
        dialogTitle = document.createElement("div")
        dialogTitle.innerHTML = "<h3>User details</h3>"
        dialogHeader.appendChild(dialogTitle)
        const closeBtn = document.createElement("wa-button") as WaButton
        closeBtn.id = "btnClosePopover"
        closeBtn.appearance = "plain"
        closeBtn.innerText = "X"
        closeBtn.addEventListener("click", closeDialog)
        dialogHeader.appendChild(closeBtn)

        dialogEl.appendChild(dialogHeader)
        dialogContent = document.createElement("article") as HTMLDivElement
        dialogEl.appendChild(dialogContent)
        
        const handler = new DialogMovable(dialogEl)
        document.body.appendChild(dialogEl)
    }
    return dialogEl
}
function closeDialog() {
    dialogEl.open = false
}

function getDialogContent(userId: string) {
    const userDetails = new UserDetails()
    userDetails.userid = userId
    return userDetails
}
@customElement("user-details")
export class UserDetails extends LitElement {

    static styles = css`
        
    `
    connectedCallback(): void {
        super.connectedCallback()
        this.get()
    }

    @property({attribute: true})
    userid = ""

    @state()
    user?: UserDto

    @state()
    roles: Array<string> = []

    get = async () => {
        if (!this.userid)
            return
        try {
            this.user = await userService.getUser(this.userid)
            this.roles = await userService.getUserRoles(this.userid)
        } catch (err: any) {
            console.error(err)
        }
    }
    addRole() {
        //userService.addRole(this.userid, "Admin")
    }
    render() {
        if (!this.user) {
            return html`
                <div>
                    <p>Loading user ${this.userid}</p>
                </div>
            `
        }

        return html`
            <section>
                <p>id: ${this.user.id}</p>
                <p>userName: ${this.user.userName}</p>
                <p>email: ${this.user.email}</p>
                <p>name: ${this.user.fullName}</p>
                <p>roles: ${this.roles.join(',')}</p>
            </section>
            <section>
                <wa-button @click=${this.addRole}>Add Role</wa-button
            </section>
        `
    }
}