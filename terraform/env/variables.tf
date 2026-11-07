# No variable here is a secret. Secrets are generated into Key Vault or set there by hand (README.md).

variable "subscription_id" { type = string }

variable "environment" {
  type = string
  validation {
    condition     = can(regex("^[a-z]{2,8}$", var.environment))
    error_message = "environment must be 2-8 lowercase letters (dev, prod, ...)."
  }
}

variable "location" {
  type    = string
  default = "westeurope"
}

variable "aspnetcore_environment" {
  type    = string
  default = "Production"
}

variable "shared" {
  type = object({
    resource_group_name  = string
    sql_server_name      = string
    updates_storage_name = string
  })
  default = {
    resource_group_name  = "automatica"
    sql_server_name      = "automaticacorecloud"
    updates_storage_name = "automaticaupdates"
  }
}

variable "vnet_address_space" {
  type    = string
  default = "10.40.0.0/16"
}

variable "database" {
  type = object({
    name                      = string
    sku_name                  = string
    max_size_gb               = number
    storage_account_type      = string
    short_term_retention_days = number
  })
}

variable "app_service_sku" {
  type    = string
  default = "B1"
}

variable "speech_sku" {
  type    = string
  default = "F0"
}

variable "email_domain" {
  type        = string
  default     = null
  description = "null = Azure managed sender domain; a custom domain needs DNS verification once."
}

variable "remote_dns_zone" {
  type    = string
  default = "automaticaremote.com"
}

variable "custom_domain" {
  type = object({
    enabled = bool
    zone    = string
    record  = string
  })
  description = "Cloud host name; enable at cut-over (imports the existing records, see imports.tf)."
}

variable "frps" {
  type = object({
    vm_size                       = string
    admin_ssh_public_key          = string
    ssh_source_cidrs              = list(string)
    tcp_port_range                = object({ start = number, end = number })
    dns_record                    = string
    manage_dns                    = bool
    cert_key_vault_name           = string
    cert_key_vault_resource_group = string
    cert_name                     = string
    frp_version                   = string
    frp_sha256                    = string
    token_auth_enabled            = bool
    require_api_key_meta          = bool
  })
}

variable "existing" {
  type = object({
    database_id     = optional(string)
    frps_a_record   = optional(string)
    cloud_cname     = optional(string)
    cloud_asuid_txt = optional(string)
  })
  default     = {}
  description = "Resource IDs of existing resources to adopt (import) instead of creating them."
}
