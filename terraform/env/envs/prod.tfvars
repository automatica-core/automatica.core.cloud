# prod (master branch). There is no prod stack today: everything is created, including a new database
# (prevent_destroy). No secrets in this file.
subscription_id        = "e458a87b-c87b-4c65-a0c5-ed2f77a239de"
environment            = "prod"
aspnetcore_environment = "Production"
vnet_address_space     = "10.41.0.0/16"
app_service_sku        = "B1"
speech_sku             = "S0"
email_domain           = "automaticacore.com" # add the verification records from the output to DNS once

database = {
  name                      = "automaticacorecloud-prod"
  sku_name                  = "S0"
  max_size_gb               = 10
  storage_account_type      = "Geo"
  short_term_retention_days = 14
}

# cloud.automaticacore.com currently dangles (CNAME to a deleted app): enabling this replaces it.
custom_domain = {
  enabled = false
  zone    = "automaticacore.com"
  record  = "cloud"
}

frps = {
  vm_size                       = "Standard_B2s"
  admin_ssh_public_key          = "ssh-ed25519 AAAA-REPLACE-WITH-PUBLIC-KEY frpsadmin" # TODO: public key only
  ssh_source_cidrs              = []
  tcp_port_range                = { start = 1024, end = 2047 }
  dns_record                    = "prod"
  manage_dns                    = false
  cert_key_vault_name           = "kv-remote-acme-mtsu"
  cert_key_vault_resource_group = "automatica-cloud-dev-acme"
  cert_name                     = "wildcard-automaticaremote-com" # TODO: Acmebot must issue *.prod.automaticaremote.com
  frp_version                   = "0.58.1"
  frp_sha256                    = "5bd9f8860b580ed9c42eed1c99dfaa03b196d0f68007dca088f6c098d498430d"
  token_auth_enabled            = false
  require_api_key_meta          = false
}

existing = {
  cloud_cname = "/subscriptions/e458a87b-c87b-4c65-a0c5-ed2f77a239de/resourceGroups/automatica/providers/Microsoft.Network/dnsZones/automaticacore.com/CNAME/cloud"
}
