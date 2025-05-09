#!/bin/bash

# Setting -e to exit immediately if any command fails
set -e

# Getting the directory 
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR" || exit 1
TOOLKIT_DIR="ToolKit"
PROJECT_FILE="$TOOLKIT_DIR/easel.csproj"

echo "Cleaning project..."
dotnet clean "$PROJECT_FILE" || { echo "Clean failed"; exit 1; }

echo "Building project..."
dotnet build "$PROJECT_FILE" || { echo "Build failed"; exit 1; }

if [[ "$1" == "--non-interactive" || "$1" == "-n" ]]; then
    echo "Running in non-interactive mode..."
    dotnet run --project "$PROJECT_FILE" -- "$@" || { echo "Run failed"; exit 1; }
else
    echo "Running in interactive mode..."
    dotnet run --project "$PROJECT_FILE" || { echo "Run failed"; exit 1; }
fi

echo "All tasks completed successfully"
