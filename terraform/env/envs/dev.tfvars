# dev (develop branch). No secrets in this file.
subscription_id        = "e458a87b-c87b-4c65-a0c5-ed2f77a239de"
environment            = "dev"
aspnetcore_environment = "Production"
vnet_address_space     = "10.40.0.0/16"
app_service_sku        = "B1"
speech_sku             = "F0"
email_domain           = null # Azure managed sender domain; set "automaticacore.com" once its DNS records are added

database = {
  name                      = "automaticacorecloud-dev" # existing, imported (see existing.database_id)
  sku_name                  = "Basic"
  max_size_gb               = 2
  storage_account_type      = "Geo"
  short_term_retention_days = 7
}

# Phase 1: false (the new app runs on its azurewebsites.net host).
# Cut-over: true -> imports and repoints cloud-dev.automaticacore.com, binds a managed certificate.
custom_domain = {
  enabled = false
  zone    = "automaticacore.com"
  record  = "cloud-dev"
}

frps = {
  vm_size                       = "Standard_B2s"
  admin_ssh_public_key          = "ssh-ed25519 AAAA-REPLACE-WITH-PUBLIC-KEY frpsadmin" # TODO: public key only
  ssh_source_cidrs              = []                                                   # no SSH from the internet
  tcp_port_range                = { start = 1024, end = 2047 }
  dns_record                    = "dev"
  manage_dns                    = false # cut-over: true -> dev.automaticaremote.com points at the new VM
  cert_key_vault_name           = "kv-remote-acme-mtsu"
  cert_key_vault_resource_group = "automatica-cloud-dev-acme"
  cert_name                     = "wildcard-dev-automaticaremote-com"
  frp_version                   = "0.58.1" # same protocol line as the frpc 0.58.0 the cores ship
  frp_sha256                    = "5bd9f8860b580ed9c42eed1c99dfaa03b196d0f68007dca088f6c098d498430d"
  token_auth_enabled            = false # true once the cores send FRPS_TOKEN (cloud returns it as frpsToken)
  require_api_key_meta          = false # true once the cores send meta_apikey
}

existing = {
  database_id     = "/subscriptions/e458a87b-c87b-4c65-a0c5-ed2f77a239de/resourceGroups/automatica/providers/Microsoft.Sql/servers/automaticacorecloud/databases/automaticacorecloud-dev"
  frps_a_record   = "/subscriptions/e458a87b-c87b-4c65-a0c5-ed2f77a239de/resourceGroups/automatica/providers/Microsoft.Network/dnsZones/automaticaremote.com/A/dev"
  cloud_cname     = "/subscriptions/e458a87b-c87b-4c65-a0c5-ed2f77a239de/resourceGroups/automatica/providers/Microsoft.Network/dnsZones/automaticacore.com/CNAME/cloud-dev"
  cloud_asuid_txt = "/subscriptions/e458a87b-c87b-4c65-a0c5-ed2f77a239de/resourceGroups/automatica/providers/Microsoft.Network/dnsZones/automaticacore.com/TXT/asuid.cloud-dev"
}
