variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "sku_name" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_cognitive_account" "this" {
  name                  = var.name
  resource_group_name   = var.resource_group_name
  location              = var.location
  kind                  = "SpeechServices"
  sku_name              = var.sku_name
  custom_subdomain_name = var.name
  # the Speech SDK in the app authenticates with the key, which is written to Key Vault (TextToSpeech--ApiKey)
  local_auth_enabled = true
  tags               = var.tags
}

output "region" { value = azurerm_cognitive_account.this.location }
output "primary_key" {
  value     = azurerm_cognitive_account.this.primary_access_key
  sensitive = true
}
