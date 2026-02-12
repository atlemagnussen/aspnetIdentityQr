export type GraphApiUser = {
    id: string
    accountEnabled: boolean
    userPrincipalName: string
    userType?: string
    mail: string
    displayName?: string
    givenName?: string
    surname?: string
    department?: string
    jobTitle?: string
    usageLocation?: string
    officeLocation?: string
    isGuestUser: boolean
}