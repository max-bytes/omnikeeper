# Build the app
FROM node:13.12.0-alpine AS build

ARG version

WORKDIR /app

# HACK: we need to install git because npm dependencies include a git repository
# TODO: move git repository depencency to proper npm repository, then remove this git installation
RUN apk add --no-cache git

# add `/app/node_modules/.bin` to $PATH
ENV PATH /app/node_modules/.bin:$PATH

COPY package.json ./
COPY package-lock.json ./
RUN npm ci

# add app
COPY . ./

RUN PUBLIC_URL=__PUBLIC_URL_PLACEHOLDER__ REACT_APP_VERSION="$version" npm run build

# Create deployable image
FROM docker-registry.mhx.at/infra-docker/container-images/react-frontend:latest

WORKDIR /var/www

COPY --from=build /app/build /var/www

COPY .env* /var/www/