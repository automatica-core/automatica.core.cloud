# Linux App Service for Automatica.Core.Cloud (API + Angular SPA in wwwroot).
# - system-assigned identity for SQL (Entra auth), Key Vault, Storage, DNS and email; no secrets in app settings
# - HTTPS only, TLS >= 1.2, FTP and basic-auth publishing off, outbound through the VNet
# - optional custom host name with an App Service managed certificate (manage_custom_domain)

variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "sku_name" { type = string }
variable "subnet_id" { type = string }
variable "app_settings" { type = map(string) }
variable "sql_connection_string" { type = string }
variable "tags" { type = map(string) }

variable "manage_custom_domain" {
  type    = bool
  default = false
}
variable "custom_domain_zone_name" { type = string }
variable "custom_domain_zone_resource_group" { type = string }
variable "custom_domain_record" {
  type        = string
  description = "Record name in the zone, e.g. cloud-dev -> cloud-dev.automaticacore.com"
}

resource "azurerm_service_plan" "this" {
  name                = "asp-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku_name
  tags                = var.tags
}

resource "azurerm_linux_web_app" "this" {
  name                = "app-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.this.id

  https_only                                     = true
  public_network_access_enabled                  = true
  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false
  client_affinity_enabled                        = false
  virtual_network_subnet_id                      = var.subnet_id
  tags                                           = var.tags

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                         = true
    http2_enabled                     = true
    minimum_tls_version               = "1.2"
    scm_minimum_tls_version           = "1.2"
    ftps_state                        = "Disabled"
    remote_debugging_enabled          = false
    vnet_route_all_enabled            = true
    health_check_path                 = "/healthz"
    health_check_eviction_time_in_min = 10

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = merge(var.app_settings, {
    WEBSITE_RUN_FROM_PACKAGE = "1"
  })

  connection_string {
    name  = "AutomaticaCoreCloudSql"
    type  = "SQLAzure"
    value = var.sql_connection_string
  }

  # no http_logs: core requests carry the api key in the url path (v1/v2 routes), so access logs would store it
  logs {
    detailed_error_messages = false
    failed_request_tracing  = false
  }

  lifecycle {
    # the deployment pipeline sets the package; terraform must not fight over it
    ignore_changes = [app_settings["WEBSITE_RUN_FROM_PACKAGE"]]
  }
}

# --- custom domain (turned on at cut-over) ---

data "azurerm_dns_zone" "custom" {
  count               = var.manage_custom_domain ? 1 : 0
  name                = var.custom_domain_zone_name
  resource_group_name = var.custom_domain_zone_resource_group
}

resource "azurerm_dns_cname_record" "this" {
  count               = var.manage_custom_domain ? 1 : 0
  name                = var.custom_domain_record
  zone_name           = data.azurerm_dns_zone.custom[0].name
  resource_group_name = data.azurerm_dns_zone.custom[0].resource_group_name
  ttl                 = 300
  record              = azurerm_linux_web_app.this.default_hostname
}

resource "azurerm_dns_txt_record" "asuid" {
  count               = var.manage_custom_domain ? 1 : 0
  name                = "asuid.${var.custom_domain_record}"
  zone_name           = data.azurerm_dns_zone.custom[0].name
  resource_group_name = data.azurerm_dns_zone.custom[0].resource_group_name
  ttl                 = 300

  record {
    value = azurerm_linux_web_app.this.custom_domain_verification_id
  }
}

resource "azurerm_app_service_custom_hostname_binding" "this" {
  count               = var.manage_custom_domain ? 1 : 0
  hostname            = trimsuffix(azurerm_dns_cname_record.this[0].fqdn, ".")
  app_service_name    = azurerm_linux_web_app.this.name
  resource_group_name = var.resource_group_name
  depends_on          = [azurerm_dns_txt_record.asuid]

  lifecycle {
    ignore_changes = [ssl_state, thumbprint]
  }
}

resource "azurerm_app_service_managed_certificate" "this" {
  count                      = var.manage_custom_domain ? 1 : 0
  custom_hostname_binding_id = azurerm_app_service_custom_hostname_binding.this[0].id
}

resource "azurerm_app_service_certificate_binding" "this" {
  count               = var.manage_custom_domain ? 1 : 0
  hostname_binding_id = azurerm_app_service_custom_hostname_binding.this[0].id
  certificate_id      = azurerm_app_service_managed_certificate.this[0].id
  ssl_state           = "SniEnabled"
}

output "id" { value = azurerm_linux_web_app.this.id }
output "name" { value = azurerm_linux_web_app.this.name }
output "default_hostname" { value = azurerm_linux_web_app.this.default_hostname }
output "principal_id" { value = azurerm_linux_web_app.this.identity[0].principal_id }
output "public_host" {
  value = var.manage_custom_domain ? "${var.custom_domain_record}.${var.custom_domain_zone_name}" : azurerm_linux_web_app.this.default_hostname
}
