import HttpService from "@db/client/services/httpService.js"
import { Database } from "@db/client/models/Database.js"
import { UserDbRole } from "@db/client/models/UserDbRole.js"
import { MigrateOptions } from "@db/client/models/MigrateOptions.js"
import { GraphApiUser } from "@db/client/models/GraphUser.js"
import { AssignDbRoleDTO } from "@db/client/models/AssignDbRole.js"

const dbService = new HttpService("api")

export function getDatabases() {
    return dbService.get<Array<Database>>("db")
}

// permissions
export function getRoles(dbName: string) {
    return dbService.get<Array<UserDbRole>>(`permissions/${dbName}`)
}
export function assignRole(dbName: string, assignRole: AssignDbRoleDTO) {
    return dbService.post(`permissions/${dbName}`, assignRole)
}

// migrations
export function script(options: MigrateOptions) {
    return dbService.post<string>(`migration/script`, options)
}
export function migrate(options: MigrateOptions) {
    return dbService.post<string>(`migration/migrateDb`, options)
}

// user
export function searchUser(query: string) {
    return dbService.get<Array<GraphApiUser>>(`user/search?query=${query}`)
}