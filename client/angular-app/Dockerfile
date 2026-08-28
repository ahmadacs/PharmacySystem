FROM node:22-alpine AS build
WORKDIR /app

COPY client/angular-app/package*.json ./
RUN npm ci --prefer-offline --no-audit --no-fund

COPY client/angular-app/ ./
RUN npm run build

FROM nginx:alpine
COPY client/angular-app/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/angular-app/browser /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]