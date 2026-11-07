# No secrets in tfvars. IDs and names only.
subscription_id = "e458a87b-c87b-4c65-a0c5-ed2f77a239de"

# Entra principal that becomes SQL admin. Use a group (e.g. "automatica-sql-admins") with Patrik in it.
sql_entra_admin = {
  login_username = "automatica-sql-admins"
  object_id      = "00000000-0000-0000-0000-000000000000" # TODO: object id of that group
}
