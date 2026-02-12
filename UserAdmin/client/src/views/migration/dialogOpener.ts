import { Database } from "@db/client/models/Database.js"
import { MigrationDialog } from "./migrationDialog.js"
import WaButton from "@awesome.me/webawesome/dist/components/button/button.js"
import { DialogMovable } from "@db/client/components/dialogMovable.js"

const dialogCache = new Map<string, MigrationDialog>()
let dialogEl: HTMLDialogElement
let dialogTitle: HTMLDivElement
let dialogContent: HTMLDivElement

export function openMigrationDialog(db: Database) {

    const dialog = getDialog()

    const dialogActualContent = getDialogContent(db)

    while(dialogContent.lastChild) {
        dialogContent.removeChild(dialogContent.lastChild)
    }
    dialogContent.appendChild(dialogActualContent)

    dialog.open = true
}

function getDialog() {
    if (!dialogEl) {
        dialogEl = document.createElement("dialog") as HTMLDialogElement
        dialogEl.id = "migrate"

        const dialogHeader = document.createElement("header") as HTMLDivElement
        dialogTitle = document.createElement("div")
        dialogTitle.innerHTML = "<h3>Migration</h3>"
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
function getDialogContent(db: Database) {
    const exists = dialogCache.get(db.name)
    if (exists)
        return exists

    const dialogContent = new MigrationDialog()
    dialogCache.set(db.name, dialogContent)
    dialogContent.db = db
    return dialogContent
}