import { DatabaseRole } from "./DatabaseRole.js"

export type AssignDbRoleDTO = {
    userName: string
    roleName: DatabaseRole
}