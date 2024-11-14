# Usar una imagen base
FROM node:14

# Crear el directorio de la aplicación
WORKDIR /app

# Copiar archivos package.json y package-lock.json e instalar dependencias
COPY package*.json ./
RUN npm install

# Copiar el resto de los archivos de la aplicación
COPY . .

# Exponer el puerto que usa la aplicación
EXPOSE 3000

# Comando para iniciar la aplicación
CMD ["npm", "start"]

