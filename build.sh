#!/bin/bash

# Set -e to exit immediately if any command fails
set -e

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Change to the project root directory
cd "$SCRIPT_DIR" || exit 1

# Directory paths 
TOOLKIT_DIR="ToolKit"
PROJECT_FILE="$TOOLKIT_DIR/easel.csproj"

echo "Cleaning project..."
dotnet clean "$PROJECT_FILE" || { echo "Clean failed"; exit 1; }

echo "Building project..."
dotnet build "$PROJECT_FILE" || { echo "Build failed"; exit 1; }

echo "Running project..."
dotnet run --project "$PROJECT_FILE" || { echo "Run failed"; exit 1; }

echo "All tasks completed successfully"
