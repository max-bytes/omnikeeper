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

# add url and auth for private npm repository

RUN echo -e "@maximiliancsuk:registry=https://npm.pkg.github.com\n//npm.pkg.github.com/:_authToken=${npm_repo_key}" > .npmrc

# fetch dependencies
RUN npm ci

# install plugins, if any are defined
RUN ([[ ! -z "$plugins" ]] && npm install $plugins) || true

# add app
COPY . ./

# build
RUN PUBLIC_URL=__PUBLIC_URL_PLACEHOLDER__ npm run build

# Create deployable image
FROM nginx:1.15-alpine
ARG version
ARG plugins

RUN apk add --no-cache nodejs yarn
RUN yarn global add @beam-australia/react-env
COPY cicd/nginx.conf /etc/nginx/nginx.conf
COPY cicd/entrypoint.sh /var/entrypoint.sh
RUN chmod +x /var/entrypoint.sh
ENTRYPOINT ["/var/entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]

WORKDIR /var/www

COPY --from=build /app/build /var/www

# copy default environment file to set defaults that need to be overwritten
COPY .env /var/www/

ENV REACT_APP_VERSION=$version
ENV REACT_APP_PLUGINS_FRONTEND=$plugins
