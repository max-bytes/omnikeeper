#!/bin/sh

set -e

echo "Serializing environment:"
react-env --dest ./env_js

echo "Dynamically replacing PUBLIC_URL_PLACEHOLDER with PUBLIC_URL_DYNAMIC"
find . -type f \( -name "*.html" -o -name "*.js*" -o -name "*.json" -o -name "*.css" \) -print0 | xargs -0 sed -i'' -e "s/__PUBLIC_URL_PLACEHOLDER__/$(echo $PUBLIC_URL_DYNAMIC | sed -e 's/[\/&]/\\&/g')/g"

echo "Running..."

exec "$@"
