# Build the app
FROM node:13.12.0-alpine AS build

WORKDIR /app

# add `/app/node_modules/.bin` to $PATH
ENV PATH /app/node_modules/.bin:$PATH

COPY package.json ./
COPY package-lock.json ./
RUN npm install
RUN npm install react-scripts@3.4.1 -g

# # add app
COPY . ./

RUN npm run build

# Create deployable image
FROM beamaustralia/react-env:latest

WORKDIR /var/www

COPY --from=build /app/build /var/www

COPY .env* /var/www/