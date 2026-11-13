#!/usr/bin/env bash
# Store screenshots of the Android app, generated end to end:
#   1. starts the API (E2E environment, demo data from DemoDataSeeder) on :5253 and the SPA (start:mobile) on :4300
#   2. uses the running emulator/device, or boots an AVD
#   3. sets the screen to 1080x2160 (Google Play allows at most 2:1) and a clean status bar (demo mode)
#   4. installs a Debug build and runs flow.yaml with Maestro, once per theme
#   5. copies the screenshots to Mobile/screenshots/output/<theme>/ and restores the device
#
# Usage: Mobile/screenshots/screenshots.sh [--theme light|dark|both] [--avd NAME] [--skip-build] [--output DIR]
# Needs: .NET SDK + maui-android workload, Node (Frontend/node_modules installed), the Android SDK and Maestro
# (brew install mobile-dev-inc/tap/maestro). Port 4300 must be free (stop `npm run e2e` / `start:mobile` first).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
HERE="$ROOT/Mobile/screenshots"
OUTPUT="$HERE/output"
THEME=light
AVD=""
SKIP_BUILD=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --theme) THEME="$2"; shift 2 ;;
    --avd) AVD="$2"; shift 2 ;;
    --skip-build) SKIP_BUILD=true; shift ;;
    --output) OUTPUT="$2"; shift 2 ;;
    -h|--help) sed -n 2,13p "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done
case "$THEME" in light|dark|both) ;; *) echo "--theme must be light, dark or both" >&2; exit 2 ;; esac

API_PORT=5253
WEB_PORT=4300 # the Debug app's SPA URL (http://10.0.2.2:4300)
# Demo users only exist in this throwaway SQLite database; DemoDataSeeder.ScreenshotUser is the one used.
USERNAME=lotte
PASSWORD='Demo-Passw0rd!'
APP_ID=nl.scoutingwilo.ssskl
SCREEN_SIZE=1080x2160

ANDROID_SDK="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-$HOME/Library/Android/sdk}}"
export PATH="$ANDROID_SDK/platform-tools:$ANDROID_SDK/emulator:$PATH"
export MAESTRO_CLI_NO_ANALYTICS=1 MAESTRO_CLI_ANALYSIS_NOTIFICATION_DISABLED=true

LOGS="$OUTPUT/logs"
WORK="$(mktemp -d)"
mkdir -p "$LOGS"

log() { printf '\033[1;32m==>\033[0m %s\n' "$*"; }
fail() { printf '\033[1;31mError:\033[0m %s\n' "$*" >&2; exit 1; }

for tool in dotnet npm adb emulator maestro curl; do
  command -v "$tool" >/dev/null || fail "$tool not found (Android SDK expected in $ANDROID_SDK; Maestro: brew install mobile-dev-inc/tap/maestro)"
done
for port in $API_PORT $WEB_PORT; do
  if lsof -iTCP:"$port" -sTCP:LISTEN >/dev/null 2>&1; then
    fail "port $port is in use; stop what's running there (npm run e2e, start:mobile, a previous run) and try again"
  fi
done

# --- cleanup -------------------------------------------------------------------------------------------------
PIDS=()
STARTED_EMULATOR=false
DEVICE_READY=false
NIGHT_MODE=""

kill_tree() {
  local pid=$1 child
  for child in $(pgrep -P "$pid" 2>/dev/null); do kill_tree "$child"; done
  kill "$pid" 2>/dev/null || true
}

cleanup() {
  set +e
  if $DEVICE_READY; then
    adb shell wm size reset >/dev/null 2>&1
    adb shell am broadcast -a com.android.systemui.demo -e command exit >/dev/null 2>&1
    [[ -n "$NIGHT_MODE" ]] && adb shell cmd uimode night "$NIGHT_MODE" >/dev/null 2>&1
  fi
  $STARTED_EMULATOR && adb emu kill >/dev/null 2>&1
  for pid in "${PIDS[@]}"; do kill_tree "$pid"; done
  rm -rf "$WORK"
}
trap cleanup EXIT
trap 'exit 130' INT TERM

wait_for_url() { # url, name, seconds
  local i
  for ((i = 0; i < $3; i += 2)); do
    curl -sf -o /dev/null "$1" && return 0
    sleep 2
  done
  fail "$2 didn't come up on $1 (logs in $LOGS)"
}

# --- API and SPA ---------------------------------------------------------------------------------------------
log "Building the API"
dotnet build "$ROOT/Backend/SSSKL.Web" -v quiet -nologo > "$LOGS/api-build.log" 2>&1 || fail "API build failed, see $LOGS/api-build.log"

