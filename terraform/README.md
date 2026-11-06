# Automatica.Core.Cloud infrastructure

Nothing here has been applied yet. Only `terraform validate` has been run.

## Layout

| Path | What it is |
|---|---|
| `shared/` | Long-lived resources that are **kept**: the SQL server `automaticacorecloud` and the package storage `automaticaupdates`. Both are imported and protected with `prevent_destroy`. Applied by hand only. |
| `env/` | One environment (`envs/dev.tfvars`, `envs/prod.tfvars`). Each environment gets its own state file. Everything in it can be recreated, **except the environment's database**, which is protected with `prevent_destroy` (the dev database is imported). |
| `modules/` | `app_service`, `database`, `key_vault`, `frps`, `email`, `speech`, `tts_storage`, `monitoring`, `network` |
| `../pipelines/cloud.yml` | Azure DevOps build and deploy pipeline. It is **not created in DevOps yet**. |

State lives in storage account `automaticatfcloud`, container `tf-container`. The keys are `automatica-cloud/shared.tfstate`, `automatica-cloud/dev.tfstate` and `automatica-cloud/prod.tfstate`, and access uses Entra auth (`use_azuread_auth`). The old state (`automatica-cloud/automatica-cloud.terraform.tfstate`, which holds the old dev VM) is left alone and only goes away at cleanup.

## What changes compared with today

- **Web app**
  - New Linux App Service per environment, running .NET 10.
  - System-assigned identity. **No secrets in app settings.**
  - SQL uses Entra auth (`Authentication=Active Directory Managed Identity`).
  - Storage and ACS email use the identity.
  - Everything else comes from Key Vault (`KeyVault__Uri`).
  - HTTPS only, TLS 1.2 minimum, FTP and basic-auth publishing off.
  - Outbound traffic goes through the VNet.
- **SQL**
  - Reachable only from the environment's app subnet (VNet rule plus service endpoint).
  - An Entra admin is added on the server.
  - `AllowAllWindowsAzureIps` and the old 2023 client IP rules are removed by hand at cleanup (step 8).
- **frps VM**
  - Ubuntu 24.04, Trusted Launch, Standard public IP.
  - NSG allows only 7000 (frpc), 80/443 (nginx) and the tunnel range 1024-2047. There is no SSH unless `ssh_source_cidrs` is set.
  - Dashboard is bound to 127.0.0.1.
  - Log level is `info`.
  - The frp download is pinned and checked against its sha256.
  - Secrets (auth token, plugin secret, dashboard password) are read from Key Vault at service start. They are not in custom data and not in the plan.
  - Every login and proxy is checked by the cloud (`POST /webapi/v1/frps/handler/{secret}`).
- **Key Vault per environment**
  - RBAC, purge protection.
  - Generated secrets: `Jwt--SigningKey`, `Frps--Token`, `Frps--PluginSecret`, `Frps--DashboardPassword`.
  - Written from other resources: `TextToSpeech--ApiKey`, `Frps--PluginHost`, `Frps--TokenAuthEnabled`.
- **TTS storage** has shared keys disabled, anonymous read of single blobs only, and a 180-day expiry.
- **Package storage** (`automaticaupdates`, kept):
  - HTTPS only (it allows plain HTTP today).
  - TLS 1.2.
  - 30-day soft delete for blobs and containers.
  - The container stays anonymously readable, because cores download from it.

## Secrets: what Patrik sets by hand

Never put these in tfvars. Set them in the environment's Key Vault (`terraform output key_vault_name`):

| Secret | Value |
|---|---|
| `License--PrivateKeyPassword` | Must be the secret part that was hard-coded in `LicenseManager.cs` before this change (see git history), so that existing license signing keys still decrypt. That value is public, so plan a signing-key rotation, which needs a core release. |
| `Seed--AdminPassword` | Optional, at least 12 characters. It is used only for a fresh database, or to give the admin a password again after the leaked seed password has been disabled. |

```bash
az keyvault secret set --vault-name <kv> --name License--PrivateKeyPassword --value '<value>'
```

## Runbook

### 0. Prerequisites

- Terraform ≥ 1.7 and `az login` into tenant `64ebf03d-…`, subscription `e458a87b-…`.
- The identity running terraform needs:
  - **Storage Blob Data Contributor** on `automaticatfcloud` for state. Nobody has it today.
  - **Contributor** plus **Role Based Access Control Administrator** (or Owner) on the subscription, or on the env resource groups plus the shared scopes. Terraform creates role assignments.
- Turn on blob versioning on `automaticatfcloud` before the first apply:
  `az storage account blob-service-properties update -g automatica -n automaticatfcloud --enable-versioning true`
- Create an Entra group for SQL admins and put its object id in `shared/shared.tfvars`.
- Put the frps SSH **public** key in `env/envs/*.tfvars`.

### 1. Shared stack (imports, nothing is recreated)

```bash
cd terraform/shared
terraform init -backend-config=backend.hcl
terraform plan -var-file=shared.tfvars
```

The plan must show **3 imports and only in-place updates**: TLS, HTTPS only, soft delete, Entra admin. It must show **no destroy and no replace**. If anything says "must be replaced", stop. `prevent_destroy` would also abort the apply.

```bash
terraform apply -var-file=shared.tfvars
```

Check that the cores still download updates. Package URLs are `https://automaticaupdates.blob.core.windows.net/…`, and plain HTTP is now refused.

### 2. Environment, phase 1 (runs next to the old app)

```bash
cd terraform/env
terraform init -backend-config=envs/dev.backend.hcl
terraform plan -var-file=envs/dev.tfvars
```

