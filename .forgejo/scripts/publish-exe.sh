#!/usr/bin/env bash
# Publishes the app as a single self-contained Windows exe.
# Usage: publish-exe.sh <output-dir> [version]
# Without a version, the one from Directory.Build.props is used.
set -euo pipefail

out=${1:?output dir}
version=${2:-}

args=(
  src/JulschaVehicleTool.App
  -c Release
  -r win-x64
  --self-contained true
  -p:PublishSingleFile=true
  -p:IncludeNativeLibrariesForSelfExtract=true
  -p:EnableCompressionInSingleFile=true
  -p:DebugType=none
  -p:EnableWindowsTargeting=true
  -o "$out"
)
[[ -n "$version" ]] && args+=("-p:Version=$version")

dotnet publish "${args[@]}"
ls -la "$out"
