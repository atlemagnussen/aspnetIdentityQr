const html = String.raw
const css = String.raw

import { browserSupportsPasskeys, requestPasskeyOptions } from "./passkeyService.js"

class PasskeyLogin extends HTMLElement {
  static formAssociated = true

  loginBtn = null
  abortController = null
  errorMsg = ""

  static styles = css`
    :host {
      display: block;
      min-height: 30px;
    }
  `

  constructor() {
    super()
    const sharedStyles = new CSSStyleSheet()
    sharedStyles.replaceSync(PasskeyLogin.styles)
    const shadow = this.attachShadow({ mode: "open" })
    shadow.adoptedStyleSheets = [sharedStyles]

    this.internals = this.attachInternals()
  }
  connectedCallback() {
    this.render()

    this.internals.form.addEventListener("submit", (event) => {
      if (event.submitter?.name === "__passkeySubmit") {
        event.preventDefault()
        this.obtainAndSubmitCredential()
      }
    })
  }

  disconnectedCallback() {
    this.abortController?.abort()
    if (this.loginBtn)
      this.loginBtn.removeEventListener("click", this.obtainKeyOptions)
  }

  render() {
    if (!browserSupportsPasskeys) {
      this.shadowRoot.innerHTML = html`
        <p>Passkey not supported</p>
      `
    }
    this.loginBtn = document.createElement("wa-button")
    this.loginBtn.innerText = "Login with passkey"
    this.loginBtn.variant = "brand"
    this.loginBtn.addEventListener("click", this.obtainKeyOptions)
    this.shadowRoot.appendChild(this.loginBtn)

    this.errorMsgCallout = document.createElement("wa-callout")
    this.errorMsgCallout.variant = "warning"
    // <wa-icon name="triangle-exclamation"></wa-icon>
    const errorIcon = document.createElement("wa-icon")
    errorIcon.slot = "icon"
    errorIcon.name = "triangle-exclamation"
    errorIcon.variant = "regular"
    this.errorMsgCallout.appendChild(errorIcon)
    this.errorMsg = document.createElement("strong")
    this.errorMsgCallout.appendChild(this.errorMsg)
    this.shadowRoot.appendChild(this.errorMsgCallout)
    this.errorMsgCallout.style.display = "none"
  }

  setError(errorMsg) {
    this.errorMsg.innerText = errorMsg
    this.errorMsgCallout.style.display = errorMsg ? "block" : "none"
  }
  obtainKeyOptions = async () => {
    this.abortController?.abort()
    this.abortController = new AbortController()
    const signal = this.abortController.signal
    const formData = new FormData()
    this.setError("")
    try {
      const email = new FormData(this.internals.form).get("Input.Email") // by name
      if (!email)
        throw new Error("missing username")
      const credential = await requestPasskeyOptions(email, undefined, signal)
      const credentialJson = JSON.stringify(credential)
      formData.append(`${this.attrs.name}.CredentialJson`, credentialJson)
      
    } catch (error) {
      if (error.name === "AbortError") {
        // The user explicitly canceled the operation - return without error.
        return
      }
      console.error(error)
      const errorMessage = error.name === "NotAllowedError"
        ? "No passkey was provided by the authenticator."
        : error.message
      console.error(errorMessage)
      this.setError(errorMessage)
    }

  }

  async tryAutofillPasskey() {
    if (browserSupportsPasskeys && this.attrs.operation === "Request" && await PublicKeyCredential.isConditionalMediationAvailable?.()) {
      await this.obtainAndSubmitCredential(/* useConditionalMediation */ true)
    }
  }
}
customElements.define("passkey-login", PasskeyLogin)