#!/usr/bin/env sh
set -eu
# Installs no network services, miners, or daemons.
export PYTHONPATH="$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)/src${PYTHONPATH:+:$PYTHONPATH}"
ROOT="$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)"
STATE="${CAWFORGE_HOME:-$ROOT/.cawforge}"
OUT="${CAWFORGE_OUTPUT:-$ROOT/out}"
python3 -m cawforge --home "$STATE" demo --to "$OUT"
