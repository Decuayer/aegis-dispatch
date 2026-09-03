output "alb_dns_name" {
  description = "DNS endpoint of the Application Load Balancer"
  value       = aws_lb.main.dns_name
}

output "ecs_cluster_name" {
  description = "Name of the ECS Cluster"
  value       = aws_ecs_cluster.main.name
}

output "ecs_service_name" {
  description = "Name of the ECS Service"
  value       = aws_ecs_service.api.name
}

output "rds_endpoint" {
  description = "Connection endpoint for PostgreSQL RDS"
  value       = aws_db_instance.postgres.endpoint
}

output "redis_primary_endpoint" {
  description = "Primary endpoint for ElastiCache Redis"
  value       = aws_elasticache_replication_group.redis.primary_endpoint_address
}
