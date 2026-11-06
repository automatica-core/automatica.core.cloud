# One-time adoption of the existing resources. Re-running is a no-op once they are in state.

import {
  to = azurerm_mssql_server.this
  id = "/subscriptions/${var.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Sql/servers/${var.sql_server_name}"
}

import {
  to = azurerm_storage_account.updates
  id = "/subscriptions/${var.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Storage/storageAccounts/${var.updates_storage_name}"
}

import {
  to = azurerm_storage_container.updates
  id = "/subscriptions/${var.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Storage/storageAccounts/${var.updates_storage_name}/blobServices/default/containers/automaticaupdates"
}
