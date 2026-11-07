# Azure Communication Services email. The app sends with its managed identity (no connection string).
# email_domain = null uses an Azure managed domain (no DNS work); a custom domain needs its verification
# records added to the DNS zone once (outputs.verification_records, see terraform/README.md).

variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "email_domain" {
  type    = string
  default = null
}
variable "sender_username" {
  type    = string
  default = "cloud"
}
variable "sender_display_name" {
  type    = string
  default = "Automatica Cloud"
}
variable "tags" { type = map(string) }

resource "azurerm_email_communication_service" "this" {
  name                = "acs-mail-${var.name}"
  resource_group_name = var.resource_group_name
  data_location       = "Europe"
  tags                = var.tags
}

resource "azurerm_email_communication_service_domain" "this" {
  name              = var.email_domain == null ? "AzureManagedDomain" : var.email_domain
  email_service_id  = azurerm_email_communication_service.this.id
  domain_management = var.email_domain == null ? "AzureManaged" : "CustomerManaged"
  tags              = var.tags
}

resource "azurerm_email_communication_service_domain_sender_username" "this" {
  name                    = var.sender_username
  email_service_domain_id = azurerm_email_communication_service_domain.this.id
  display_name            = var.sender_display_name
}

resource "azurerm_communication_service" "this" {
  name                = "acs-${var.name}"
  resource_group_name = var.resource_group_name
  data_location       = "Europe"
  tags                = var.tags
}

resource "azurerm_communication_service_email_domain_association" "this" {
  communication_service_id = azurerm_communication_service.this.id
  email_service_domain_id  = azurerm_email_communication_service_domain.this.id
}

output "communication_service_id" { value = azurerm_communication_service.this.id }
output "endpoint" { value = "https://${azurerm_communication_service.this.hostname}" }
output "sender_address" {
  value = "${var.sender_username}@${azurerm_email_communication_service_domain.this.from_sender_domain}"
}
output "verification_records" { value = azurerm_email_communication_service_domain.this.verification_records }
