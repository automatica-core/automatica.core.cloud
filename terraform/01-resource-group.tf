

# Create a resource group
resource "azurerm_resource_group" "rg" {
  name     = "${var.prefix}-${var.environment}"
  location = "West Europe"

  tags = {
    Environment = var.environment_tag
  }
}

