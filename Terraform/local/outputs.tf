output "resource_group_name" {
  value = null_resource.resource_group.triggers.name
}

output "app_service_name" {
  value = null_resource.app_service.triggers.name
}

output "sql_server_name" {
  value = null_resource.sql_server.triggers.name
}

output "summary_file" {
  value = local_file.infrastructure_summary.filename
}
