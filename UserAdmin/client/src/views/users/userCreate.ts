import type { UserCreatedDto } from "@db/api"
import { html, nothing } from "lit"
import { customElement, state } from "lit/decorators.js"
import { AdminElement, valueOf } from "@db/client/components/adminElement.js"
import { adminStyles } from "@db/client/components/sharedStyles.js"
import "@db/client/components/oneTimeValue.js"
import * as users from "@db/client/views/users/usersService.js"

/** Create a user and hand the admin a set-password link to pass on. */
@customElement("user-create")
export class UserCreate extends AdminElement {
    static styles = adminStyles

    @state()
    private email = ""

    @state()
    private userName = ""

    @state()
    private fullName = ""

    @state()
    private created?: UserCreatedDto

    // explicit click/Enter handling: wa-button type=submit doesn't submit a form inside our shadow root
    private onEnter = (e: KeyboardEvent) => {
        if (e.key === "Enter" && this.email.trim() && !this.busy) this.submit()
    }

    private async submit() {
        const result = await this.run(() => users.createUser({
            email: this.email.trim(),
            userName: this.userName.trim() || null,
            fullName: this.fullName.trim() || null,
        }))
        if (result) {
            this.created = result
            this.dispatchEvent(new CustomEvent("user-changed"))
        }
    }

    render() {
        if (this.created) {
            return html`
                <div class="stack">
                    <p>Created <strong>${this.created.user.userName}</strong>.</p>
                    <one-time-value
                        heading="Set-password link"
                        message="Send this to the user. It opens the ID server's reset page, where they enter their email and choose a password."
                        value=${this.created.passwordLink.url}
                        expiresAt=${this.created.passwordLink.expiresAt}
                    ></one-time-value>
                    <p class="help">Assign roles from the user list afterwards.</p>
                    <div class="actions">
                        <wa-button variant="brand" @click=${() => this.dispatchEvent(new CustomEvent("close-dialog"))}>Done</wa-button>
                    </div>
                </div>
            `
        }

        return html`
            <div class="stack" @keydown=${this.onEnter}>
                <wa-input label="Email" type="email" required autofocus .value=${this.email}
                    @input=${(e: Event) => (this.email = valueOf(e))}></wa-input>
                <wa-input label="User name" hint="Defaults to the email" .value=${this.userName}
                    @input=${(e: Event) => (this.userName = valueOf(e))}></wa-input>
                <wa-input label="Full name" .value=${this.fullName}
                    @input=${(e: Event) => (this.fullName = valueOf(e))}></wa-input>
                <p class="help">The email is marked as confirmed. You'll get a one-time link for the user to set their password.</p>
                <error-viewer .error=${this.error}></error-viewer>
                <div class="actions">
                    <wa-button variant="brand" ?loading=${this.busy} ?disabled=${!this.email.trim()} @click=${this.submit}>
                        Create user
                    </wa-button>
                </div>
            </div>
            ${nothing}
        `
    }
}
