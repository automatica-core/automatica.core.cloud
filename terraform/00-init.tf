terraform {
  backend "azurerm" {
    key = "automatica-cloud/automatica-cloud.terraform.tfstate"
  }
}
terraform {
  required_version = ">= 1.1.6"
}

terraform {
  required_providers {
    
    azurerm = {
      source = "hashicorp/azurerm"
    }

    
    local = {
      source = "hashicorp/local"
    }

    null = {
      source = "hashicorp/null"
    }

    template = {
      source = "hashicorp/template"
    }

    http-full = {
      source = "salrashid123/http-full"
      version = "1.0.0"
    }
  }
}

provider "azurerm" {
  skip_provider_registration = "true"
  features {}
}

provider "http-full" {
}


# define the deployment location (az account list-locations --output table)
variable "location" {
  type    = string
  default = "westeurope"
}

# define the prefixed name
variable "prefix" {
  default = "automatica-cloud"
}

# define the environemnt name
variable "environment" {
  description = "deployment prefix"
}

variable "environment_tag" {
}

variable "container_memory" {
    default = "1.5"
}

variable "frps_tag" {
  
}

variable "frp_dashboard_user" {
  
}
variable "frp_dashboard_password" {
  
}
variable "frp_subdomain" {
  
}


variable "dns_ressource_group" {
  
}
variable "dns_zone_name" {
  
}

variable "key_vault_id" {
  default = "/subscriptions/e458a87b-c87b-4c65-a0c5-ed2f77a239de/resourceGroups/automatica/providers/Microsoft.KeyVault/vaults/automaticakv"
}


variable "server_image" {
  default = "UbuntuServer"
}
variable "server_version" {
  default = "18.04-LTS"
}

variable "server_type" {
  default = "Standard_B2s"
}

