#!/usr/bin/env bash
set -euo pipefail

echo "Building Docker images..."
docker compose build

echo "Starting services..."
docker compose up -d

echo "Waiting for services..."
sleep 10

echo "Checking health..."
curl -s http://localhost:5000/health | head -c 200
echo

echo ""
echo "Services running:"
echo "  API Gateway: http://localhost:5000"
echo "  Products:    http://localhost:5001"
echo "  Orders:      http://localhost:5002"
echo "  Payments:    http://localhost:5003"
echo "  RabbitMQ:    http://localhost:15672 (guest/guest)"
echo "  Frontend:    http://localhost:3000"
