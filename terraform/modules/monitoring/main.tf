variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "retention_days" {
  type    = number
  default = 30
}
variable "tags" { type = map(string) }

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = var.retention_days
  tags                = var.tags
}

resource "azurerm_application_insights" "this" {
  name                = "appi-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.this.id
  application_type    = "web"
  tags                = var.tags
}

output "connection_string" {
  value     = azurerm_application_insights.this.connection_string
  sensitive = true
}
output "workspace_id" { value = azurerm_log_analytics_workspace.this.id }
