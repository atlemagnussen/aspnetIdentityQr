
import fs from "fs"
import fsp from "fs/promises"
import * as path from "path"
import * as http from "http"
//import * as https from "https"

export async function saveFile(content, directory, name) {

    if (!fs.existsSync(directory))
        await fsp.mkdir(directory)
    
    const fullPath = path.join(directory, name)

    await fsp.writeFile(fullPath, content)
    return fullPath
}

export async function readFile(path) {
    const content = await fsp.readFile(path, { encoding: 'utf8' })
    return content
}

export function getData(url) {
    let httpMod = http
    return new Promise((resolve, reject) => {
        const request = httpMod.request(url, (res) => {
            res.setEncoding('utf8')
            let data = ""
            res.on("data", (chunk) => data += chunk)
            res.on("end", () => resolve(data))
        })
            .on("error", (e) => reject(e))
        request.end()
    })
}