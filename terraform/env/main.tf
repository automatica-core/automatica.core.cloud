# One environment (dev / prod) of Automatica.Core.Cloud. Everything here can be recreated, except the
# environment's database (prevent_destroy; the dev one is imported). Shared resources come from ../shared.
#
#   terraform init -backend-config=envs/dev.backend.hcl
#   terraform plan -var-file=envs/dev.tfvars

terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.20"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  backend "azurerm" {}
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = false
    }
  }
  subscription_id                 = var.subscription_id
  storage_use_azuread             = true
  resource_provider_registrations = "none"
}

data "azurerm_client_config" "current" {}

locals {
  name = "automatica-cloud-${var.environment}"
  tags = {
    application = "automatica-cloud"
    environment = var.environment
    managed_by  = "terraform"
  }
  # globally unique names (storage, key vault) get a short stable suffix
  suffix = substr(sha1("${var.subscription_id}-${var.environment}"), 0, 4)
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.name}"
  location = var.location
  tags     = local.tags
}

# --- shared (kept) resources ---

data "azurerm_mssql_server" "shared" {
  name                = var.shared.sql_server_name
  resource_group_name = var.shared.resource_group_name
}

data "azurerm_storage_account" "updates" {
  name                = var.shared.updates_storage_name
  resource_group_name = var.shared.resource_group_name
}

data "azurerm_dns_zone" "remote" {
  name                = var.remote_dns_zone
  resource_group_name = var.shared.resource_group_name
}

data "azurerm_key_vault" "acme" {
  name                = var.frps.cert_key_vault_name
  resource_group_name = var.frps.cert_key_vault_resource_group
}

# --- modules ---

module "network" {
  source              = "../modules/network"
  name                = local.name
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  address_space       = var.vnet_address_space
  tags                = local.tags
}

module "monitoring" {
  source              = "../modules/monitoring"
  name                = local.name
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  tags                = local.tags
}

module "database" {
  source                    = "../modules/database"
  name                      = var.database.name
  server_id                 = data.azurerm_mssql_server.shared.id
  sku_name                  = var.database.sku_name
  max_size_gb               = var.database.max_size_gb
  storage_account_type      = var.database.storage_account_type
  short_term_retention_days = var.database.short_term_retention_days
  app_subnet_id             = module.network.app_subnet_id
  env                       = var.environment
  tags                      = local.tags
}

module "tts_storage" {
  source              = "../modules/tts_storage"
  name                = "stacctts${var.environment}${local.suffix}"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  tags                = local.tags
}

module "speech" {
  source              = "../modules/speech"
  name                = "spch-${local.name}-${local.suffix}"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  sku_name            = var.speech_sku
  tags                = local.tags
}

module "email" {
  source              = "../modules/email"
  name                = local.name
  resource_group_name = azurerm_resource_group.this.name
  email_domain        = var.email_domain
  tags                = local.tags
}

module "key_vault" {
  source              = "../modules/key_vault"
  name                = "kv-acc-${var.environment}-${local.suffix}"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  deployer_object_id  = data.azurerm_client_config.current.object_id
  tags                = local.tags

  extra_secrets = {
    "TextToSpeech--ApiKey"   = module.speech.primary_key
    "Frps--PluginHost"       = module.app.public_host
    "Frps--TokenAuthEnabled" = tostring(var.frps.token_auth_enabled)
  }
}

module "app" {
  source              = "../modules/app_service"
  name                = local.name
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  sku_name            = var.app_service_sku
  subnet_id           = module.network.app_subnet_id
  tags                = local.tags

  sql_connection_string = join(";", [
    "Server=tcp:${data.azurerm_mssql_server.shared.fully_qualified_domain_name},1433",
    "Database=${module.database.name}",
    "Authentication=Active Directory Managed Identity",
    "Encrypt=True",
    "TrustServerCertificate=False",
    "Connect Timeout=30",
  ])

