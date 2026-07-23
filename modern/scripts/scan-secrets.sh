#!/bin/sh
set -eu
node "$(dirname "$0")/scan-secrets.mjs"
