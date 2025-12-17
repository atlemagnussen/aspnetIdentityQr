const html = String.raw
const css = String.raw

import { browserSupportsPasskeys, passkeyCreateOptions, createPasskey } from "./passkeyService.js"

class PasskeySubmit extends HTMLElement {
  btnCreate = null
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
    sharedStyles.replaceSync(PasskeySubmit.styles)
    const shadow = this.attachShadow({ mode: "open" })
    shadow.adoptedStyleSheets = [sharedStyles]
  }
  connectedCallback() {
    this.render()
  }

  disconnectedCallback() {
    this.abortController?.abort()
    if (this.createBtn)
      this.createBtn.removeEventListener("click", this.obtainAndSubmitNewKey)
  }

  render() {
    if (!browserSupportsPasskeys) {
      this.shadowRoot.innerHTML = html`
        <p>Passkey not supported</p>
      `
    }
    this.createBtn = document.createElement("button")
    this.createBtn.innerText = "Create new passkey"
    this.createBtn.addEventListener("click", this.obtainAndSubmitNewKey)
    this.shadowRoot.appendChild(this.createBtn)
  }
  obtainAndSubmitNewKey = async () => {
    this.abortController?.abort()
    this.abortController = new AbortController()
    const signal = this.abortController.signal

    try {
      const credential = await passkeyCreateOptions(signal)
      console.log("credential", credential)
      await createPasskey(signal, credential)
      console.log("created")
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
customElements.define("passkey-create", PasskeySubmit)