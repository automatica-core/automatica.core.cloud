[Unit]
Description=frps

[Service]
ExecStart=/usr/bin/frps -c /etc/frp/frps.ini
Restart=on-failure
RestartSec=30

[Install]
WantedBy=multi-user.target