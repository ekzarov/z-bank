#!/bin/sh
set -eu

: "${BANKOFZ_BASE_URL:=http://api:8080}"
: "${BANKOFZ_DEMO_PASSWORD:?Set BANKOFZ_DEMO_PASSWORD}"

work_dir=$(mktemp -d)
trap 'rm -rf "$work_dir"' EXIT

curl -fsS "$BANKOFZ_BASE_URL/health/live" >/dev/null
curl -fsS "$BANKOFZ_BASE_URL/health/ready" >/dev/null
curl -fsS "$BANKOFZ_BASE_URL/api/configuration/bank" >/dev/null

for user in customer operator administrator; do
  cookie_file="$work_dir/$user.cookies"
  curl -fsS -c "$cookie_file" "$BANKOFZ_BASE_URL/api/session/csrf" >/dev/null
  token=$(awk '$6 == "BankOfZ.XSRF-TOKEN" { print $7 }' "$cookie_file")
  curl -fsS \
    -b "$cookie_file" \
    -c "$cookie_file" \
    -H "X-XSRF-TOKEN: $token" \
    -H "Content-Type: application/json" \
    -d "{\"userName\":\"$user\",\"password\":\"$BANKOFZ_DEMO_PASSWORD\",\"rememberMe\":false}" \
    "$BANKOFZ_BASE_URL/api/session/login" >/dev/null
done

curl -fsS -b "$work_dir/customer.cookies" \
  "$BANKOFZ_BASE_URL/api/customers/1000000001/accounts" >/dev/null

printf 'Bank of Z smoke passed for customer, operator, and administrator.\n'
