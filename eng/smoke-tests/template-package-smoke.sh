#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
configuration="${CONFIGURATION:-Release}"
package_output="${1:-${PACKAGE_OUTPUT:-artifacts/packages}}"
work_root="${TEMPLATE_SMOKE_WORK_ROOT:-${RUNNER_TEMP:-/tmp}/asi-backbone-template-smoke}"

make_absolute_path() {
  local path="$1"

  if [[ "$path" = /* || "$path" =~ ^[A-Za-z]:[\\/].* ]]; then
    printf '%s\n' "$path"
  else
    printf '%s/%s\n' "$repo_root" "$path"
  fi
}

to_dotnet_path() {
  local path="$1"

  if command -v cygpath >/dev/null 2>&1; then
    cygpath -w "$path"
  else
    printf '%s\n' "$path"
  fi
}

package_output="$(to_dotnet_path "$(make_absolute_path "$package_output")")"

if [ ! -d "$package_output" ]; then
  echo "Package output directory was not found: $package_output"
  exit 1
fi

template_package="$(find "$package_output" -maxdepth 1 -name 'AsiBackbone.Templates.*.nupkg' -type f | sort | tail -n 1)"

if [ -z "$template_package" ]; then
  echo "AsiBackbone.Templates package was not found in $package_output."
  exit 1
fi

rm -rf "$work_root"
mkdir -p "$work_root"

cat > "$work_root/NuGet.config" <<NUGETCONFIG
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-asi-backbone" value="$package_output" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
NUGETCONFIG

export DOTNET_CLI_HOME="$work_root/.dotnet-home"
mkdir -p "$DOTNET_CLI_HOME"

dotnet new install "$template_package"

for host_style in plain netcoretemplate; do
  project_name="AsiBackboneTemplateSmoke${host_style}"
  project_dir="$work_root/$project_name"

  echo "Generating template host style '$host_style' into $project_dir"
  dotnet new asibackbone-webapi \
    --name "$project_name" \
    --output "$project_dir" \
    --hostStyle "$host_style"

  if ! grep -q "\"HostStyle\": \"$host_style\"" "$project_dir/appsettings.json"; then
    echo "Generated appsettings.json did not contain selected host style '$host_style'."
    exit 1
  fi

  dotnet restore "$project_dir/$project_name.csproj" \
    --configfile "$work_root/NuGet.config"

  dotnet build "$project_dir/$project_name.csproj" \
    --configuration "$configuration" \
    --no-restore

done
