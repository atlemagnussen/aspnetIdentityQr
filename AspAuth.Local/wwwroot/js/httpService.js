const jsonContentType = "application/json"

export default class HttpService {
    
    BaseUrl = window.location.origin

    /**
     * @param {string} urlPath - only path and not baseurl/host
     */
    get(urlPath) {
        const req = this.createRequest(urlPath, "get", jsonContentType)
        return this.http(req)
    }
    /**
     * @param {string} urlPath - only path and not baseurl/host
     * @param {AbortSignal | null} signal
     * @param {any} data - payload
     * @returns {Promise}
     */
    post(urlPath, signal, data) {
        const req = this.createRequest(urlPath, "post", signal, jsonContentType, data)
        return this.http(req)
    }
    /**
     * @param {string} urlPath - only path and not baseurl/host
     * @param {any} data - payload
     */
    put(urlPath, data) {
        const req = this.createRequest(urlPath, "put", jsonContentType, data)
        return this.http(req)
    } 
    /**
     * @param {string} urlPath - only path and not baseurl/host
     */
    delete(urlPath) {
    const req = this.createRequest(urlPath, "delete")
        return this.http(req)
    }
    
    /**
     * @param {string} url
     * @param {string} method
     * @param {AbortSignal | null} signal
     * @param {string?} contentType
     * @returns {Request}
     */
    createRequest = (url, method, signal, contentType, data) => {
        const headers = getHeaders(contentType)
        const args = {
            credentials: "include",
            method,
            headers,
            signal
        }
        
        if (data) {
            if (contentType === jsonContentType)
                args.body = JSON.stringify(data)
            else
                args.body = data
        }

        const fullUrl = `${this.BaseUrl}/${url}`
        return new Request(fullUrl, args)
    }
    /**
     * @param {RequestInfo} request 
     * @returns {Promise}
     */
    async http(request) {
        try {
            const res = await fetch(request)
            return resHandler(res)
        }
        catch (e) {
            console.error(e)
        }
        throw new HttpServiceError(`Server error. Check if ${this.BaseUrl} is up`, 500)
    }
}

async function resHandler (res) {
    if (res.ok) {
        const contentType = res.headers.get("content-type")
        if (res.status === 200 || res.status === 201) {

            if (contentType) {
                if (contentType.includes("application/json")) {
                    const json = await res.json()
                    return json
                }
                // pdf or octet-stream
                if (contentType.includes("application/")) {
                    const file = await res.arrayBuffer()
                    return file
                }
            }
            const text = await res.text()
            return text
        }
        else {
            return ""
        }
    } else {
        console.error(`${res.statusText} (${res.status})`)
        let errorFetchMsg = "Server returned error"

        if (res.status == 401) {
            errorFetchMsg = "Status 401, token might be expired"
            
        } else {
            errorFetchMsg = await res.text()
        }
        console.log(errorFetchMsg)
        throw new HttpServiceError(errorFetchMsg, res.status)
    }
}

function getBearer() {
    const token = getAccessToken()
    if (token) {
        return `Bearer ${token}`
    }
    return ""
}

function getHeaders(contentType) {
    const headers = {}
    if (contentType)
        headers["Content-Type"] = contentType
    
    return headers
}

export class HttpServiceError extends Error {
    status = 0

    constructor(message, status) {
        super(message)
        this.status = status
    }
}
