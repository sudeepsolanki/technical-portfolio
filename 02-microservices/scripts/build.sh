#!/usr/bin/env bash
set -euo pipefail

echo "Building solution..."
dotnet build Microservices.sln

echo "Running tests..."
dotnet test tests/SagaTests/SagaTests.csproj --verbosity normal

echo "Build complete!"
