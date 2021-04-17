#!/bin/bash

git_user_id=$1
git_repo_id=$2
release_note=$3
git_host=$4
swagger_file=$5

if [ "$git_host" = "" ]; then
    echo "[INFO] No git_host provided."
    exit -1
fi

if [ "$git_user_id" = "" ]; then
    echo "[INFO] No git_user_id provided."
    exit -1
fi

if [ "$git_repo_id" = "" ]; then
    echo "[INFO] No git_repo_id provided."
    exit -1
fi

if [ "$release_note" = "" ]; then
    echo "[INFO] No release_note provided."
    exit -1
fi

if [ "$swagger_file" = "" ]; then
    echo "[INFO] No swagger_file provided."
    exit -1
fi

rm -rf build/go
mkdir -p build/go

# HACK: openapi-generator for go cannot deal with multipart/formdata due to a bug in the go code generator 
# that's why we remove any endpoint that contains this
cat $swagger_file \
    | jq 'del(.paths[] | select(.post.requestBody.content."multipart/form-data" != null))' \
    > build/omnikeeper_trimmed.json

cd build/go

# make git remember credentials
git config --global credential.helper store

# Clone the current repo
if [ "$ACCESS_TOKEN_REPO_CLIENT_GO" = "" ]; then
    echo "[INFO] \$ACCESS_TOKEN_REPO_CLIENT_GO (environment variable) is not set. Using the git credential in your environment."
    git clone https://${git_host}/${git_user_id}/${git_repo_id}.git .
else
    git clone https://${git_user_id}:${ACCESS_TOKEN_REPO_CLIENT_GO}@${git_host}/${git_user_id}/${git_repo_id}.git .
fi

# create updated library
docker run --rm -v "${PWD}/..:/local" -u `id -u $USER`:`id -g $USER` openapitools/openapi-generator-cli generate \
    -i /local/omnikeeper_trimmed.json \
    -g go \
    -o /local/go \
    -p enumClassPrefix=true \
    --git-host "${git_host}" --git-user-id "${git_user_id}" --git-repo-id "${git_repo_id}.git" \
    --global-property=verbose=true \
    --additional-properties=packageName=okclient,packageVersion=$VERSION
# Flag -p enumClassPrefix=true is necessary to avoid enum name clashes
# Flags --git-* are necessary so that the generated go.mod file contains the correct package definition

# Adds the files in the local repository and stages them for commit.
git add .

# Commits the tracked changes and prepares them to be pushed to a remote repository.
git commit -m "$release_note"

# Pushes the changes in the local repository up to the remote repository
git push

