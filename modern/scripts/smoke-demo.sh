#!/bin/sh
set -eu

: "${BANKOFZ_BASE_URL:=http://api:8080}"
: "${BANKOFZ_DEMO_USER:=customer}"
: "${BANKOFZ_DEMO_PASSWORD:?Set BANKOFZ_DEMO_PASSWORD}"

cookie_file=$(mktemp)
trap 'rm -f "$cookie_file"' EXIT

curl -fsS -c "$cookie_file" "$BANKOFZ_BASE_URL/api/session/csrf" >/dev/null
token=$(grep 'BankOfZ.XSRF-TOKEN' "$cookie_file" | cut -f7)
login=$(curl -fsS \
  -b "$cookie_file" \
  -c "$cookie_file" \
  -H "X-XSRF-TOKEN: $token" \
  -H "Content-Type: application/json" \
  -d "{\"userName\":\"$BANKOFZ_DEMO_USER\",\"password\":\"$BANKOFZ_DEMO_PASSWORD\",\"rememberMe\":false}" \
  "$BANKOFZ_BASE_URL/api/session/login")
accounts=$(curl -fsS \
  -b "$cookie_file" \
  "$BANKOFZ_BASE_URL/api/customers/1000000001/accounts")

printf '%s\n%s\n' "$login" "$accounts"
