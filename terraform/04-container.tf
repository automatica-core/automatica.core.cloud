variable "exclusion" {
  default = [80,443, 7000, 7500]
}

locals {
  ports1 = [for i in range(1024,  2047) : {
    port = i
  } if !contains(var.exclusion, i)]
  ports2 = [for i in range(2048,  3071) : {
    port = i
  } if !contains(var.exclusion, i)]
  ports3 = [for i in range(3072,  4095) : {
    port = i
  } if !contains(var.exclusion, i)]
  ports4 = [for i in range(4096,  5119) : {
    port = i
  } if !contains(var.exclusion, i)]
  ports5 = [for i in range(5120,  6143) : {
    port = i
  } if !contains(var.exclusion, i)]
}


resource "azurerm_container_group" "frps" {
  name                = "${var.environment}-${var.prefix}-frps"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  restart_policy      = "Always"


  container {
    name   = "sync"
    image  = "automaticacore/frps:${var.frps_tag}"
    cpu    = "1.0"
    memory = var.container_memory

    ports {
      port     = 443
      protocol = "TCP"
    }
    ports {
      port     = 80
      protocol = "TCP"
    }
    ports {
      port     = 7000
      protocol = "TCP"
    }
    ports {
      port     = 7500
      protocol = "TCP"
    }
    
    dynamic "ports" {
      for_each = local.ports1
      content  {
        port   = ports.value.port
        protocol  = "TCP"
      }
    } 
    dynamic "ports" {
      for_each = local.ports1
      content  {
        port   = ports.value.port
        protocol  = "UDP"
      }
    } 

    dynamic "ports" {
      for_each = local.ports2
      content  {
        port   = ports.value.port
        protocol  = "TCP"
      }
    } 
    dynamic "ports" {
      for_each = local.ports2
      content  {
        port   = ports.value.port
        protocol  = "UDP"
      }
    } 

    dynamic "ports" {
      for_each = local.ports2
      content  {
        port   = ports.value.port
        protocol  = "TCP"
      }
    } 
    dynamic "ports" {
      for_each = local.ports2
      content  {
        port   = ports.value.port
        protocol  = "UDP"
      }
    } 

    # dynamic "ports" {
    #   for_each = local.ports3
    #   content  {
    #     port   = ports.value.port
    #     protocol  = "TCP"
    #   }
    # } 
    # dynamic "ports" {
    #   for_each = local.ports3
    #   content  {
    #     port   = ports.value.port
    #     protocol  = "UDP"
    #   }
    # } 
    
    # dynamic "ports" {
    #   for_each = local.ports4
    #   content  {
    #     port   = ports.value.port
    #     protocol  = "TCP"
    #   }
    # } 
    # dynamic "ports" {
    #   for_each = local.ports4
    #   content  {
    #     port   = ports.value.port
    #     protocol  = "UDP"
    #   }
    # } 

    # dynamic "ports" {
    #   for_each = local.ports4
    #   content  {
    #     port   = ports.value.port
    #     protocol  = "TCP"
    #   }
    # } 
    # dynamic "ports" {
    #   for_each = local.ports4
    #   content  {
    #     port   = ports.value.port
    #     protocol  = "UDP"
    #   }
    # } 
    environment_variables = {
   
      DASHBOARD_USER = var.frp_dashboard_user
      DASHBOARD_PASSWORD = var.frp_dashboard_password
      SUBDOMAIN_HOST = var.frp_subdomain
    }
  }

  tags = {
    environment = var.environment
  }

}

data "azurerm_container_group" "frps" {
  name                = "${var.environment}-${var.prefix}-frps"
  resource_group_name = azurerm_resource_group.rg.name
}


resource "azurerm_dns_a_record" "frps_a" {
  name                = var.environment == "prod" ? "@" : "${var.environment}"
  zone_name           = var.dns_zone_name
  resource_group_name = var.dns_ressource_group
  ttl                 = 300
  records             = [data.azurerm_container_group.frps.ip_address]
}
