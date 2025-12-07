# Deploying to AWS ECS Fargate (HTTPS + Swagger)

This app runs on ECS Fargate as a container. You commit code to GitHub; GitHub Actions builds a container image, pushes it to ECR, and deploys to your ECS service.

## Prereqs
- An AWS account with IAM permissions to create ECR repo, ECS cluster/service, ALB, Target Group, IAM roles, CloudWatch logs.
- A VPC with public subnets (for the ALB) and private subnets (for ECS tasks). RDS should live in private subnets.
- ACM certificate for your domain (in the same region) if you want a custom HTTPS domain.

## One-time AWS setup
1) Create an ECR repo (e.g., `idansure-subscription`).
2) Create an ECS cluster (EC2-less), networking type: Fargate.
3) Create an ALB + HTTPS listener (443) with your ACM certificate.
4) Create an ECS service targeting that ALB/Target Group with port 8080 on the container.
   - Task definition family should match `deploy/taskdef.json` (or update the names there).
   - Network mode: awsvpc; assign public IP = false; subnets = private; security groups allow outbound to internet via NAT.
5) IAM roles
   - Execution role: `ecsTaskExecutionRole` with ECR pull and CloudWatch logs.
   - Task role: permissions for S3/Polly/Secrets Manager/SSM as needed.
6) CloudWatch Log Group: `/ecs/idan-subscription-api` (or adjust in `taskdef.json`).

## GitHub OIDC (no long-lived AWS keys)
- Create an IAM role `GitHubActionsDeployRole` with a trust policy for GitHub OIDC and permissions for:
  - ECR: push image
  - ECS: register task definition, update service
  - Logs: describe groups (optional)
- In `.github/workflows/ecs-fargate.yml`, set `AWS_ACCOUNT_ID`, `AWS_REGION`, `ECR_REPOSITORY`, `ECS_CLUSTER`, `ECS_SERVICE`, `CONTAINER_NAME` to your values.

## Environment/config
- Swagger UI in prod is toggled by `Swagger:Enabled=true` (already set in `taskdef.json`).
- App listens on port 8080. ALB forwards 443 -> target group (8080).
- Inject secrets via SSM/Secrets Manager. Either:
  - Map as env vars in the task definition with `{ "name": "Key", "valueFrom": "arn:aws:ssm:..." }` or Secrets Manager ARNs.
  - Or mount as plain env vars for non-sensitive values.

## Deploy flow
- Push to `main`.
- GitHub Actions builds the image using `SubscriptionSystem/Dockerfile` and pushes to ECR.
- The workflow renders `deploy/taskdef.json` with the new image tag and updates the ECS service.
- ALB health checks `/api/health/ready` (configure in target group) and traffic shifts when healthy.

## Swagger URL
- Once the service is healthy behind the ALB with HTTPS, your Swagger UI is at:
  - `https://<your-domain>/swagger`
- Forwarded headers are respected; Swagger will use `https`.

## Troubleshooting
- 502 on ALB: ensure target group health check path matches your endpoint (e.g., `/api/health/ready`), port is 8080, security groups and subnets are correct.
- 403/404: check your ALB listener rules and service path rules.
- No logs: ensure awslogs config and log group exist (or allow creation by the task role).
