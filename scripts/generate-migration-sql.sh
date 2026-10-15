#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${CONFIGURATION:-Debug}"
startup="$root/src/MultiTenantDemo.Api"

build="true"
if [[ "${1:-}" == "--no-build" ]]; then
  build="false"
fi

cd "$root"

generate() {
  local context="$1"
  local directory="$root/migrations/$2"
  local project="$root/src/$3"
  local previous="0"

  ef() {
    dotnet ef "$@" --no-build --configuration "$configuration" --project "$project" --startup-project "$startup"
  }

  if [[ "$build" == "true" ]]; then
    dotnet build "$startup" --configuration "$configuration" --nologo --verbosity quiet
  fi

  rm -rf "$directory"
  mkdir -p "$directory"

  mapfile -t migrations < <(ef migrations list --no-connect --json --context "$context" | jq -r '.[].id')

  for migration in "${migrations[@]}"; do
    ef migrations script "$previous" "$migration" --context "$context" --output "$directory/$migration.up.sql" > /dev/null
    ef migrations script "$migration" "$previous" --context "$context" --output "$directory/$migration.down.sql" > /dev/null
    previous="$migration"
  done

  ef migrations script --idempotent --context "$context" --output "$directory/idempotent.sql" > /dev/null
  echo "Generated ${#migrations[@]} migration(s) for $context in migrations/$2"
}

generate ClientDbContext client MultiTenantDemo.Client.Data
generate SystemDbContext system MultiTenantDemo.System.Data
