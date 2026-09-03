resource "aws_db_subnet_group" "db" {
  name        = "${var.project_name}-db-subnet-group"
  subnet_ids  = aws_subnet.isolated[*].id
  description = "Isolated database subnets for PostgreSQL"

  tags = {
    Name = "${var.project_name}-db-subnet-group"
  }
}

resource "aws_db_parameter_group" "postgres" {
  name        = "${var.project_name}-postgres16-params"
  family      = "postgres16"
  description = "Custom PostgreSQL 16 parameters with PostGIS support"

  parameter {
    name         = "shared_preload_libraries"
    value        = "pg_stat_statements"
    apply_method = "pending-reboot"
  }
}

resource "aws_db_instance" "postgres" {
  identifier             = "${var.project_name}-db"
  engine                 = "postgres"
  engine_version         = "16.3"
  instance_class         = var.db_instance_class
  allocated_storage      = var.db_allocated_storage
  max_allocated_storage  = 200
  storage_type           = "gp3"
  db_name                = var.db_name
  username               = var.db_username
  password               = var.db_password
  db_subnet_group_name   = aws_db_subnet_group.db.name
  vpc_security_group_ids = [aws_security_group.db_sg.id]
  parameter_group_name   = aws_db_parameter_group.postgres.name

  publicly_accessible = false
  skip_final_snapshot = true
  multi_az            = true

  tags = {
    Name = "${var.project_name}-postgres"
  }
}
