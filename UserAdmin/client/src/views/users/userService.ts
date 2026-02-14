import { apiService } from "@db/client/services/apiService.js"
import { UserDto } from "@db/api"

export function getUsers() {
    return apiService.get<Array<UserDto>>("users")
}