#!/bin/bash

   
git_host=$1
swagger_file=$2
version=$3
git_user_id=$4
git_ssh_file=$5

git_repo_id="omnikeeper-client-go"
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
git config --global user.email "generator@mhx.at"
git config --global user.name "generator"

# Clone the current repo
if [ "$git_ssh_file" = "" ]; then
    echo "[INFO] \$git_password is not set. Using the git credential in your environment."
    git clone https://${git_host}/${git_user_id}/${git_repo_id}.git .
else
    echo "$git_ssh_file" > ~/id_rsa
    chmod 600 ~/id_rsa
    cat ~/id_rsa
    GIT_SSH_COMMAND='ssh -i ~/id_rsa -o IdentitiesOnly=yes' git clone git@${git_host}:${git_user_id}/${git_repo_id}.git .
fi


# create updated library
echo "Generating client version ${version}"
docker run --rm -v "${PWD}/..:/local" -u `id -u $USER`:`id -g $USER` openapitools/openapi-generator-cli generate \
    -i /local/omnikeeper_trimmed.json \
    -g go \
    -o /local/go \
    -p enumClassPrefix=true \
    --git-host "${git_host}" --git-user-id "${git_user_id}" --git-repo-id "${git_repo_id}.git" \
    --global-property=verbose=true \
    --additional-properties=packageName=okclient,packageVersion=${version}
# Flag -p enumClassPrefix=true is necessary to avoid enum name clashes
# Flags --git-* are necessary so that the generated go.mod file contains the correct package definition

# Adds the files in the local repository and stages them for commit.
git add .

# Commits the tracked changes and prepares them to be pushed to a remote repository.
git commit -m "$release_note"

# Pushes the changes in the local repository up to the remote repository
git push

