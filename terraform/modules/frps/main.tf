# frps (fast reverse proxy) for the cores' Remote Connect.
# - Inbound only 7000 (frpc control), 80/443 (nginx, TLS with the Acmebot wildcard cert) and the TCP tunnel range;
#   SSH only from ssh_source_cidrs (empty = no SSH rule at all).
# - The dashboard listens on 127.0.0.1 only.
# - Secrets (auth token, plugin secret, dashboard password) are read from Key Vault at service start by the VM's
#   user-assigned identity; they are not in custom_data and not in the terraform plan.
# - Every login and proxy is checked by the cloud through the frps HTTP plugin (webapi/v1/frps/handler).

variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "subnet_id" { type = string }
variable "vm_size" { type = string }
variable "admin_ssh_public_key" {
  type        = string
  description = "Public key only."
}
variable "ssh_source_cidrs" {
  type    = list(string)
  default = []
}
variable "tcp_port_range" {
  type        = object({ start = number, end = number })
  description = "Remote ports the cloud hands out for tcp/udp tunnels (RemoteControlPort.Port + 1023)."
}
variable "subdomain_host" { type = string }
variable "key_vault_id" { type = string }
variable "key_vault_name" { type = string }
variable "cert_key_vault_id" {
  type        = string
  description = "Key Vault that Acmebot renews the wildcard certificate in."
}
variable "cert_key_vault_name" { type = string }
variable "cert_name" { type = string }
variable "frp_version" { type = string }
variable "frp_sha256" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_user_assigned_identity" "this" {
  name                = "id-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  tags                = var.tags
}

resource "azurerm_role_assignment" "kv_secrets" {
  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

# only the one certificate secret, not the whole Acmebot vault
resource "azurerm_role_assignment" "cert" {
  scope                = "${var.cert_key_vault_id}/secrets/${var.cert_name}"
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

resource "azurerm_public_ip" "this" {
  name                = "pip-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  allocation_method   = "Static"
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_network_security_group" "this" {
  name                = "nsg-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  tags                = var.tags

  security_rule {
    name                       = "frps-control"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "7000"
    source_address_prefix      = "Internet"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "https"
    priority                   = 110
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_ranges    = ["80", "443"]
    source_address_prefix      = "Internet"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "tunnels"
    priority                   = 120
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "${var.tcp_port_range.start}-${var.tcp_port_range.end}"
    source_address_prefix      = "Internet"
    destination_address_prefix = "*"
  }

  dynamic "security_rule" {
    for_each = length(var.ssh_source_cidrs) > 0 ? [1] : []
    content {
      name                       = "ssh-admin"
      priority                   = 200
      direction                  = "Inbound"
      access                     = "Allow"
      protocol                   = "Tcp"
      source_port_range          = "*"
      destination_port_range     = "22"
      source_address_prefixes    = var.ssh_source_cidrs
      destination_address_prefix = "*"
    }
  }

  security_rule {
    name                       = "deny-other-inbound"
    priority                   = 4000
    direction                  = "Inbound"
    access                     = "Deny"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "*"
    source_address_prefix      = "Internet"
    destination_address_prefix = "*"
  }
}

resource "azurerm_network_interface" "this" {
  name                = "nic-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  tags                = var.tags

  ip_configuration {
    name                          = "primary"
    subnet_id                     = var.subnet_id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.this.id
  }
}

resource "azurerm_network_interface_security_group_association" "this" {
  network_interface_id      = azurerm_network_interface.this.id
  network_security_group_id = azurerm_network_security_group.this.id
}

locals {
  cert_file = "/etc/nginx/certs/${var.cert_key_vault_name}.${var.cert_name}"

  cloud_init = templatefile("${path.module}/templates/cloud-init.yaml.tftpl", {
    frp_version  = var.frp_version
    frp_sha256   = var.frp_sha256
    frps_service = file("${path.module}/templates/frps.service")
    render_script = templatefile("${path.module}/templates/frps-render-config.sh.tftpl", {
      key_vault_name     = var.key_vault_name
      identity_client_id = azurerm_user_assigned_identity.this.client_id
      subdomain_host     = var.subdomain_host
      port_start         = var.tcp_port_range.start
      port_end           = var.tcp_port_range.end
    })
    nginx_conf = templatefile("${path.module}/templates/nginx.conf.tftpl", {
      subdomain_host = var.subdomain_host
      cert_file      = local.cert_file
    })
  })
}

resource "azurerm_linux_virtual_machine" "this" {
  name                            = "vm-${var.name}"
  computer_name                   = var.name
  resource_group_name             = var.resource_group_name
  location                        = var.location
  size                            = var.vm_size
  admin_username                  = "frpsadmin"
  disable_password_authentication = true
  network_interface_ids           = [azurerm_network_interface.this.id]
  custom_data                     = base64encode(local.cloud_init)
  secure_boot_enabled             = true
  vtpm_enabled                    = true
  patch_mode                      = "AutomaticByPlatform"
  patch_assessment_mode           = "AutomaticByPlatform"
  tags                            = var.tags

  admin_ssh_key {
    username   = "frpsadmin"
    public_key = var.admin_ssh_public_key
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.this.id]
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "StandardSSD_LRS"
    disk_size_gb         = 30
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "ubuntu-24_04-lts"
    sku       = "server"
    version   = "latest"
  }

  boot_diagnostics {}

  depends_on = [azurerm_role_assignment.kv_secrets, azurerm_role_assignment.cert]
}

# keeps the wildcard certificate in /etc/nginx/certs in sync with Key Vault (Acmebot renews it there)
resource "azurerm_virtual_machine_extension" "kv_cert" {
  name                       = "KeyVaultForLinux"
  virtual_machine_id         = azurerm_linux_virtual_machine.this.id
  publisher                  = "Microsoft.Azure.KeyVault"
  type                       = "KeyVaultForLinux"
  type_handler_version       = "3.0"
  auto_upgrade_minor_version = true

  settings = jsonencode({
    secretsManagementSettings = {
      pollingIntervalInS       = "3600"
      certificateStoreLocation = "/etc/nginx/certs"
      requireInitialSync       = true
      observedCertificates     = ["https://${var.cert_key_vault_name}.vault.azure.net/secrets/${var.cert_name}"]
    }
    authenticationSettings = {
      msiEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token"
      msiClientId = azurerm_user_assigned_identity.this.client_id
    }
  })
}

output "public_ip" { value = azurerm_public_ip.this.ip_address }
output "principal_id" { value = azurerm_user_assigned_identity.this.principal_id }
