FROM snowdreamtech/frps:0.58.1

COPY ./frps.toml /etc/frp/frps.toml

ENTRYPOINT ["/usr/bin/frps", "-c", "/etc/frp/frps.toml"]
