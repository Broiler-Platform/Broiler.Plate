#!/usr/bin/env bash
# Creates the draft GitHub pre-release for one Broiler.Plate version.
#
#   eng/release-draft.sh <version> <artifacts-dir>
#
# <artifacts-dir> holds the Publish workflow's artifacts, extracted as
# `gh run download` / actions/download-artifact leave them:
#
#   <artifacts-dir>/broiler-plate-win-x64-<version>/Broiler.Plate.Windows.exe
#
# The executable is zipped on its own into a release asset. Python writes the zip, so the
# script runs the same on the Linux runner and by hand from Git Bash on Windows.
#
# The tag plate-v<version> must already exist on the remote; the release is created
# for it as a draft, so nothing is public until someone publishes it. Needs the GitHub
# CLI with GH_TOKEN (or a login) that may write releases, and Python 3.
set -euo pipefail

version=${1:?usage: eng/release-draft.sh <version> <artifacts-dir>}
artifacts=${2:?usage: eng/release-draft.sh <version> <artifacts-dir>}
tag="plate-v$version"
out=$(mktemp -d)

asset() {
  local rid=$1 executable=$2
  local source="$artifacts/broiler-plate-$rid-$version/$executable"
  local zip="$out/Broiler.Plate-$version-$rid.zip"
  [ -f "$source" ] || { echo "missing $source" >&2; exit 1; }
  "$python" - "$source" "$zip" "$executable" <<'PY'
import sys, zipfile, time
source, target, name = sys.argv[1:4]
info = zipfile.ZipInfo(name, date_time=time.localtime()[:6])
info.compress_type = zipfile.ZIP_DEFLATED
with open(source, 'rb') as f, zipfile.ZipFile(target, 'w') as z:
    z.writestr(info, f.read())
PY
  echo "$zip"
}

python=$(command -v python3 || command -v python) || { echo "needs python3" >&2; exit 1; }

win=$(asset win-x64 Broiler.Plate.Windows.exe)

cat > "$out/notes.md" <<EOF
Broiler Plate **$version**, a preview build for evaluation and testing, not for
production use.

## Downloads

| Platform | File | Run |
| --- | --- | --- |
| Windows x64 | \`Broiler.Plate-$version-win-x64.zip\` | unzip, start \`Broiler.Plate.Windows.exe\` |

The zip holds a single self-contained NativeAOT executable: no .NET runtime to install,
nothing else to copy. The executable is not code-signed, so Windows SmartScreen may ask
before the first start.
EOF

gh release create "$tag" "$win" \
  --verify-tag \
  --draft \
  --prerelease \
  --title "Broiler Plate $version" \
  --notes-file "$out/notes.md" \
  --generate-notes
