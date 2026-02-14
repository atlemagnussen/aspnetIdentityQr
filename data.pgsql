INSERT INTO public."IdentityResources" ("Enabled", "Name", "DisplayName", "Description", "Required", "Emphasize", "ShowInDiscoveryDocument", "Created", "NonEditable")
values (true, 'roles', 'Roles', 'Allow the service access to your user roles', true, true, true, NOW(), FALSE)

INSERT INTO public."IdentityResourceClaims" ("IdentityResourceId", "Type")
values (6, 'role')
