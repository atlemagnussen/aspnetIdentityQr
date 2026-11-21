import { apiService } from "@db/client/services/apiService.js"
import type {
    ClientDto, ClientSecretCreateDto, ClientSecretCreatedDto, ClientSecretDto, ClientSummaryDto, ScopeDto,
} from "@db/api"

const enc = encodeURIComponent

export const getClients = () => apiService.get<ClientSummaryDto[]>("clients")
export const getClient = (clientId: string) => apiService.get<ClientDto>(`clients/${enc(clientId)}`)
export const createClient = (dto: ClientDto) => apiService.post<ClientDto>("clients", dto)
export const updateClient = (clientId: string, dto: ClientDto) => apiService.put<ClientDto>(`clients/${enc(clientId)}`, dto)
export const deleteClient = (clientId: string) => apiService.delete(`clients/${enc(clientId)}`)

export const getSecrets = (clientId: string) => apiService.get<ClientSecretDto[]>(`clients/${enc(clientId)}/secrets`)
export const createSecret = (clientId: string, dto: ClientSecretCreateDto) =>
    apiService.post<ClientSecretCreatedDto>(`clients/${enc(clientId)}/secrets`, dto)
export const deleteSecret = (clientId: string, secretId: number) =>
    apiService.delete(`clients/${enc(clientId)}/secrets/${secretId}`)

export const getScopes = () => apiService.get<ScopeDto[]>("scopes")

export interface GrantTypeInfo {
    value: string
    label: string
    legacy?: boolean
}

export const knownGrantTypes: GrantTypeInfo[] = [
    { value: "authorization_code", label: "Authorization code: users sign in (use with PKCE)" },
    { value: "client_credentials", label: "Client credentials: machine-to-machine, no user" },
    { value: "urn:ietf:params:oauth:grant-type:device_code", label: "Device code: TVs, CLIs" },
    { value: "urn:openid:params:grant-type:ciba", label: "CIBA: backchannel authentication" },
    { value: "implicit", label: "Implicit", legacy: true },
    { value: "hybrid", label: "Hybrid", legacy: true },
    { value: "password", label: "Resource owner password", legacy: true },
]

/** e.g. 3600 -> "1 h", 1296000 -> "15 d" */
export function humanSeconds(seconds?: number | null) {
    if (!seconds) return ""
    const units: [number, string][] = [[86400, "d"], [3600, "h"], [60, "min"]]
    for (const [size, unit] of units)
        if (seconds >= size && seconds % size === 0) return `${seconds / size} ${unit}`
    for (const [size, unit] of units)
        if (seconds >= size) return `≈ ${(seconds / size).toFixed(1)} ${unit}`
    return `${seconds} s`
}

export const validateAbsoluteUrl = (value: string) => {
    try {
        new URL(value)
        return null
    } catch {
        return "Must be an absolute URL, e.g. https://app.example.com/signin-oidc"
    }
}

export const validateOrigin = (value: string) => {
    try {
        const url = new URL(value)
        if (url.protocol !== "http:" && url.protocol !== "https:") return "Must be http or https."
        if (url.pathname !== "/" || url.search || url.hash) return `Only the origin, e.g. ${url.origin}`
        return null
    } catch {
        return "Must be an origin, e.g. https://app.example.com"
    }
}
