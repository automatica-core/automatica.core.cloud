output "resource_group" { value = azurerm_resource_group.this.name }
output "web_app_name" { value = module.app.name }
output "web_app_default_hostname" { value = module.app.default_hostname }
output "web_app_principal_id" {
  description = "Create the SQL user for this identity (README: 'Database access')."
  value       = module.app.principal_id
}
output "key_vault_name" { value = module.key_vault.name }
output "key_vault_uri" { value = module.key_vault.uri }
output "frps_public_ip" { value = module.frps.public_ip }
output "database_name" { value = module.database.name }
output "email_sender" { value = module.email.sender_address }
output "email_verification_records" { value = module.email.verification_records }
