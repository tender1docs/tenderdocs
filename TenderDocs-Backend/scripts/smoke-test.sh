#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# TenderDocs end-to-end smoke test — exercises the three fixed areas:
#   1. Project document assignment with an invalid RequirementId  -> no 500 / no FK error
#   2. Project ZIP export                                         -> valid, openable ZIP
#   3. Auth: register + login + protected route + refresh
#
# Usage:  BASE=http://localhost:8080 ./scripts/smoke-test.sh
# Requires: bash, curl, python3 (for JSON + zip validation), unzip optional.
# Run AFTER `docker compose up` is healthy.
# ---------------------------------------------------------------------------
set -uo pipefail
BASE="${BASE:-http://localhost:8080}"
API="$BASE/api"
PASS=0; FAIL=0
ok(){ echo "  PASS: $1"; PASS=$((PASS+1)); }
no(){ echo "  FAIL: $1"; FAIL=$((FAIL+1)); }
jq_get(){ python3 -c "import sys,json;print(json.load(sys.stdin).get('$1',''))"; }

echo "== 0. Protected route rejects anonymous =="
code=$(curl -s -o /dev/null -w '%{http_code}' "$API/documents")
[ "$code" = "401" ] && ok "GET /documents without token -> 401" || no "expected 401, got $code"

echo "== 1. Register a fresh workspace =="
EMAIL="smoke_$(date +%s)@example.com"
REG=$(curl -s -X POST "$API/auth/register" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Passw0rd!23\",\"fullName\":\"Smoke Test\",\"organizationName\":\"Smoke Co\"}")
TOKEN=$(echo "$REG" | jq_get accessToken)
REFRESH=$(echo "$REG" | jq_get refreshToken)
[ -n "$TOKEN" ] && ok "register returned a JWT" || { no "register failed: $REG"; echo "Cannot continue."; exit 1; }
AUTH=(-H "Authorization: Bearer $TOKEN")

echo "== 2. Login with the same credentials =="
LOGIN=$(curl -s -X POST "$API/auth/login" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Passw0rd!23\"}")
[ -n "$(echo "$LOGIN" | jq_get accessToken)" ] && ok "login returned a JWT" || no "login failed: $LOGIN"

echo "== 3. Refresh token =="
RF=$(curl -s -X POST "$API/auth/refresh" -H 'Content-Type: application/json' -d "{\"refreshToken\":\"$REFRESH\"}")
[ -n "$(echo "$RF" | jq_get accessToken)" ] && ok "refresh returned a new JWT" || no "refresh failed: $RF"

echo "== 4. Create a project =="
PROJ=$(curl -s -X POST "$API/projects" "${AUTH[@]}" -H 'Content-Type: application/json' \
  -d '{"name":"Smoke Project","description":"e2e","requirements":["GST","PAN"]}')
PID=$(echo "$PROJ" | jq_get id)
[ -n "$PID" ] && ok "project created ($PID)" || { no "create project failed: $PROJ"; exit 1; }

echo "== 5. Upload a document into the project =="
TMP=$(mktemp /tmp/smoke_XXXX.txt); echo "hello tender" > "$TMP"
UP=$(curl -s -X POST "$API/documents/upload" "${AUTH[@]}" \
  -F "File=@$TMP;type=text/plain" -F "DocumentType=Gst" -F "ProjectId=$PID")
DID=$(echo "$UP" | jq_get id)
[ -n "$DID" ] && ok "document uploaded + auto-assigned ($DID)" || no "upload failed: $UP"

echo "== 6. FIX #1: assign with a BOGUS RequirementId must NOT 500 / FK-error =="
TMP2=$(mktemp /tmp/smoke2_XXXX.txt); echo "second doc" > "$TMP2"
UP2=$(curl -s -X POST "$API/documents/upload" "${AUTH[@]}" -F "File=@$TMP2;type=text/plain" -F "DocumentType=Pan")
DID2=$(echo "$UP2" | jq_get id)
code=$(curl -s -o /tmp/assign_out -w '%{http_code}' -X POST "$API/projects/$PID/documents" "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d "{\"documentId\":\"$DID2\",\"requirementId\":\"00000000-0000-0000-0000-000000000123\"}")
if [ "$code" = "204" ] || [ "$code" = "200" ]; then ok "assign with invalid RequirementId -> $code (no FK violation)";
else no "assign returned $code: $(cat /tmp/assign_out)"; fi

echo "== 7. FIX #2: project ZIP export is a valid archive =="
curl -s "${AUTH[@]}" "$API/projects/$PID/zip" -o /tmp/smoke.zip
if python3 - <<'PY'
import zipfile,sys
try:
    z=zipfile.ZipFile('/tmp/smoke.zip')
    bad=z.testzip()
    print('  names:', z.namelist()[:5])
    sys.exit(1 if bad else 0)
except Exception as e:
    print('  not a valid zip:', e); sys.exit(1)
PY
then ok "ZIP downloaded and opens correctly"; else no "ZIP invalid/empty"; fi

echo "== 8. Per-document download returns bytes =="
sz=$(curl -s "${AUTH[@]}" "$API/documents/$DID/download" -o /tmp/smoke_dl.bin -w '%{size_download}')
[ "${sz:-0}" -gt 0 ] && ok "document download returned $sz bytes" || no "download empty"

rm -f "$TMP" "$TMP2" /tmp/smoke.zip /tmp/smoke_dl.bin /tmp/assign_out 2>/dev/null
echo ""
echo "==== RESULT: $PASS passed, $FAIL failed ===="
exit $([ "$FAIL" -eq 0 ] && echo 0 || echo 1)
