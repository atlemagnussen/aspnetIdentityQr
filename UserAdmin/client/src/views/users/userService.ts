import { apiService } from "@db/client/services/apiService.js"
import { UserDto } from "@db/api"

export function getUsers() {
    return apiService.get<Array<UserDto>>("users")
}

export function getUser(id: string) {
    return apiService.get<UserDto>(`users/${id}`)
}

export function getUserRoles(id: string) {
    return apiService.get<Array<string>>(`users/${id}/roles`)
}

export function addRole(id: string, role: string) {
    return apiService.put(`users/${id}/roles`, role as any)
}