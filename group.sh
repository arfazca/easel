#!/bin/bash

# Array of file extensions to process
FILE_EXTENSIONS=(
    "cs"
    "tex"
    "csproj"
    "sln"
    "txt"
)

# Function for processing files of a specific type
process_files() {
    local extension=$1
    local header=$2
    echo "$header"
    find . -type f -name "*.$extension" -print0 | sort -z | while IFS= read -r -d $'\0' file; do
        echo -e "\n===== $file ====="
        cat "$file"
    done
}

options=("${FILE_EXTENSIONS[@]}" "ALL" "EXIT")

selected=$(printf "%s\n" "${options[@]}" | fzf --multi --prompt="Select file types to process (TAB to select multiple, ESC to cancel): ")

# Check if EXIT was selected or fzf was cancelled (empty result)
if [[ $selected == *"EXIT"* || -z "$selected" ]]; then
    echo "Operation cancelled"
    exit 0
fi

# This process is based on selection 
if [[ $selected == *"ALL"* ]]; then
    output_all_extensions() {
        for ext in "${FILE_EXTENSIONS[@]}"; do
            process_files "$ext" "===== Contents of all .$ext files ====="
            echo -e "\n\n"
        done
    }
    output_all_extensions | pbcopy
else
    output_selected_extensions() {
        for ext in $selected; do
            process_files "$ext" "===== Contents of all .$ext files ====="
            echo -e "\n\n"
        done
    }
    output_selected_extensions | pbcopy
fi

echo "Combined content of selected file types copied to clipboard!"
