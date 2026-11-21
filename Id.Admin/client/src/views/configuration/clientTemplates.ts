import { AccessTokenType, TokenExpiration, TokenUsage, type ClientDto } from "@db/api"

/** Defaults for a new client: mostly Duende's, but with one-time (rotating) refresh tokens */
export function blankClient(clientId = ""): ClientDto {
    return {
        clientId,
        clientName: null,
        description: null,
        clientUri: null,
        logoUri: null,
        enabled: true,
        protocolType: "oidc",
        allowedGrantTypes: ["authorization_code"],
        requireClientSecret: true,
        requirePkce: true,
        allowPlainTextPkce: false,
        requireRequestObject: false,
        requireDPoP: false,
        requirePushedAuthorization: false,
        allowAccessTokensViaBrowser: false,
        allowOfflineAccess: false,
        enableLocalLogin: true,
        identityProviderRestrictions: [],
        requireConsent: false,
        allowRememberConsent: true,
        consentLifetime: null,
        redirectUris: [],
        postLogoutRedirectUris: [],
        allowedCorsOrigins: [],
        frontChannelLogoutUri: null,
        frontChannelLogoutSessionRequired: true,
        backChannelLogoutUri: null,
        backChannelLogoutSessionRequired: true,
        initiateLoginUri: null,
        allowedScopes: [],
        accessTokenType: AccessTokenType.JWT,
        accessTokenLifetime: 3600,
        identityTokenLifetime: 300,
        authorizationCodeLifetime: 300,
        absoluteRefreshTokenLifetime: 2592000,
        slidingRefreshTokenLifetime: 1296000,
        refreshTokenUsage: TokenUsage.ONE_TIME_ONLY,
        refreshTokenExpiration: TokenExpiration.ABSOLUTE,
        updateAccessTokenClaimsOnRefresh: false,
        includeJwtId: true,
        alwaysIncludeUserClaimsInIdToken: false,
        userSsoLifetime: null,
        coordinateLifetimeWithUserSession: false,
        allowedIdentityTokenSigningAlgorithms: [],
        claims: [],
        alwaysSendClientClaims: false,
        clientClaimsPrefix: "client_",
        pairWiseSubjectSalt: null,
        properties: [],
    }
}

export interface ClientTemplate {
    key: string
    label: string
    description: string
    create: (clientId: string) => ClientDto
}

export const clientTemplates: ClientTemplate[] = [
    {
        key: "web",
        label: "Web app",
        description: "Server-side app (ASP.NET, etc.): authorization code + PKCE with a client secret.",
        create: id => ({
            ...blankClient(id),
            allowedGrantTypes: ["authorization_code"],
            requireClientSecret: true,
            allowedScopes: ["openid", "profile"],
        }),
    },
    {
        key: "spa",
        label: "Single-page app",
        description: "Browser app (Lit, React, oidc-client): public client, authorization code + PKCE, no secret. Needs CORS origins.",
        create: id => ({
            ...blankClient(id),
            allowedGrantTypes: ["authorization_code"],
            requireClientSecret: false,
            allowedScopes: ["openid", "profile"],
        }),
    },
    {
        key: "m2m",
        label: "Machine-to-machine",
        description: "Service calling an API without a user: client credentials with a secret. Pick API scopes only.",
        create: id => ({
            ...blankClient(id),
            allowedGrantTypes: ["client_credentials"],
            requireClientSecret: true,
            allowedScopes: [],
        }),
    },
    {
        key: "blank",
        label: "Blank",
        description: "Sensible defaults (rotating refresh tokens); configure everything yourself.",
        create: id => blankClient(id),
    },
]