  app_settings = {
    ASPNETCORE_ENVIRONMENT                = var.aspnetcore_environment
    APPLICATIONINSIGHTS_CONNECTION_STRING = module.monitoring.connection_string
    "KeyVault__Uri"                       = "https://kv-acc-${var.environment}-${local.suffix}.vault.azure.net/"
    "App__PublicBaseUrl"                  = "https://${var.custom_domain.enabled ? "${var.custom_domain.record}.${var.custom_domain.zone}" : "app-${local.name}.azurewebsites.net"}"
    "Mail__SenderAddress"                 = module.email.sender_address
    "TextToSpeech__Region"                = module.speech.region
    "RemoteControl__SubscriptionId"       = var.subscription_id
    "RemoteControl__ResourceGroup"        = var.shared.resource_group_name
    "RemoteControl__DnsZoneName"          = var.remote_dns_zone
    "RemoteControl__CNameTarget"          = "${var.frps.dns_record}.${var.remote_dns_zone}"
    "RemoteControl__CNamePrefix"          = var.frps.dns_record
    "Frps__RequireApiKeyMeta"             = tostring(var.frps.require_api_key_meta)
    # https URLs = managed identity (Automatica.Core.Cloud.Storage.BlobContainers / AcsMailSender)
    "ConnectionStrings__AutomaticaCoreUpdateStore"         = trimsuffix(data.azurerm_storage_account.updates.primary_blob_endpoint, "/")
    "ConnectionStrings__AutomaticaTextToSpeechStorage"     = trimsuffix(module.tts_storage.blob_endpoint, "/")
    "ConnectionStrings__AutomaticaAzureEmailCommunication" = module.email.endpoint
  }

  manage_custom_domain              = var.custom_domain.enabled
  custom_domain_zone_name           = var.custom_domain.zone
  custom_domain_zone_resource_group = var.shared.resource_group_name
  custom_domain_record              = var.custom_domain.record
}

module "frps" {
  source               = "../modules/frps"
  name                 = "frps-${var.environment}"
  resource_group_name  = azurerm_resource_group.this.name
  location             = var.location
  subnet_id            = module.network.frps_subnet_id
  vm_size              = var.frps.vm_size
  admin_ssh_public_key = var.frps.admin_ssh_public_key
  ssh_source_cidrs     = var.frps.ssh_source_cidrs
  tcp_port_range       = var.frps.tcp_port_range
  subdomain_host       = "${var.frps.dns_record}.${var.remote_dns_zone}"
  key_vault_id         = module.key_vault.id
  key_vault_name       = module.key_vault.name
  cert_key_vault_id    = data.azurerm_key_vault.acme.id
  cert_key_vault_name  = data.azurerm_key_vault.acme.name
  cert_name            = var.frps.cert_name
  frp_version          = var.frps.frp_version
  frp_sha256           = var.frps.frp_sha256
  tags                 = local.tags
}

# <env>.automaticaremote.com -> frps. Off until cut-over (the record still points at the old VM).
resource "azurerm_dns_a_record" "frps" {
  count               = var.frps.manage_dns ? 1 : 0
  name                = var.frps.dns_record
  zone_name           = data.azurerm_dns_zone.remote.name
  resource_group_name = data.azurerm_dns_zone.remote.resource_group_name
  ttl                 = 300
  records             = [module.frps.public_ip]
}

# --- least-privilege access for the web app's identity ---

locals {
  app_roles = {
    key_vault = { scope = module.key_vault.id, role = "Key Vault Secrets User" }
    updates   = { scope = "${data.azurerm_storage_account.updates.id}/blobServices/default/containers/automaticaupdates", role = "Storage Blob Data Contributor" }
    tts       = { scope = module.tts_storage.id, role = "Storage Blob Data Contributor" }
    dns       = { scope = data.azurerm_dns_zone.remote.id, role = "DNS Zone Contributor" }
    email     = { scope = module.email.communication_service_id, role = "Contributor" }
  }
}

resource "azurerm_role_assignment" "app" {
  for_each             = local.app_roles
  scope                = each.value.scope
  role_definition_name = each.value.role
  principal_id         = module.app.principal_id
}
