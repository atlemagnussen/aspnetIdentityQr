import { doImportWa } from "./waLoader.js"
const systemDark = window.matchMedia("(prefers-color-scheme: dark)").matches;

if (systemDark) {
    document.body.classList.add("dark")
    document.body.classList.add("wa-dark")
}
else {
    document.body.classList.add("light")
    document.body.classList.add("wa-light")
}

doImportWa().then((success) => {
    console.log("imported wa", success)
})