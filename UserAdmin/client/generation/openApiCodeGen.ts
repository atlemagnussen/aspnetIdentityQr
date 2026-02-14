import * as OpenAPI from "@hey-api/openapi-ts"
import { getData, saveFile } from "./common.mjs"

const openApiUrl = "http://localhost:5057/openapi/v1.json"

// let swaggerUrl = "https://api-test-digilean.tools/swagger/v2/swagger.json"
let localFolder = "api"
let localFile = "v1.json"

const config: OpenAPI.UserConfig = {
    input: `./${localFolder}/${localFile}`,
    output: {
        path: `./${localFolder}`,
        clean: false
    },
    plugins: [
        {
            enums: "typescript",
            name: "@hey-api/typescript"
        }
    ]
}

const gen = async () => {
    try {
        const fileContent = await getData(openApiUrl)
        if (!fileContent)
            console.error(`no file content for ${openApiUrl}`)
        await saveFile(fileContent, localFolder, localFile)
        OpenAPI.createClient(config)
    }
    catch (err) {
        console.error(err)
    }
}
gen().then(() => console.log("done"))
    .catch(err => console.error(err))
