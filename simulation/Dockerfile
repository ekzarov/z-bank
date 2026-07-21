FROM node:24-alpine

WORKDIR /app
COPY package.json ./
COPY fixtures ./fixtures
COPY src ./src
COPY scripts ./scripts

EXPOSE 9080
CMD ["node", "src/server.js"]
