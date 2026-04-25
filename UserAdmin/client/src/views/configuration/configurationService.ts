import { apiService } from "@db/client/services/apiService.js"
import { Client, ClientUrlPatch } from "@db/api"

export function getClients() {
    return apiService.get<Array<Client>>("clients")
}

export function addRedirectUrl(clientId: string, url: string) {
  const payload: ClientUrlPatch = { url }
  return apiService.put<Client>(`clients/${clientId}/redirecturls`, payload)
}