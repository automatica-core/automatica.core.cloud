
locals {
    node_name = "${var.prefix}-${var.environment}"
}

resource "azurerm_virtual_network" "main" {
  name                = "${local.node_name}-network"
  address_space       = ["10.0.0.0/16"]
  location            = var.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_subnet" "internal" {
  name                 = local.node_name
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.2.0/24"]
}


resource "azurerm_network_security_group" "sg" {
    name                = "${local.node_name}-security-group"
    location            = var.location
    resource_group_name = azurerm_resource_group.rg.name

    security_rule {
        name                       = "All"
        priority                   = 1001
        direction                  = "Inbound"
        access                     = "Allow"
        protocol                   = "*"
        source_port_range          = "*"
        destination_port_range     = "*"
        source_address_prefix      = "*"
        destination_address_prefix = "*"
    }

    tags = {
        environment = var.environment
    }
}


resource "azurerm_public_ip" "node_public_ip" {
    name                         = "${local.node_name}-public-ip"
    location                     = var.location
    resource_group_name          = azurerm_resource_group.rg.name
    allocation_method            = "Static"

    tags = {
        environment = var.environment
    }
}

resource "azurerm_network_interface" "nic" {
    name                        = "${local.node_name}-nic"
    location                    = var.location
    resource_group_name         = azurerm_resource_group.rg.name

    ip_configuration {
        name                          = "${local.node_name}-nic-config"
        subnet_id                     = azurerm_subnet.internal.id
        private_ip_address_allocation = "Dynamic"
        public_ip_address_id          = azurerm_public_ip.node_public_ip.id
    }

    tags = {
        environment = var.environment
    }
}

resource "azurerm_network_interface_security_group_association" "sg_link" {
    network_interface_id      = azurerm_network_interface.nic.id
    network_security_group_id = azurerm_network_security_group.sg.id
}

data "azurerm_key_vault_key" "key" {
  name         = "frps-private"
  key_vault_id = var.key_vault_id
}

data "azurerm_key_vault_secret" "key" {
  name         = "frps-private"
  key_vault_id = var.key_vault_id
}

data "template_file" "cloud_init" {
  template   = file("${path.root}/cloud-init/cloud-init.yml")
}


data "template_file" "frps" {
  template   = file("${path.root}/templates/frps.tpl")

  vars = {
    dashboard_user          = "admin"
    dashboard_password      = "admin"
    subdomain               = "${var.environment}.automaticaremote.com"
  }
}

data "template_file" "frps_service" {
  template   = file("${path.root}/templates/frps.service.tpl")
}

resource "azurerm_linux_virtual_machine" "frps_node" {
    name                  = "${local.node_name}"
    location              = var.location
    resource_group_name   = azurerm_resource_group.rg.name
    network_interface_ids = [azurerm_network_interface.nic.id]
    size                  = var.server_type

    os_disk {
        name              = "${local.node_name}-disk"
        caching           = "ReadWrite"
        storage_account_type = "Standard_LRS"
        disk_size_gb = 20
    }

    source_image_reference {
        publisher = "Canonical"
        offer     = var.server_image
        sku       = var.server_version
        version   = "latest"
    }

    computer_name  = "${local.node_name}"
    admin_username = "frps"
    disable_password_authentication = true

    admin_ssh_key {
        username       = "frps"
        public_key     = data.azurerm_key_vault_key.key.public_key_openssh
    }

    custom_data = base64encode(data.template_file.cloud_init.rendered)

    connection {
        host = self.public_ip_address
        user = "frps"
        private_key = data.azurerm_key_vault_secret.key.value
    }

    lifecycle {
      ignore_changes = [custom_data]
    }

    provisioner "file" {
      content = data.template_file.frps.rendered
      destination = "/etc/frps.ini"
    }

    provisioner "file" {
      content = data.template_file.frps_service.rendered
      destination = "/etc/systemd/system/frps.service"
    }

    provisioner "remote-exec" {
     inline = [
      "systemctl enable --now frps",
     ]
    }

    tags = {
        environment = var.environment
    }
}
