import { DatabaseType } from "./DatabaseType.js";

export type Database = {
    id?: number
    name: string
    type: DatabaseType
    connectionStringName: string
    server?: string | null
    serverReplica?: string | null
    isOnPremise?: boolean
    customerId?: number
    created?: string
    comment?: string | null
    version?: string
    updated?: string
};