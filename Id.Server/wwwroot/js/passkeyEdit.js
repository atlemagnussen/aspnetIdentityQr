import { renamePasskey } from "./passkeyService.js"

let dialogEl = document.querySelector("#passkeyEditDialog")

let currentCredentialId = ""

function setupSaveAction() {
    const input = dialogEl.querySelector("wa-input")
    const btnSave = dialogEl.querySelector("#btnSave")
    btnSave.addEventListener("click", async () => {
        const newName = input.value
        console.log("newName", newName)
        dialogEl.open = false
        if (currentCredentialId || newName) {
            await renamePasskey(null, currentCredentialId, newName)
            location.reload()
        }
    })
}

function openEditDialog(evt) {
    const el = evt.currentTarget
    const { credentialId } = el.dataset
    if (!credentialId) {
        console.log("missing credentialId")
        return
    }
    currentCredentialId = credentialId
    const currentValue = el.previousSibling.textContent.trim()
    dialogEl.querySelector("wa-input").value = currentValue
    dialogEl.open = true
}

function init() {
    const editButtons = document.querySelectorAll('wa-button[data-credential-id]')
    editButtons.forEach(el => {
        el.addEventListener("click", openEditDialog)
    })
}

init()
setupSaveAction()