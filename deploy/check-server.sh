#!/usr/bin/env bash
# Looks at a new server and prints what it really has (processor, memory, disk, speed, system).
# It changes nothing on the server: the only thing written is one temporary file for the disk speed test, and it is
# deleted at the end.
#
# From your own computer (the password is typed by you, here nobody else sees it):
#   ssh root@SERVER_IP 'bash -s' < deploy/check-server.sh
set -u

line() { printf '\n==== %s ====\n' "$1"; }

line "System"
. /etc/os-release 2>/dev/null && echo "OS: ${PRETTY_NAME:-unknown}"
echo "Kernel: $(uname -r)"
echo "Uptime: $(uptime -p 2>/dev/null)"
echo "Virtualization: $(systemd-detect-virt 2>/dev/null || echo unknown)"

line "Processor"
echo "Cores seen by the system: $(nproc)"
lscpu 2>/dev/null | grep -E "Model name|^CPU\(s\)|Thread|Core\(s\)|Socket|MHz|L3" || true

line "Memory"
free -h
echo
grep -E "MemTotal|SwapTotal" /proc/meminfo

line "Disk"
lsblk -o NAME,SIZE,TYPE,ROTA,MOUNTPOINT 2>/dev/null || true
echo "(ROTA 0 = SSD/NVMe, 1 = a spinning disk)"
df -h / | tail -n +1

line "Disk speed (writes 1 GB once, then deletes it)"
dd if=/dev/zero of=/root/.rentox-speedtest bs=1M count=1024 oflag=direct 2>&1 | tail -n 1
rm -f /root/.rentox-speedtest

line "Network"
ip -4 -o addr show scope global 2>/dev/null | awk '{print $2, $4}'
echo "Public address as the world sees it: $(curl -s -m 8 https://ifconfig.me || echo 'could not reach')"
echo -n "Reach the internet (cloudflare): "
(curl -s -o /dev/null -m 8 -w '%{http_code}\n' https://www.cloudflare.com) || echo failed
echo -n "Download speed from a test file (MB/s): "
curl -s -m 40 -o /dev/null -w '%{speed_download}\n' https://speed.cloudflare.com/__down?bytes=50000000 2>/dev/null \
  | awk '{printf "%.1f\n", $1/1048576}' || echo failed

line "What is already installed or listening"
for tool in docker git curl ufw fail2ban; do
  if command -v "$tool" >/dev/null 2>&1; then echo "$tool: yes"; else echo "$tool: no"; fi
done
echo
echo "Listening ports:"
ss -tulpn 2>/dev/null | awk 'NR==1 || /LISTEN|UNCONN/' | head -n 20
echo
echo "Firewall:"
(ufw status 2>/dev/null || echo "ufw is not installed") | head -n 8

line "Who can log in"
echo "Users with a shell:"
grep -E '/(bash|sh|zsh)$' /etc/passwd | cut -d: -f1 | tr '\n' ' '
echo
grep -E '^(PermitRootLogin|PasswordAuthentication)' /etc/ssh/sshd_config 2>/dev/null || true

printf '\nDone.\n'
