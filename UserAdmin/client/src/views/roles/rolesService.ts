import { apiService } from "@db/client/services/apiService.js"
import type { RoleDto } from "@db/api"

const enc = encodeURIComponent

export const getRoles = () => apiService.get<RoleDto[]>("roles")
export const createRole = (name: string) => apiService.post<RoleDto>("roles", { name })
export const renameRole = (id: string, name: string) => apiService.put<RoleDto>(`roles/${enc(id)}`, { name })
export const deleteRole = (id: string) => apiService.delete(`roles/${enc(id)}`)
