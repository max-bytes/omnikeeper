#!/bin/bash

git_host=$1
swagger_file=$2
version=$3

git_user_id="landscape"
git_repo_id="omnikeeper-client-powershell"
release_note="Update to version ${version}"

if [ "$git_host" = "" ]; then
    echo "[INFO] No git_host provided."
    exit -1
fi

if [ "$swagger_file" = "" ]; then
    echo "[INFO] No swagger_file provided."
    exit -1
fi

if [ "$version" = "" ]; then
    version="0.0.0"
    echo "[INFO] No version provided, setting to ${version}"
fi

rm -rf build/powershell
mkdir -p build/powershell

cp $swagger_file build/omnikeeper.json

cd build/powershell

# make git remember credentials
git config --global credential.helper store
git config --global user.email "generator@mhx.at"
git config --global user.name "generator"

# Clone the current repo
if [ "$ACCESS_TOKEN_REPO_CLIENT_POWERSHELL" = "" ]; then
    echo "[INFO] \$ACCESS_TOKEN_REPO_CLIENT_POWERSHELL (environment variable) is not set. Using the git credential in your environment."
    git clone https://${git_host}/${git_user_id}/${git_repo_id}.git .
else
    git clone https://${git_user_id}:${ACCESS_TOKEN_REPO_CLIENT_POWERSHELL}@${git_host}/${git_user_id}/${git_repo_id}.git .
fi


# create updated library
echo "Generating client version ${version}"
docker run --rm -v "${PWD}/..:/local" -u `id -u $USER`:`id -g $USER` openapitools/openapi-generator-cli generate \
    -i /local/omnikeeper.json \
    -g powershell \
    -o /local/powershell \
    --global-property=verbose=true \
    --additional-properties=packageName=okclient,packageVersion=${version},apiNamePrefix=OK


# HACK:
find . -type f -name "*.ps1" -exec sed -r -i 's/\$AllProperties = \(\)/\$AllProperties = @\(\)/g' {} +

# Adds the files in the local repository and stages them for commit.
git add .

# Commits the tracked changes and prepares them to be pushed to a remote repository.
git commit -m "$release_note"

# Pushes the changes in the local repository up to the remote repository
git push

