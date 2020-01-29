FROM nginx

WORKDIR /usr/share/nginx/html
COPY ./www .

EXPOSE 80 443

CMD ["nginx", "-g", "daemon off;"]