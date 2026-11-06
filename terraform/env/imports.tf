# Adopt existing resources per environment (IDs in envs/<env>.tfvars -> existing). An empty value skips the import.

import {
  for_each = var.existing.database_id == null ? {} : { db = var.existing.database_id }
  to       = module.database.azurerm_mssql_database.this
  id       = each.value
}

import {
  for_each = var.existing.frps_a_record == null || !var.frps.manage_dns ? {} : { a = var.existing.frps_a_record }
  to       = azurerm_dns_a_record.frps[0]
  id       = each.value
}

import {
  for_each = var.existing.cloud_cname == null || !var.custom_domain.enabled ? {} : { cname = var.existing.cloud_cname }
  to       = module.app.azurerm_dns_cname_record.this[0]
  id       = each.value
}

import {
  for_each = var.existing.cloud_asuid_txt == null || !var.custom_domain.enabled ? {} : { txt = var.existing.cloud_asuid_txt }
  to       = module.app.azurerm_dns_txt_record.asuid[0]
  id       = each.value
}
