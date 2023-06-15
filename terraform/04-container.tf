


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