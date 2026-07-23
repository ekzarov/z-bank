#!/bin/sh
set -eu

: "${BANKOFZ_BASE_URL:=http://localhost:8088}"
: "${BANKOFZ_DEMO_PASSWORD:?Set BANKOFZ_DEMO_PASSWORD}"

cookie_file=$(mktemp)
before_file=$(mktemp)
after_file=$(mktemp)
trap 'rm -f "$cookie_file" "$before_file" "$after_file"' EXIT

curl -fsS -c "$cookie_file" "$BANKOFZ_BASE_URL/api/session/csrf" >/dev/null
token=$(awk '$6 == "BankOfZ.XSRF-TOKEN" { print $7 }' "$cookie_file")
curl -fsS -b "$cookie_file" -c "$cookie_file" \
  -H "X-XSRF-TOKEN: $token" -H "Content-Type: application/json" \
  -d "{\"userName\":\"customer\",\"password\":\"$BANKOFZ_DEMO_PASSWORD\",\"rememberMe\":false}" \
  "$BANKOFZ_BASE_URL/api/session/login" >/dev/null
curl -fsS -b "$cookie_file" \
  "$BANKOFZ_BASE_URL/api/customers/1000000001/accounts" >"$before_file"

docker compose restart db api ui >/dev/null
attempt=0
until curl -fsS "$BANKOFZ_BASE_URL/health/ready" >/dev/null; do
  attempt=$((attempt + 1))
  [ "$attempt" -lt 30 ] || { echo "Readiness did not recover after restart." >&2; exit 1; }
  sleep 2
done

rm -f "$cookie_file"
curl -fsS -c "$cookie_file" "$BANKOFZ_BASE_URL/api/session/csrf" >/dev/null
token=$(awk '$6 == "BankOfZ.XSRF-TOKEN" { print $7 }' "$cookie_file")
curl -fsS -b "$cookie_file" -c "$cookie_file" \
  -H "X-XSRF-TOKEN: $token" -H "Content-Type: application/json" \
  -d "{\"userName\":\"customer\",\"password\":\"$BANKOFZ_DEMO_PASSWORD\",\"rememberMe\":false}" \
  "$BANKOFZ_BASE_URL/api/session/login" >/dev/null
curl -fsS -b "$cookie_file" \
  "$BANKOFZ_BASE_URL/api/customers/1000000001/accounts" >"$after_file"

cmp "$before_file" "$after_file" >/dev/null ||
  { echo "Read-only account data changed across restart." >&2; exit 1; }

printf 'Restart and SQL-volume persistence verification passed.\n'
