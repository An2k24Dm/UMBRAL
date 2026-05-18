# Contenedor de desarrollo Expo/Metro.
# El emulador Android o un dispositivo físico se conectan EXTERNAMENTE al
# Metro Bundler expuesto por este contenedor en los puertos 19000/19001/19002/8081.
# Para Android Emulator usa http://10.0.2.2:5000 hacia el api-gateway.
FROM node:20-alpine
WORKDIR /app

# Herramientas comunes que Expo/Metro pueden necesitar en Alpine.
RUN apk add --no-cache bash git

COPY package.json ./
RUN npm install --no-audit --no-fund

COPY . ./

ENV EXPO_DEVTOOLS_LISTEN_ADDRESS=0.0.0.0
ENV REACT_NATIVE_PACKAGER_HOSTNAME=0.0.0.0
ENV CI=1

EXPOSE 19000 19001 19002 8081

# Sin --tunnel para no depender de ngrok dentro del contenedor.
CMD ["npx", "expo", "start", "--lan", "--port", "19000"]
