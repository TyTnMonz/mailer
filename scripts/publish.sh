#!/bin/bash
# Publish script for self-contained deployment
# Creates a single executable that doesn't require .NET runtime

echo "========================================"
echo "  Mailer - Self-Contained Publisher"
echo "========================================"
echo ""

echo "Cleaning previous builds..."
dotnet clean -c Release
echo ""

echo "Publishing self-contained executable..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
echo ""

if [ $? -eq 0 ]; then
    echo "========================================"
    echo "  BUILD SUCCESSFUL!"
    echo "========================================"
    echo ""
    echo "Output location:"
    echo "  bin/Release/net9.0/win-x64/publish/Mailer.exe"
    echo ""
    echo "File size: ~70-80 MB (includes .NET runtime)"
    echo ""
    echo "This executable can run on any Windows PC without .NET installation."
    echo ""
else
    echo "========================================"
    echo "  BUILD FAILED!"
    echo "========================================"
    echo ""
    exit 1
fi
