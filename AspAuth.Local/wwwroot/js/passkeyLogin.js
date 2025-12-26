const html = String.raw
const css = String.raw

import { browserSupportsPasskeys, requestPasskeyOptions } from "./passkeyService.js"

class PasskeyLogin extends HTMLElement {
  loginBtn = null
  abortController = null

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
  }
  connectedCallback() {
    this.render()
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
  }
  obtainKeyOptions = async () => {
    this.abortController?.abort()
    this.abortController = new AbortController()
    const signal = this.abortController.signal

    try {
      const credential = await requestPasskeyOptions("atlemagnussen@gmail.com", "conditional", signal)
      console.log("credential", credential)
      
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
    }

  }

  async tryAutofillPasskey() {
    if (browserSupportsPasskeys && this.attrs.operation === "Request" && await PublicKeyCredential.isConditionalMediationAvailable?.()) {
      await this.obtainAndSubmitCredential(/* useConditionalMediation */ true)
    }
  }
}
customElements.define("passkey-login", PasskeyLogin)