#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: scripts/add-migration.sh <client|system> <MigrationName>" >&2
  exit 1
}

[[ $# -eq 2 ]] || usage

case "$1" in
  client) context="ClientDbContext"; project="MultiTenantDemo.Client.Data" ;;
  system) context="SystemDbContext"; project="MultiTenantDemo.System.Data" ;;
  *) usage ;;
esac

name="$2"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
path="$root/src/$project"
startup="$root/src/MultiTenantDemo.Api"

cd "$root"

dotnet restore "$startup" --verbosity quiet

output="$(dotnet ef migrations add "$name" --context "$context" --project "$path" --startup-project "$startup" --json --prefix-output)"

mapfile -t files < <(sed -n 's/^data:    //p' <<< "$output" | jq -r '.migrationFile, .metadataFile, .snapshotFile')

for file in "${files[@]}"; do
  sed -i -e '1s/^\xEF\xBB\xBF//' -e '/^[[:space:]]*\/\//d' "$file"
  echo "Created ${file#"$root/"}"
done
