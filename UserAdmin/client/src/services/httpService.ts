const jsonContentType = "application/json"

export default class HttpService {

  BaseUrl = window.location.origin

  /**
   *
   */
  constructor (baseUrl: string) {
    if (baseUrl)
      this.BaseUrl = baseUrl
  }
  /**
   * @param {string} urlPath - only path and not baseurl/host
   */
  get<T>(urlPath: string) {
    const req = this.createRequest(urlPath, "get", jsonContentType)
    return this.http<T>(req)
  }
  /**
   * @param {string} urlPath - only path and not baseurl/host
   * @param {any} data - payload
   * @returns {Promise}
   */
  post<T>(urlPath: string, data: object = {}) {
    const req = this.createRequest(urlPath, "post", jsonContentType, data)
    return this.http<T>(req)
  }
  /**
   * @param {string} urlPath - only path and not baseurl/host
   * @param {any} data - payload
   */
  put<T>(urlPath: string, data: object) {
    const req = this.createRequest(urlPath, "put", jsonContentType, data)
    return this.http<T>(req)
  }
  /**
   * @param {string} urlPath - only path and not baseurl/host
   */
  delete<T = void>(urlPath: string) {
    const req = this.createRequest(urlPath, "delete")
    return this.http<T>(req)
  }

  /**
   * @param {string} url
   * @param {string} method
   * @param {string?} contentType
   * @returns {Request}
   */
  createRequest = (url: string, method: string, contentType?: string, data?: object) => {
    contentType ??= jsonContentType
    const headers = getHeaders(contentType)
    const args: RequestInit = {
      credentials: "include",
      method,
      headers
    }

    if (data) {
      if (contentType === jsonContentType)
        args.body = JSON.stringify(data)
      else
        args.body = data as BodyInit
    }

    const fullUrl = `${this.BaseUrl}/${url}`
    return new Request(fullUrl, args)
  }
  /**
   * @param {RequestInfo} request 
   * @returns {Promise}
   */
  async http<T>(request: Request) {
    try {
      const res = await fetch(request)
      return resHandler(res) as T
    }
    catch (e) {
      console.error(e)
    }
    throw new HttpServiceError(`Server error. Check if ${this.BaseUrl} is up`, 500)
  }
}

async function resHandler(res: Response) {
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
      errorFetchMsg = "Your session has expired. Reload the page to sign in again."
    } else if (res.status == 403) {
      errorFetchMsg = "You don't have access to this (Admin role required)."
    } else {
      errorFetchMsg = await problemMessage(res)
    }
    console.log(errorFetchMsg)
    throw new HttpServiceError(errorFetchMsg, res.status)
  }
}

/** ProblemDetails (application/problem+json) -> its detail/title, otherwise the raw text */
async function problemMessage(res: Response) {
  const text = await res.text()
  try {
    const problem = JSON.parse(text)
    if (problem?.errors && typeof problem.errors === "object") {
      // model validation errors: { field: [messages] }
      const messages = Object.values(problem.errors as Record<string, string[]>).flat()
      if (messages.length) return messages.join(" ")
    }
    return problem?.detail || problem?.title || text || `${res.status} ${res.statusText}`
  } catch {
    return text || `${res.status} ${res.statusText}`
  }
}

function getHeaders(contentType: string) {
  const headers: HeadersInit = {}
  if (contentType)
    headers["Content-Type"] = contentType

  return headers
}

export class HttpServiceError extends Error {
  status = 0

  constructor (message: string, status: number) {
    super(message)
    this.status = status
  }
}
