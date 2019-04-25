FROM node:10

RUN apt-get update

RUN apt-get install -y apt-utils build-essential

WORKDIR /usr/src/app

COPY package*.json ./

RUN npm install

COPY . .

ENTRYPOINT ["./run.sh", "100"]