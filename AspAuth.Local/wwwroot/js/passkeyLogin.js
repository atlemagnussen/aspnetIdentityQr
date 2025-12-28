const html = String.raw
const css = String.raw

import { browserSupportsPasskeys, requestPasskeyOptions, isConditionalMediationAvailable } from "./passkeyService.js"

class PasskeyLogin extends HTMLElement {
  static formAssociated = true

  loginBtn = null
  abortController = null
  errorMsg = ""

  userName = ""
  /** @type {PublicKeyCredentialRequestOptions | null} */
  options = null

  attrs = {
    userName: "Input.Email",
    password: "Input.Password"
  }

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
        this.submitForm()
      }
    })

    const userNameEl = this.internals.form.querySelector("#InputEmail")
    this.userName = userNameEl.value ?? ""
    userNameEl.addEventListener("change", (e) => {
      this.userName = userNameEl.value ?? ""
      if (this.userName)
        this.obtainKeyOptions()
    })

    // try autofill if allowed
    if (isConditionalMediationAvailable()) {
      this.obtainKeyOptions(true).then(() => {
        this.submitForm(true)
      })
    }
  }

  disconnectedCallback() {
    this.abortController?.abort()
    // if (this.loginBtn)
    //   this.loginBtn.removeEventListener("click", this.submitForm)
  }

  render() {
    if (!browserSupportsPasskeys) {
      this.shadowRoot.innerHTML = html`
        <p>Passkey not supported</p>
      `
    }
    this.loginBtn = document.createElement("wa-button")
    this.loginBtn.style.display = "none"
    this.loginBtn.variant = "success"
    this.loginBtn.type = "submit"
    const btnIcon = document.createElement("wa-icon")
    btnIcon.slot = "start"
    btnIcon.name = "key"
    this.loginBtn.appendChild(btnIcon)
    this.loginBtn.appendChild(document.createTextNode("Login with passkey"))
    // this.loginBtn.addEventListener("click", this.submitForm)
    this.shadowRoot.appendChild(this.loginBtn)

    this.errorMsgCallout = document.createElement("wa-callout")
    this.errorMsgCallout.variant = "warning"
    
    const errorIcon = document.createElement("wa-icon")
    errorIcon.slot = "icon"
    errorIcon.name = "triangle-exclamation"
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
  obtainKeyOptions = async (conditionalMediation = false) => {
    this.abortController?.abort()
    this.abortController = new AbortController()
    const signal = this.abortController.signal
    
    this.loginBtn.setAttribute("loading", "")
    this.setError("")
    try {
      //const email = new FormData(this.internals.form).get(this.attrs.userName) // by name
      if (!this.userName && !conditionalMediation)
        throw new Error("missing username")
      this.options = await requestPasskeyOptions(this.userName, signal)
      if (this.options.allowCredentials && (this.options.allowCredentials.length > 0 || conditionalMediation)) {
        this.loginBtn.style.display = "block"
      } else {
        this.loginBtn.style.display = "none"
      }
    } catch (error) {
      this.loginBtn.style.display = "none"
    }
    finally {
      this.loginBtn.removeAttribute("loading")
    }
  }

  submitForm = async (conditionalMediation = false) => {
    const formData = new FormData()
    const returnUrl = this.getAttribute("return-url")

    if (!this.options) {
      this.setError("no options")
      return
    }
    const signal = this.abortController.signal
    this.loginBtn.setAttribute("loading", "")
    try {
      const credential = await navigator.credentials.get({ 
        publicKey: this.options,
        mediation: conditionalMediation ? "conditional" : undefined,
        signal
      })
      const credentialJson = JSON.stringify(credential)
      formData.append("credentialJson", credentialJson)

      this.internals.setFormValue(formData)

      this.internals.form.action = `/Identity/Account/LoginPasskey?returnUrl=${returnUrl}` // special form instead of complicating Login form even more
      this.internals.form.submit()
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
    finally {
      this.loginBtn.removeAttribute("loading")
    }
  }
}
customElements.define("passkey-login", PasskeyLogin)