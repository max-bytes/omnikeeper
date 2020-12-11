# Build the app
FROM node:13.12.0-alpine AS build

ARG version
ARG npm_repo_key

WORKDIR /app

# add `/app/node_modules/.bin` to $PATH
ENV PATH /app/node_modules/.bin:$PATH

COPY package.json ./
COPY package-lock.json ./

# add registry and auth for npm
RUN echo -e "registry=https://repo.mhx.at/repository/npm-stable/\n_auth=${npm_repo_key}" > .npmrc

#debug
RUN cat .npmrc
RUN echo "${npm_repo_key}"

# fetch dependencies
RUN npm ci

# add app
COPY . ./

# build
RUN PUBLIC_URL=__PUBLIC_URL_PLACEHOLDER__ REACT_APP_VERSION="$version" npm run build

# Create deployable image
FROM docker-registry.mhx.at/infra-docker/container-images/react-frontend:latest

WORKDIR /var/www

COPY --from=build /app/build /var/www

COPY .env* /var/www/