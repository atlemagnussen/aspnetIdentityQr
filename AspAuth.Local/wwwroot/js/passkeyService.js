import HttpService from "./httpService.js"


export const browserSupportsPasskeys =
  typeof navigator.credentials !== "undefined" &&
  typeof window.PublicKeyCredential !== "undefined" &&
  typeof window.PublicKeyCredential.parseCreationOptionsFromJSON === "function" &&
  typeof window.PublicKeyCredential.parseRequestOptionsFromJSON === "function"



const http = new HttpService()

const urlPasskeyCreationOptions = "api/Account/PasskeyCreationOptions"
const urlPasskeyCreate = "api/Account/PasskeyCreate"
const urlPasskeyRequestOptions = "api/Account/PasskeyRequestOptions"


/**
 * get createOptions before create
 * 
 * @param {AbortSignal | null} signal 
 * @returns 
 */
export async function passkeyCreateOptions(signal) {
  const optionsResponse = await http.post(urlPasskeyCreationOptions, signal)
  console.log("Create optionsResponse", optionsResponse)
  const optionsJson = typeof optionsResponse === "string" ? JSON.parse(optionsResponse) : optionsResponse
  const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson)
  const createdCred = await navigator.credentials.create({ publicKey: options, signal })
  return createdCred
}

/**
 * create passkey
 * @param {AbortSignal | null} signal 
 * @param {*} credentialJson 
 * @returns 
 */
export function createPasskey(signal, credentialJson) {
  const credentialsString = JSON.stringify(credentialJson)
  return http.post(urlPasskeyCreate, signal, credentialsString)
}


/**
 * Get existing options
 * 
 * @param {string} userName 
 * @param {string} mediation 
 * @param {AbortSignal | null} signal 
 * @returns 
 */
async function requestCredential(userName, mediation, signal) {
  const optionsJson = await http.post(`${urlPasskeyRequestOptions}?userName=${email}`, signal)
  console.log("optionsJson", optionsJson)
  const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson)
  return await navigator.credentials.get({ publicKey: options, mediation, signal })
}

class PasskeySubmit extends HTMLElement {
  static formAssociated = true

  connectedCallback() {
    this.internals = this.attachInternals()
    this.attrs = {
      operation: this.getAttribute("operation"),
      name: this.getAttribute("name"),
      emailName: this.getAttribute("email-name")
    }

    this.internals.form.addEventListener("submit", (event) => {
      if (event.submitter?.name === "__passkeySubmit") {
        event.preventDefault()
        this.obtainAndSubmitCredential()
      }
    })

    this.tryAutofillPasskey()
  }

  disconnectedCallback() {
    this.abortController?.abort()
  }

  async obtainCredential(useConditionalMediation, signal) {
    if (!browserSupportsPasskeys) {
      throw new Error("Some passkey features are missing. Please update your browser.")
    }

    if (this.attrs.operation === "Create") {
      return await createCredential(headers, signal)
    } else if (this.attrs.operation === "Request") {
      const email = new FormData(this.internals.form).get(this.attrs.emailName)
      const mediation = useConditionalMediation ? "conditional" : undefined
      return await requestCredential(email, mediation, headers, signal)
    } else {
      throw new Error(`Unknown passkey operation "${this.attrs.operation}".`)
    }
  }

  async obtainAndSubmitCredential(useConditionalMediation = false) {
    this.abortController?.abort()
    this.abortController = new AbortController()
    const signal = this.abortController.signal
    const formData = new FormData()
    try {
      const credential = await this.obtainCredential(useConditionalMediation, signal)
      const credentialJson = JSON.stringify(credential)
      formData.append(`${this.attrs.name}.CredentialJson`, credentialJson)
    } catch (error) {
      if (error.name === "AbortError") {
        // The user explicitly canceled the operation - return without error.
        return
      }
      console.error(error)
      if (useConditionalMediation) {
        // An error occurred during conditional mediation, which is not user-initiated.
        // We log the error in the console but do not relay it to the user.
        return
      }
      const errorMessage = error.name === "NotAllowedError"
        ? "No passkey was provided by the authenticator."
        : error.message
      formData.append(`${this.attrs.name}.Error`, errorMessage)
    }
    this.internals.setFormValue(formData)
    this.internals.form.submit()
  }

  async tryAutofillPasskey() {
    if (browserSupportsPasskeys && this.attrs.operation === "Request" && await PublicKeyCredential.isConditionalMediationAvailable?.()) {
      await this.obtainAndSubmitCredential(/* useConditionalMediation */ true)
    }
  }
}
customElements.define("passkey-submit", PasskeySubmit)