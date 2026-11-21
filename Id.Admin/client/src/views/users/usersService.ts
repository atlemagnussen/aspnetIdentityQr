import { apiService } from "@db/client/services/apiService.js"
import type {
    ClaimDto, PasswordLinkDto, UserCreateDto, UserCreatedDto, UserDetailsDto, UserListItemDto, UserUpdateDto,
} from "@db/api"

const enc = encodeURIComponent

export const getUsers = () => apiService.get<UserListItemDto[]>("users")
export const getUser = (id: string) => apiService.get<UserDetailsDto>(`users/${enc(id)}`)
export const createUser = (dto: UserCreateDto) => apiService.post<UserCreatedDto>("users", dto)
export const updateUser = (id: string, dto: UserUpdateDto) => apiService.put<UserDetailsDto>(`users/${enc(id)}`, dto)
export const deleteUser = (id: string) => apiService.delete(`users/${enc(id)}`)

export const addRole = (id: string, role: string) =>
    apiService.put<UserDetailsDto>(`users/${enc(id)}/roles/${enc(role)}`, {})
export const removeRole = (id: string, role: string) =>
    apiService.delete<UserDetailsDto>(`users/${enc(id)}/roles/${enc(role)}`)

export const addClaim = (id: string, claim: ClaimDto) =>
    apiService.post<UserDetailsDto>(`users/${enc(id)}/claims`, claim)
export const removeClaim = (id: string, claim: ClaimDto) =>
    apiService.delete<UserDetailsDto>(`users/${enc(id)}/claims?type=${enc(claim.type)}&value=${enc(claim.value)}`)

/** until: ISO date, or null = until unlocked */
export const lockUser = (id: string, until: string | null) =>
    apiService.post<UserDetailsDto>(`users/${enc(id)}/lock`, { until })
export const unlockUser = (id: string) => apiService.post<UserDetailsDto>(`users/${enc(id)}/unlock`)
export const resetTwoFactor = (id: string) => apiService.post<UserDetailsDto>(`users/${enc(id)}/reset-2fa`)
export const signOutEverywhere = (id: string) => apiService.post<UserDetailsDto>(`users/${enc(id)}/sign-out-everywhere`)
export const createPasswordLink = (id: string) => apiService.post<PasswordLinkDto>(`users/${enc(id)}/password-link`)

export const removeLogin = (id: string, provider: string, key: string) =>
    apiService.delete<UserDetailsDto>(`users/${enc(id)}/logins?provider=${enc(provider)}&key=${enc(key)}`)
export const removePasskey = (id: string, credentialId: string) =>
    apiService.delete<UserDetailsDto>(`users/${enc(id)}/passkeys/${enc(credentialId)}`)

/** LockoutEnd far in the future = locked until unlocked (server uses DateTimeOffset.MaxValue) */
export const isIndefinite = (lockoutEnd?: string | null) =>
    !!lockoutEnd && new Date(lockoutEnd).getUTCFullYear() >= 9000
