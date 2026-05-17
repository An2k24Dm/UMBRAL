FROM node:20-alpine
WORKDIR /app

# Caché de capas para dependencias.
COPY package.json ./
RUN npm install --no-audit --no-fund

# Código.
COPY . ./

ENV HOST=0.0.0.0
ENV PORT=3000
EXPOSE 3000

# Vite en modo dev escuchando en 0.0.0.0:3000
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3000"]
