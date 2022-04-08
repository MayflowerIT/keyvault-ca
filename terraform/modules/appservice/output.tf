output "app_hostname" {
  value = azurerm_app_service.appservice.default_site_hostname
}

output "est_user" {
  value = var.est_user
}

output "est_password" {
  value = var.est_password
}