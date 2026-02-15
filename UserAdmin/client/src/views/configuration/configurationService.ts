import { apiService } from "@db/client/services/apiService.js"
import { Client } from "@db/api"

export function getClients() {
    return apiService.get<Array<Client>>("clients")
}