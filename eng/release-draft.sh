#!/usr/bin/env bash
# Creates the draft GitHub pre-release for one Broiler.Plate version.
#
#   eng/release-draft.sh <version> <artifacts-dir>
#
# <artifacts-dir> holds the Publish workflow's artifacts, extracted as
# `gh run download` / actions/download-artifact leave them:
#
#   <artifacts-dir>/broiler-plate-win-x64-self-contained-<version>/Broiler.Plate.Windows.exe
#   <artifacts-dir>/broiler-plate-win-x64-framework-dependent-<version>/Broiler.Plate.Windows.exe, *.dll, ...
#   <artifacts-dir>/broiler-plate-linux-x64-self-contained-<version>/Broiler.Plate.Linux
#   <artifacts-dir>/broiler-plate-linux-x64-framework-dependent-<version>/Broiler.Plate.Linux, *.dll, ...
#   <artifacts-dir>/broiler-plate-web-<version>/Broiler.Plate-<version>-browser-wasm.zip
#
# Each desktop artifact is zipped into one release asset,
# Broiler.Plate-<version>-<rid>-<variant>.zip: the self-contained one holds the NativeAOT
# executable alone, the framework-dependent one the whole publish folder. The executable is
# recorded as rwxr-xr-x and every other file as rw-r--r--: the workflow artifact drops the
# Linux binary's executable bit, the release zip restores it.
# The WebAssembly package is verified from its web-artifact descriptor and attached as a release asset.
#
# The tag plate-v<version> must already exist on the remote; the release is created
# for it as a draft, so nothing is public until someone publishes it. Needs the GitHub
# CLI with GH_TOKEN (or a login) that may write releases, and Python 3.
set -euo pipefail

version=${1:?usage: eng/release-draft.sh <version> <artifacts-dir>}
artifacts=${2:?usage: eng/release-draft.sh <version> <artifacts-dir>}
tag="plate-v$version"
out=$(mktemp -d)
trap 'rm -rf "$out"' EXIT
: "${PUBLISH_WINDOWS:=true}" "${PUBLISH_LINUX:=true}" "${PUBLISH_WEB:=true}" "${CREATE_DRAFT:=true}"
if [ "$PUBLISH_WINDOWS$PUBLISH_LINUX$PUBLISH_WEB" = falsefalsefalse ]; then
  echo 'Select at least one publish target.' >&2; exit 1
fi

# Python writes the zip so the Unix mode is recorded explicitly instead of read from the
# file system, which on Windows has no executable bit to read.
asset() {
  local rid=$1 variant=$2 executable=$3
  local source="$artifacts/broiler-plate-$rid-$variant-$version"
  local zip="$out/Broiler.Plate-$version-$rid-$variant.zip"
  [ -f "$source/$executable" ] || { echo "missing $source/$executable" >&2; exit 1; }
  if [ "$variant" = self-contained ] && [ "$(find "$source" -type f | wc -l)" -ne 1 ]; then
    echo "$source should hold $executable alone" >&2; exit 1
  fi
  "$python" - "$source" "$zip" "$executable" <<'PY'
import os, sys, zipfile, time
source, target, executable = sys.argv[1:4]
stamp = time.localtime()[:6]
with zipfile.ZipFile(target, 'w') as z:
    for root, dirs, files in os.walk(source):
        dirs.sort()
        for name in sorted(files):
            path = os.path.join(root, name)
            arcname = os.path.relpath(path, source).replace(os.sep, '/')
            info = zipfile.ZipInfo(arcname, date_time=stamp)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.create_system = 3          # Unix, so external_attr carries a mode
            mode = 0o755 if arcname == executable else 0o644
            info.external_attr = (0o100000 | mode) << 16   # regular file
            with open(path, 'rb') as f:
                z.writestr(info, f.read())
    for license_name in ('LICENSE', 'LICENSE.md', 'LICENSE.txt'):
        if os.path.isfile(license_name):
            if 'LICENSE' not in z.namelist():
                z.write(license_name, 'LICENSE')
            break
PY
  echo "$zip"
}

python=$(command -v python3 || command -v python) || { echo "needs python3" >&2; exit 1; }

# One assignment per asset: under set -e a failing $(...) stops the script only when it is
# the whole assignment, not one element of an array literal.
desktop_assets=()
for rid in win-x64 linux-x64; do
  if [ "$rid" = win-x64 ] && [ "$PUBLISH_WINDOWS" != true ]; then continue; fi
  if [ "$rid" = linux-x64 ] && [ "$PUBLISH_LINUX" != true ]; then continue; fi
  if [ "$rid" = win-x64 ]; then executable=Broiler.Plate.Windows.exe; else executable=Broiler.Plate.Linux; fi
  for variant in self-contained framework-dependent; do
    zip=$(asset "$rid" "$variant" "$executable")
    desktop_assets+=("$zip")
  done
done

web_assets=()
if [ "$PUBLISH_WEB" = true ]; then
  web_source="$artifacts/broiler-plate-web-$version"
  web_zip="$out/Broiler.Plate-$version-browser-wasm.zip"
  web_metadata="$web_source/Broiler.Plate-$version-browser-wasm.web-artifact.json"
  web_sums="$web_source/Broiler.Plate-$version-browser-wasm.SHA256SUMS.txt"
  "$python" eng/web/artifact.py verify --archive "$web_source/$(basename "$web_zip")" --metadata "$web_metadata" --require-release --product plate --version "$version" --tag "$tag" --commit "$(git rev-parse HEAD)" --repository "${GITHUB_REPOSITORY:-Broiler-Platform/Broiler.Plate}"
  cp "$web_source/$(basename "$web_zip")" "$web_zip"
  web_assets=("$web_zip" "$web_metadata" "$web_sums")
fi

desktop_rows=''
for path in "${desktop_assets[@]}"; do
  desktop_rows+="| Desktop | \`$(basename "$path")\` | extract the complete ZIP |"$'\n'
done
web_rows=''
if [ "$PUBLISH_WEB" = true ]; then
  web_rows="| WebAssembly | \`Broiler.Plate-$version-browser-wasm.zip\` | complete static browser application |"
fi
cat > "$out/notes.md" <<EOF
Broiler Plate **$version**, a preview build for evaluation and testing, not for
production use.

## Downloads

| Platform | File | Run |
| --- | --- | --- |
$desktop_rows
$web_rows

The **self-contained** zips hold a single NativeAOT executable: no .NET runtime to install,
nothing else to copy. The **framework-dependent** zips hold the executable together with the
application's assemblies and run on the .NET 10 runtime installed on the machine, so they
pick up its servicing updates. The executables are not code-signed, so Windows SmartScreen
may ask before the first start.
EOF

"$python" eng/write-component-manifest.py --directory "$out" --product plate --version "$version" --repository "${GITHUB_REPOSITORY:-Broiler-Platform/Broiler.Plate}" --tag "$tag" --commit "$(git rev-parse HEAD)" --self-contained-mode native-aot

draft_args=()
if [ "$CREATE_DRAFT" = true ]; then draft_args+=(--draft); fi
gh release create "$tag" "${desktop_assets[@]}" "${web_assets[@]}" "$out/component-release.json" "$out/SHA256SUMS.txt" \
  --verify-tag \
  "${draft_args[@]}" \
  --prerelease \
  --title "Broiler Plate $version" \
  --notes-file "$out/notes.md" \
  --generate-notes
