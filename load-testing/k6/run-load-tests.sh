#!/usr/bin/env bash
set -e

echo "=========================================================="
echo "   SOCAR Dispatch — k6 Emergency Load & Stress Runner     "
echo "=========================================================="

API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
SIGNALR_WS_URL="${SIGNALR_WS_URL:-ws://localhost:5000}"
AUTH_TOKEN="${AUTH_TOKEN:-}"

command -v k6 >/dev/null 2>&1 || {
  echo "❌ Error: k6 is not installed. Please install k6 (e.g., 'brew install k6')."
  exit 1
}

echo "Target API: $API_BASE_URL"
echo "Target WebSocket: $SIGNALR_WS_URL"
echo ""

echo "▶ 1. Running Scenario: Emergency Incident Surge (500 VUs)..."
k6 run \
  -e API_BASE_URL="$API_BASE_URL" \
  -e AUTH_TOKEN="$AUTH_TOKEN" \
  load-testing/k6/scenarios/emergency_surge.js

echo ""
echo "▶ 2. Running Scenario: WebSocket Location Telemetry Stream (200 VUs)..."
k6 run \
  -e SIGNALR_WS_URL="$SIGNALR_WS_URL" \
  -e AUTH_TOKEN="$AUTH_TOKEN" \
  load-testing/k6/scenarios/websocket_location_stream.js

echo ""
echo "✅ All k6 load tests completed successfully."
