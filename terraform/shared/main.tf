# Shared, long-lived resources that are KEPT (never recreated):
# - SQL server automaticacorecloud (holds every environment's database)
# - update/plugin package storage automaticaupdates (cores download packages by their stored URL)
# Both are imported (imports.tf) and guarded with prevent_destroy. Environment stacks reference them via data sources.

terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.20"
    }
  }

  backend "azurerm" {}
}

provider "azurerm" {
  features {}
  subscription_id                 = var.subscription_id
  storage_use_azuread             = true
  resource_provider_registrations = "none"
}

variable "subscription_id" { type = string }
variable "resource_group_name" {
  type    = string
  default = "automatica"
}
variable "location" {
  type    = string
  default = "westeurope"
}
variable "sql_server_name" {
  type    = string
  default = "automaticacorecloud"
}
variable "sql_admin_login" {
  type        = string
  default     = "p3root"
  description = "Existing SQL login (break-glass). Its password is not managed here."
}
variable "sql_entra_admin" {
  type = object({
    login_username = string
    object_id      = string
  })
  description = "Entra group/user that administers SQL (needed to create the apps' managed-identity users)."
}
variable "updates_storage_name" {
  type    = string
  default = "automaticaupdates"
}

data "azurerm_client_config" "current" {}

resource "azurerm_mssql_server" "this" {
  name                          = var.sql_server_name
  resource_group_name           = var.resource_group_name
  location                      = var.location
  version                       = "12.0"
  administrator_login           = var.sql_admin_login
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true # access is limited by virtual network rules, see env/

  azuread_administrator {
    login_username              = var.sql_entra_admin.login_username
    object_id                   = var.sql_entra_admin.object_id
    tenant_id                   = data.azurerm_client_config.current.tenant_id
    azuread_authentication_only = false
  }

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [administrator_login_password, tags]
  }
}

resource "azurerm_storage_account" "updates" {
  name                             = var.updates_storage_name
  resource_group_name              = var.resource_group_name
  location                         = var.location
  account_kind                     = "StorageV2"
  account_tier                     = "Standard"
  account_replication_type         = "GRS"
  https_traffic_only_enabled       = true
  min_tls_version                  = "TLS1_2"
  allow_nested_items_to_be_public  = true # the package container is anonymously readable (cores download from it)
  cross_tenant_replication_enabled = false

  blob_properties {
    delete_retention_policy {
      days = 30
    }
    container_delete_retention_policy {
      days = 30
    }
  }

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags]
  }
}

resource "azurerm_storage_container" "updates" {
  name                  = "automaticaupdates"
  storage_account_id    = azurerm_storage_account.updates.id
  container_access_type = "blob"

  lifecycle {
    prevent_destroy = true
  }
}

output "sql_server_id" { value = azurerm_mssql_server.this.id }
output "sql_server_fqdn" { value = azurerm_mssql_server.this.fully_qualified_domain_name }
output "updates_storage_id" { value = azurerm_storage_account.updates.id }
output "updates_blob_endpoint" { value = azurerm_storage_account.updates.primary_blob_endpoint }
