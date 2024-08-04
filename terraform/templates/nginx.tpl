server {
        listen 80 default_server;
        listen [::]:80 default_server;

        return 301 https://$host$request_uri;
}
server {
       
        listen 443 ssl default_server;
        listen [::]:443 ssl default_server;
        ssl_certificate /etc/nginx/certs/automaticaremote.crt;
        ssl_certificate_key /etc/nginx/certs/automaticaremote.key;

        server_name ~^(?<subdomain>.+)\.${domain}\.${domain_end}$;

        location / {
                proxy_pass https://localhost:8443;
                proxy_ssl_verify        off;
                proxy_ssl_session_reuse off;

                proxy_ssl_name $host;
                proxy_ssl_server_name on;

                proxy_http_version 1.1;
                proxy_set_header Upgrade $http_upgrade;
                proxy_set_header Connection "upgrade";

                proxy_set_header X-Real-IP $remote_addr;
                proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;

                include proxy_params;
        }

}