log "Starting the API with demo data on :$API_PORT"
ASPNETCORE_ENVIRONMENT=E2E \
E2E__DemoData=true \
E2E__UserPassword="$PASSWORD" \
ConnectionStrings__db="Data Source=ssskl-screenshots.db" \
Storage__LocalRoot=screenshots-blobs \
  dotnet run --project "$ROOT/Backend/SSSKL.Web" --no-build --no-launch-profile --urls "http://localhost:$API_PORT" \
  > "$LOGS/api.log" 2>&1 &
PIDS+=($!)

log "Starting the SPA on :$WEB_PORT"
(cd "$ROOT/Frontend" && SSSKL_API_URL="http://localhost:$API_PORT" exec npm run start:mobile) > "$LOGS/spa.log" 2>&1 &
PIDS+=($!)

# --- device --------------------------------------------------------------------------------------------------
if ! adb get-state >/dev/null 2>&1; then
  [[ -n "$AVD" ]] || AVD="$(emulator -list-avds | head -n 1)"
  [[ -n "$AVD" ]] || fail "no device connected and no AVD found; create one in Android Studio's Device Manager"
  log "Booting emulator $AVD"
  emulator -avd "$AVD" -no-snapshot-save -no-boot-anim > "$LOGS/emulator.log" 2>&1 &
  STARTED_EMULATOR=true
fi
adb wait-for-device
for _ in $(seq 1 120); do
  [[ "$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == 1 ]] && break
  sleep 2
done
DEVICE_READY=true
NIGHT_MODE="$(adb shell cmd uimode night 2>/dev/null | tr -d '\r' | awk '{print $NF}')"

if ! $SKIP_BUILD; then
  log "Building and installing the app (Debug)"
  # Assemblies inside the APK: Maestro's clearState wipes the app's data, where Fast Deployment keeps them.
  dotnet build "$ROOT/Mobile/SSSKL.Mobile" -t:Install -f net11.0-android -p:SssklTargetFrameworks=net11.0-android \
    -p:EmbedAssembliesIntoApk=true -v quiet -nologo > "$LOGS/app-build.log" 2>&1 \
    || fail "app build failed, see $LOGS/app-build.log"
fi
adb shell pm path "$APP_ID" >/dev/null 2>&1 || fail "$APP_ID isn't installed (run without --skip-build)"

log "Preparing the screen ($SCREEN_SIZE, demo status bar)"
adb shell wm size "$SCREEN_SIZE"
adb shell settings put global sysui_demo_allowed 1
demo() { adb shell am broadcast -a com.android.systemui.demo -e command "$@" >/dev/null; }
demo enter
demo clock -e hhmm 1200
demo battery -e level 100 -e plugged false
demo network -e wifi show -e level 4
demo network -e mobile show -e datatype none -e level 4
demo notifications -e visible false

wait_for_url "http://localhost:$API_PORT/healthz/live" "The API" 240
wait_for_url "http://localhost:$WEB_PORT" "The SPA" 300

# --- screenshots ---------------------------------------------------------------------------------------------
run_theme() {
  local theme=$1 night=no dest="$OUTPUT/$1" results="$WORK/$1"
  [[ "$theme" == dark ]] && night=yes
  log "Taking $theme screenshots"
  adb shell cmd uimode night "$night" >/dev/null
  sleep 2
  mkdir -p "$results"
  maestro test --test-output-dir="$results" -e USERNAME="$USERNAME" -e PASSWORD="$PASSWORD" "$HERE/flow.yaml" \
    > "$LOGS/maestro-$theme.log" 2>&1 \
    || fail "the Maestro flow failed ($theme); see $LOGS/maestro-$theme.log and the debug output in $results"

  rm -rf "$dest"
  mkdir -p "$dest"
  find "$results" -path '*/takeScreenshot/*.png' -exec cp {} "$dest/" \;
  local file width height count=0
  for file in "$dest"/*.png; do
    width=$(sips -g pixelWidth "$file" | awk '/pixelWidth/ {print $2}')
    height=$(sips -g pixelHeight "$file" | awk '/pixelHeight/ {print $2}')
    if ((height > 2 * width || width > 2 * height)); then
      echo "Warning: $(basename "$file") is ${width}x${height}; Google Play allows at most 2:1" >&2
    fi
    count=$((count + 1))
  done
  log "$count screenshots in $dest"
}

if [[ "$THEME" == both ]]; then
  run_theme light
  run_theme dark
else
  run_theme "$THEME"
fi
