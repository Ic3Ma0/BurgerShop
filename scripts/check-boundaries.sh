#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
python3 "$project_dir/scripts/check_boundaries.py" "$@"
python3 -B -m unittest discover -s "$project_dir/scripts/tests" -p 'test_check_boundaries.py'
