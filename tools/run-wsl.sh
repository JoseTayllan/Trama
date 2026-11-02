#!/usr/bin/env bash
set -euo pipefail

# Native Linux build in a dedicated mirror; never copy Windows binaries or personal data.
action="${1:-test}"
case "$action" in test|run|sync) ;; *) echo 'Use: bash tools/run-wsl.sh [test|run|sync]' >&2; exit 2;; esac
source_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
workspace="${TRAMA_WSL_WORKSPACE:-$HOME/trama-codex}"
workspace="$(realpath -m -- "$workspace")"
if [[ "$workspace" == "$source_root" || "$workspace" == / || "$workspace" == "$HOME" ]]; then
    echo 'Choose a separate development directory.' >&2; exit 2
fi
case "$workspace/" in "$source_root/"*) echo 'Keep the Linux mirror outside the source checkout.' >&2; exit 2;; esac
if [[ -e "$workspace" ]]; then
    if [[ ! -f "$workspace/.trama-source" ]] || [[ "$(cat "$workspace/.trama-source")" != "$source_root" ]]; then
        echo 'The destination is not a Trama development mirror owned by this checkout.' >&2; exit 2
    fi
else
    mkdir -p -- "$workspace"
    printf '%s\n' "$source_root" > "$workspace/.trama-source"
fi
command -v rsync >/dev/null || { echo 'Install rsync in the Linux environment.' >&2; exit 2; }
for component in src tests assets; do
    [[ ! -L "$workspace/$component" ]] || { echo 'Mirror source directories must not be symbolic links.' >&2; exit 2; }
    rsync -a --delete --exclude=bin --exclude=obj --exclude=.avalonia-build-tasks \
        "$source_root/$component/" "$workspace/$component/"
done
for file in global.json Directory.Build.props Directory.Packages.props NuGet.Config Trama.slnx .editorconfig; do
    cp -- "$source_root/$file" "$workspace/$file"
done
# Windows publishing can append RID-specific graphs. Keep the pinned generic
# graph in the Linux mirror; locked restore below still validates every package.
command -v python3 >/dev/null || { echo 'Install python3 in the Linux environment.' >&2; exit 2; }
python3 - "$workspace" <<'PY'
import json
import sys
from pathlib import Path

workspace = Path(sys.argv[1])
for component in ("src", "tests"):
    for lock in (workspace / component).glob("*/packages.lock.json"):
        content = json.loads(lock.read_text(encoding="utf-8-sig"))
        dependencies = content["dependencies"]
        generic = {target: graph for target, graph in dependencies.items() if "/" not in target}
        if not generic:
            raise ValueError(f"Missing generic dependency graph: {lock}")
        if dependencies != generic:
            content["dependencies"] = generic
            lock.write_text(json.dumps(content, indent=2) + "\n", encoding="utf-8")
PY

echo "Linux development mirror: $workspace"
[[ "$action" == sync ]] && exit 0
if [[ ! -x "$HOME/.dotnet-trama/dotnet" ]]; then
    echo 'Install the pinned SDK in ~/.dotnet-trama before running.' >&2; exit 2
fi
export DOTNET_ROOT="$HOME/.dotnet-trama"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
cd "$workspace"
dotnet restore Trama.slnx --locked-mode
dotnet build Trama.slnx --configuration Release --no-restore --verbosity quiet
if [[ "$action" == test ]]; then
    dotnet format Trama.slnx --verify-no-changes --no-restore
    dotnet test Trama.slnx --configuration Release --no-build --logger trx --results-directory artifacts/wsl-results --verbosity quiet
else
    exec dotnet run --project src/Trama.Desktop --configuration Release --no-build -- --data-directory "$workspace/.local-profile"
fi
