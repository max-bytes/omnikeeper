# Build the app
FROM node:13.12.0-alpine AS build

ARG version
ARG npm_repo_key
ARG plugins

WORKDIR /app

# add `/app/node_modules/.bin` to $PATH
ENV PATH /app/node_modules/.bin:$PATH

COPY package.json ./
COPY package-lock.json ./

# add url and auth for private mhx npm repository
RUN echo -e "registry=https://repo.mhx.at/repository/npm-stable/\n_auth=${npm_repo_key}" > .npmrc

# fetch dependencies
RUN npm ci

# install plugins, if any are defined
RUN [[ ! -z "$plugins" ]] && npm install $plugins

# add app
COPY . ./

# build
RUN PUBLIC_URL=__PUBLIC_URL_PLACEHOLDER__ REACT_APP_VERSION="$version" npm run build

# Create deployable image
FROM docker-registry.mhx.at/infra-docker/container-images/react-frontend:latest

WORKDIR /var/www

COPY --from=build /app/build /var/www

COPY .env* /var/www/