The plan imports `automaticacorecloud-dev` and must not replace it. Everything else is new. `custom_domain.enabled` and `frps.manage_dns` stay `false`, so DNS is not touched yet.

```bash
terraform apply -var-file=envs/dev.tfvars
```

### 3. Key Vault secrets

Set the secrets from the table above.

### 4. Database access for the app identity

Connect to the database as the Entra admin (Azure Data Studio / sqlcmd with `-G`), then:

```sql
CREATE USER [app-automatica-cloud-dev] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [app-automatica-cloud-dev];  -- the app runs EF migrations at startup
```

Your own client IP needs a temporary firewall rule for this. Remove it afterwards.

### 5. Deploy the app

Use the pipeline, or by hand:

```bash
dotnet publish Automatica.Core.Cloud -c Release -o out
cd out && zip -r ../app.zip .
az webapp deploy -g rg-automatica-cloud-dev -n app-automatica-cloud-dev --src-path ../app.zip --type zip
```

The first start does the following:

1. Applies migration `AddSecurityHardening`. It is additive: new columns and tables, plus `UPDATE Users SET Enabled = 1`, because the old app never checked `Enabled`.
2. Replaces the leaked seeded `sa` ApiKey.
3. Disables the leaked seeded password.
4. Hashes all CoreServer ApiKeys.

Smoke test:

```bash
curl https://app-automatica-cloud-dev.azurewebsites.net/healthz
```

Then log in to the UI. Then point one test core at the new host, or wait for the cut-over.

### 6. Cut-over (DNS)

In `envs/dev.tfvars`:

```hcl
custom_domain = { enabled = true, zone = "automaticacore.com", record = "cloud-dev" }
frps = { ... manage_dns = true ... }
```

Then plan and apply. The plan imports the existing `cloud-dev` CNAME, the `asuid.cloud-dev` TXT and the `dev` A record, then repoints them. It binds the host name and issues a managed certificate. **This is the moment the cores switch.** DNS TTL is 300 s.

After that, restart frps once so it picks up `Frps--PluginHost` with the custom host name:

```bash
az vm run-command invoke -g rg-automatica-cloud-dev -n vm-frps-dev --command-id RunShellScript --scripts "systemctl restart frps"
```

### 7. frps token (after the cores ship it)

The cloud already returns `frpToken` in the `createRemoteConnect` response. Once released cores send `token` and `meta_apikey` (see the contract note in the side-session notes), turn on enforcement:

1. Set `frps.token_auth_enabled = true` and `frps.require_api_key_meta = true`.
2. Apply.
3. Restart frps.

Until then, frps runs without a token, and the cloud plugin decides alone:

- Login: the `user` must be a known ServerGuid.
- New proxy: the subdomain or port must be one the cloud assigned to that server.

### 8. Cleanup (after a few quiet days)

- Delete the old web app `automatica-develop` (RG `automatica-develop`) and RG `automatica-cloud-dev`. That removes the old VM, the retired Basic public IP and the NSG that allowed everything. Then delete the old state blob `automatica-cloud/automatica-cloud.terraform.tfstate`.
- SQL firewall: delete `AllowAllWindowsAzureIps` and the four `ClientIPAddress_2023-*` rules.
- RBAC:
  - Remove **DNS Zone Contributor on the whole subscription** from the old web app identity.
  - Scope down `automatica-core-automatica-e458…` (appId `3b4105cd…`, **Contributor on the subscription**) to what the pipeline needs.
  - Delete the three assignments of deleted principals.
- Delete the expired client secret of app `automatica-core-dev` (`2ad684d4…`), and remove its values from your local `Properties/launchSettings.json`.
- Dangling DNS records:
  - `cloud` and `dev-cloud` in `automaticacore.com` point to the deleted `automatica-core-cloud.azurewebsites.net`, so **anyone can take them over**. Delete them now, or let the prod apply (`custom_domain.enabled = true`) replace `cloud`.
  - Also clean up `dev-remote`, `home.dev-remote` and `cdnverify.demo`, and in `automaticaremote.com` the records `demo` and `remote`.
- `automaticabinaries/automatica-pi-images` is publicly readable. Check that this is intended.

### Rollback

Until step 8 the old app and VM still exist. Point DNS back by setting `custom_domain.enabled` and `frps.manage_dns` to `false`, then fixing the record targets by hand. The migration is additive, but note:

- Users who log in on the new app get a new password hash that the old app cannot verify.
- Keys rotated on the new app are stored hashed only.

## Azure DevOps pipeline (not set up)

`pipelines/cloud.yml`:

- **Build:** Angular, tests, publish, and terraform fmt/validate.
- **Per environment:**
  1. `terraform plan`
  2. deployment job on environment `automatica-cloud-<env>`, where approvals are configured
  3. `terraform apply` of exactly that plan
  4. zip deploy of the app

Branches: `develop` deploys to dev, `master` deploys to prod.

Setup:

1. Create two **workload identity federation** service connections, `automatica-cloud-dev` and `automatica-cloud-prod`, with the roles from step 0. Don't use client secrets.
2. Create the environments `automatica-cloud-dev` and `automatica-cloud-prod` with an approval check (at least on prod).
3. Create the pipeline from `pipelines/cloud.yml`.
4. The old `azure-pipelines.yml` (frps Docker image and terraform artifact for the old release) is superseded. Disable that pipeline.
5. The old CI variable group that held the leaked `sa` ApiKey (`9b47…`) is dead after the first start of the new app. Delete the variable and give the CLI a **deploy key** instead (UI → Deploy keys, scope `DeployPlugin`/`DeployUpdate`/`DeployDocker`, branches `develop` or `master`).
