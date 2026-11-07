# The environment's database on the shared (kept) SQL server. The dev database already exists and is
# imported (see env/imports.tf); prevent_destroy guards it from ever being replaced or deleted by terraform.

variable "name" { type = string }
variable "server_id" { type = string }
variable "sku_name" { type = string }
variable "max_size_gb" { type = number }
variable "storage_account_type" { type = string }
variable "short_term_retention_days" { type = number }
variable "tags" { type = map(string) }
variable "app_subnet_id" { type = string }
variable "env" { type = string }

resource "azurerm_mssql_database" "this" {
  name                 = var.name
  server_id            = var.server_id
  sku_name             = var.sku_name
  max_size_gb          = var.max_size_gb
  collation            = "SQL_Latin1_General_CP1_CI_AS"
  storage_account_type = var.storage_account_type
  tags                 = var.tags

  short_term_retention_policy {
    retention_days = var.short_term_retention_days
  }

  lifecycle {
    prevent_destroy = true
    # values the portal manages or that differ in format after import; never worth a replace
    ignore_changes = [license_type, maintenance_configuration_name, read_scale, zone_redundant, tags]
  }
}

# SQL is reachable only from the app subnet (service endpoint), not from "all Azure services".
resource "azurerm_mssql_virtual_network_rule" "app" {
  name      = "app-${var.env}"
  server_id = var.server_id
  subnet_id = var.app_subnet_id
}

output "id" { value = azurerm_mssql_database.this.id }
output "name" { value = azurerm_mssql_database.this.name }
