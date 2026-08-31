# ---------- Giai đoạn build ----------
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Đường dẫn API được nhúng lúc build vì Vite thay thế biến môi trường ở thời điểm biên dịch.
ARG VITE_API_BASE_URL=/api
ARG VITE_BASE_PATH=/admin/
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_BASE_PATH=$VITE_BASE_PATH
RUN npm run build

# ---------- Giai đoạn chạy ----------
FROM nginx:1.27-alpine AS runtime

# SPA dùng client-side routing: mọi đường dẫn không phải file tĩnh đều trả index.html.
COPY nginx-spa.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
