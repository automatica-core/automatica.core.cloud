# Cache for synthesized speech. Cores fetch the audio by URL, so the container allows anonymous blob reads
# (no listing). Writes need the app's managed identity; shared keys are disabled.

variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "tags" { type = map(string) }
variable "retention_days" {
  type    = number
  default = 180
}

resource "azurerm_storage_account" "this" {
  name                             = var.name
  resource_group_name              = var.resource_group_name
  location                         = var.location
  account_kind                     = "StorageV2"
  account_tier                     = "Standard"
  account_replication_type         = "LRS"
  https_traffic_only_enabled       = true
  min_tls_version                  = "TLS1_2"
  allow_nested_items_to_be_public  = true
  shared_access_key_enabled        = false
  cross_tenant_replication_enabled = false
  default_to_oauth_authentication  = true
  tags                             = var.tags

  blob_properties {
    delete_retention_policy {
      days = 7
    }
  }
}

resource "azurerm_storage_container" "tts" {
  name                  = "tts"
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "blob"
}

resource "azurerm_storage_management_policy" "expire" {
  storage_account_id = azurerm_storage_account.this.id

  rule {
    name    = "expire-tts"
    enabled = true
    filters {
      blob_types   = ["blockBlob"]
      prefix_match = ["tts/"]
    }
    actions {
      base_blob {
        delete_after_days_since_modification_greater_than = var.retention_days
      }
    }
  }
}

output "id" { value = azurerm_storage_account.this.id }
output "blob_endpoint" { value = azurerm_storage_account.this.primary_blob_endpoint }
