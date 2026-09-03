variable "aws_region" {
  type        = string
  description = "Target AWS deployment region"
  default     = "eu-central-1"
}

variable "environment" {
  type        = string
  description = "Deployment environment name"
  default     = "production"
}

variable "project_name" {
  type        = string
  description = "Application project identifier"
  default     = "socar-dispatch"
}

variable "vpc_cidr" {
  type        = string
  description = "VPC CIDR network block"
  default     = "10.0.0.0/16"
}

variable "availability_zones" {
  type        = list(string)
  description = "Multi-AZ availability zones list"
  default     = ["eu-central-1a", "eu-central-1b"]
}

variable "container_image" {
  type        = string
  description = "Backend container image repository and tag"
  default     = "decuayer/socar-dispatch-api:latest"
}

variable "container_port" {
  type        = number
  description = "Port exposed by the backend container"
  default     = 8080
}

variable "acm_certificate_arn" {
  type        = string
  description = "AWS Certificate Manager SSL/TLS certificate ARN for ALB HTTPS listener"
  default     = ""
}

variable "db_instance_class" {
  type        = string
  description = "RDS PostgreSQL instance type"
  default     = "db.t4g.medium"
}

variable "db_allocated_storage" {
  type        = number
  description = "RDS allocated storage in gigabytes"
  default     = 50
}

variable "db_name" {
  type        = string
  description = "PostgreSQL default database name"
  default     = "socar_dispatch"
}

variable "db_username" {
  type        = string
  description = "PostgreSQL master username"
  default     = "socar_admin"
}

variable "db_password" {
  type        = string
  description = "PostgreSQL master password"
  sensitive   = true
}

variable "redis_node_type" {
  type        = string
  description = "ElastiCache Redis node type"
  default     = "cache.t4g.medium"
}
