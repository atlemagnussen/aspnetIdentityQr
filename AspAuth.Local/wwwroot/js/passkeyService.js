import HttpService from "./httpService.js"

export const browserSupportsPasskeys =
  typeof navigator.credentials !== "undefined" &&
  typeof window.PublicKeyCredential !== "undefined" &&
  typeof window.PublicKeyCredential.parseCreationOptionsFromJSON === "function" &&
  typeof window.PublicKeyCredential.parseRequestOptionsFromJSON === "function"


export function isConditionalMediationAvailable() {
  if (
    typeof PublicKeyCredential.isConditionalMediationAvailable === "function" &&
    PublicKeyCredential.isConditionalMediationAvailable()
  ) {
    console.log("Conditional Mediation is available");
    return true;
  }
  console.log("Conditional Mediation is not available");
  return false;
}
const http = new HttpService("/api/Account")

const urlPasskeyCreationOptions = "PasskeyCreationOptions"
const urlPasskeyRequestOptions = "PasskeyRequestOptions"
const urlPasskeyCreate = "PasskeyCreate"
const urlPasskeyUpdate = "PasskeyUpdate"
const urlPasskeyDelete = "PasskeyDelete"


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
 * Get existing options for authenticatio 
 * 
 * @param {string} userName 
 * @param {string} mediation 
 * @param {AbortSignal | null} signal 
 * @returns 
 */
export async function requestPasskeyOptions(userName, signal) {
  const optionsResponse = await http.post(`${urlPasskeyRequestOptions}?userName=${userName}`, signal)
  console.log("optionsResponse", optionsResponse)
  const optionsJson = typeof optionsResponse === "string" ? JSON.parse(optionsResponse) : optionsResponse
  const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson)
  return options
}

/**
 * create passkey
 * @param {AbortSignal | null} signal 
 * @param {object} credentialJson 
 * @returns 
 */
export function createPasskey(signal, credentialJson) {
  const credentialsString = JSON.stringify(credentialJson)
  return http.post(urlPasskeyCreate, signal, credentialsString)
}

/**
 * update name of passkey
 * @param {AbortSignal | null} signal 
 * @param {string} credentialId 
 * @param {string} name 
 */
export function renamePasskey(signal, credentialId, name) {
  var model = { credentialId, name }
  return http.post(urlPasskeyUpdate, signal, model)
}
/**
 * Delete passkey
 * @param {AbortSignal | null} signal 
 * @param {string} credentialId 
 * @returns 
 */
export function deletePasskey(signal, credentialId) {
  var model = { credentialId, name: "" }
  return http.post(urlPasskeyDelete, signal, model)
}