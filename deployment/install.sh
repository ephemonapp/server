#!/bin/bash

set -e


if [ -z "${BASH_VERSION:-}" ]; then
  printf "\033[31m%s\n\033[0m" "This installer needs bash, not sh." >&2
  printf "%s\n" "  curl -sSL https://get.ephemon.app | bash" >&2
  exit 1
fi

readonly INSTALLER_URL="https://get.ephemon.app"

# Every prompt is read from file descriptor 3 rather than stdin, because stdin
# is where the script itself arrives when it is piped:
#   curl -sSL https://get.ephemon.app | bash
# Reading answers from the same pipe would eat the rest of the script.
if [ -t 0 ]; then
  exec 3<&0
elif (exec 3< /dev/tty) 2> /dev/null; then
  exec 3< /dev/tty
else
  printf "\033[31m%s\n\033[0m" "This installer is interactive and found no terminal to ask questions on."
  printf "%s\n" "Run it from a shell you are sitting in front of:"
  printf "%s\n" "  curl -sSL https://get.ephemon.app | bash"
  exit 1
fi

umask 022

NOW=$(date +%Y%m%d%H%M)
DIRECTORY="ephemon-$NOW"
DIRECTORY_CREATED=0
PRESERVE_DIRECTORY=0

TURN_MIN_PORT=10000
TURN_MAX_PORT=20000
TURN_PORT=3478

readonly REQUIRED_DOCKER_VERSION="26.1.1"
readonly REQUIRED_DOCKER_COMPOSE_VERSION="v2.27.0"

readonly CLIENT_REPO="ephemonapp/client"
readonly CLIENT_ASSET_PATTERN="ephemon-client-oss"

SUDO=()
DOCKER=()
PRIVILEGE_ESCALATION_CHECKED=0

red()    { printf "\033[31m%s\n\033[0m" "$1"; }
green()  { printf "\033[32m%s\n\033[0m" "$1"; }
yellow() { printf "\033[33m%s\n\033[0m" "$1"; }
bold()   { printf "\033[1m%s\n\033[0m" "$1"; }


detect_privilege_escalation() {
  SUDO=()
  PRIVILEGE_ESCALATION_CHECKED=1

  [ "$(id -u)" = "0" ] && return 0

  if command -v sudo > /dev/null 2>&1; then
    SUDO=(sudo)
    return 0
  fi

  return 1
}

run_with_optional_sudo() {
  local __REASON__="$1"
  shift

  local __STATUS__
  if "$@"; then
    return 0
  else
    __STATUS__=$?
  fi

  [ "$PRIVILEGE_ESCALATION_CHECKED" = "1" ] || detect_privilege_escalation || true

  if [ "$(id -u)" = "0" ] || [ ${#SUDO[@]} -eq 0 ]; then
    return "$__STATUS__"
  fi

  /bin/echo
  yellow "The command failed as the current user:"
  yellow "  $__REASON__"
  if ! prompt_confirmation "Retry this command with sudo"; then
    return "$__STATUS__"
  fi

  "${SUDO[@]}" "$@"
}

download_to_stdout() {
  if command -v curl > /dev/null 2>&1; then
    curl -fsSL "$1"
  elif command -v wget > /dev/null 2>&1; then
    wget -qO- "$1"
  else
    return 1
  fi
}

docker_version() {
  docker --version 2>/dev/null | awk '{print $3}' | sed 's/,//'
}

compose_version() {
  docker compose version 2>/dev/null | awk '{print $4}' | sed 's/,//'
}

version_at_least() {
  [ -n "$2" ] || return 1
  [ "$(printf '%s\n' "$1" "$2" | sort -V | head -n1)" = "$1" ]
}

docker_ready() {
  command -v docker > /dev/null 2>&1 || return 1
  version_at_least "$REQUIRED_DOCKER_VERSION" "$(docker_version)" || return 1
  version_at_least "$REQUIRED_DOCKER_COMPOSE_VERSION" "$(compose_version)" || return 1
  return 0
}

install_docker() {
  /bin/echo "Installing Docker from https://get.docker.com ..."

  local __SCRIPT__
  __SCRIPT__=$(mktemp) || return 1

  if ! download_to_stdout "https://get.docker.com" > "$__SCRIPT__"; then
    rm -f "$__SCRIPT__"
    red "Could not download the Docker installation script."
    return 1
  fi

  if ! sh "$__SCRIPT__" < /dev/null; then
    rm -f "$__SCRIPT__"
    red "Docker's official installation script failed."
    return 1
  fi

  rm -f "$__SCRIPT__"
  return 0
}

install_compose_plugin() {
  /bin/echo "Installing the Docker Compose plugin..."

  local __ARCH__ __URL__ __DEST_DIR__ __TMP__
  case "$(uname -m)" in
    x86_64|amd64)   __ARCH__="x86_64" ;;
    aarch64|arm64)  __ARCH__="aarch64" ;;
    armv7l)         __ARCH__="armv7" ;;
    *)
      red "No Docker Compose plugin build for this architecture ($(uname -m))."
      return 1
      ;;
  esac

  __DEST_DIR__="/usr/local/lib/docker/cli-plugins"
  __URL__="https://github.com/docker/compose/releases/latest/download/docker-compose-linux-$__ARCH__"

  __TMP__=$(mktemp) || return 1
  if ! download_to_stdout "$__URL__" > "$__TMP__"; then
    rm -f "$__TMP__"
    red "Could not download the Docker Compose plugin."
    return 1
  fi

  run_with_optional_sudo "create $__DEST_DIR__" mkdir -p "$__DEST_DIR__" || { rm -f "$__TMP__"; return 1; }
  run_with_optional_sudo "install Docker Compose into $__DEST_DIR__" install -m 0755 "$__TMP__" "$__DEST_DIR__/docker-compose" || { rm -f "$__TMP__"; return 1; }
  rm -f "$__TMP__"

  return 0
}

ensure_docker_socket_access() {
  DOCKER=()

  if docker info > /dev/null 2>&1; then
    return 0
  fi

  if [ "$(id -u)" = "0" ]; then
    red "Docker daemon is not accessible."
    return 1
  fi

  if id -nG "$(id -un)" | tr ' ' '\n' | grep -qx docker; then
    red "The user belongs to the docker group, but the current session does not have access."
    yellow "Log out and log in again, then rerun:"
    yellow "  curl -sSL $INSTALLER_URL | bash"
    return 1
  fi

  /bin/echo
  yellow "Docker is installed, but $(id -un) is not in the docker group."

  if ! prompt_confirmation "Add $(id -un) to the docker group"; then
    [ "$PRIVILEGE_ESCALATION_CHECKED" = "1" ] || detect_privilege_escalation || true

    if [ ${#SUDO[@]} -eq 0 ]; then
      red "Cannot continue without Docker access: sudo is unavailable."
      return 1
    fi

    /bin/echo
    yellow "Continuing by invoking Docker through sudo."
    if ! "${SUDO[@]}" -v || ! "${SUDO[@]}" docker info > /dev/null 2>&1; then
      red "Docker is not accessible even through sudo."
      return 1
    fi

    DOCKER=("${SUDO[@]}")
    yellow "Docker commands will run through sudo during this installation."
    return 0
  fi

  [ "$PRIVILEGE_ESCALATION_CHECKED" = "1" ] || detect_privilege_escalation || true

  if [ ${#SUDO[@]} -eq 0 ]; then
    red "sudo is unavailable. Run this command as root:"
    red "  usermod -aG docker $(id -un)"
    return 1
  fi

  if ! "${SUDO[@]}" usermod -aG docker "$(id -un)"; then
    red "Could not add $(id -un) to the docker group."
    return 1
  fi

  green "$(id -un) was added to the docker group."
  yellow "The group change does not affect the current shell."
  yellow "Log out and log in again, then rerun:"
  yellow "  curl -sSL $INSTALLER_URL | bash"
  exit 0
}

check_dependencies() {
  detect_privilege_escalation || true

  if ! docker_ready; then
    /bin/echo
    if command -v docker > /dev/null 2>&1; then
      yellow "Docker $(docker_version) with Compose $(compose_version) does not meet the requirement"
      yellow "(Docker >= $REQUIRED_DOCKER_VERSION, Compose >= $REQUIRED_DOCKER_COMPOSE_VERSION)."
    else
      yellow "Docker is not installed."
    fi
    /bin/echo
    /bin/echo "This installer can set it up for you. It runs Docker's own installation script"
    /bin/echo "from https://get.docker.com, which adds the official Docker repository and"
    /bin/echo "installs the engine, the CLI and the Compose plugin."

    if ! prompt_confirmation "Install Docker and the Compose plugin now"; then
      red "Cannot proceed without Docker."
      exit 1
    fi

    if command -v docker > /dev/null 2>&1 && version_at_least "$REQUIRED_DOCKER_VERSION" "$(docker_version)"; then
      install_compose_plugin || handle_error
    else
      install_docker || handle_error
      if ! version_at_least "$REQUIRED_DOCKER_COMPOSE_VERSION" "$(compose_version)"; then
        install_compose_plugin || handle_error
      fi
    fi

    if ! docker_ready; then
      red "Docker is still not usable after installation:"
      red "  docker  $(docker_version) (need >= $REQUIRED_DOCKER_VERSION)"
      red "  compose $(compose_version) (need >= $REQUIRED_DOCKER_COMPOSE_VERSION)"
      exit 1
    fi
    green "Docker $(docker_version) with Compose $(compose_version) is ready."
  fi

  if ! ensure_docker_socket_access; then
    red "Cannot talk to the Docker daemon as the current user."
    red "Start Docker and make sure the current user belongs to the docker group."
    exit 1
  fi

  if [ "$(uname -s)" != "Linux" ]; then
    yellow "WARNING: this host is not Linux ($(uname -s))."
    yellow "         coturn needs host networking to relay media; it will not work here."
    yellow "         Everything else installs fine, so this is usable for a dry run only."
    if ! prompt_confirmation "Continue anyway"; then
      exit 1
    fi
  fi
}


cleanup() {
  if [ "$PRESERVE_DIRECTORY" = "1" ]; then
    yellow "Leaving ./$DIRECTORY and its containers in place so you can look at them:"
    yellow "  cd ./$DIRECTORY && docker compose ps && docker compose logs"
    yellow "Remove it yourself when you are done:  docker compose down -v"
    if [ -d "./$DIRECTORY/certbot/conf/live" ]; then
      yellow "It also holds the Let's Encrypt certificate and account key for $DOMAIN --"
      yellow "deleting those and re-issuing repeatedly runs into the limit of 5 identical"
      yellow "certificates per week."
    fi
    return 0
  fi

  if [ -f "./$DIRECTORY/docker-compose.yml" ]; then
    "${DOCKER[@]}" docker compose -f "./$DIRECTORY/docker-compose.yml" down -v --remove-orphans < /dev/null > /dev/null 2>&1 || true
  fi
  if [ "$DIRECTORY_CREATED" = "1" ] && [ -d "./$DIRECTORY" ]; then
    rm -rf "./${DIRECTORY:?}" && red "./$DIRECTORY Deleted."
  fi
}

handle_error() {
  local __STATUS__=$?
  trap - ERR SIGINT SIGQUIT SIGTSTP
  red "Something went wrong. Unable to proceed. Status: $__STATUS__" >&2
  red "Cleaning up..."
  cleanup
  red "Done."
  exit 1
}

trap 'handle_error' ERR SIGINT SIGQUIT


mask() {
  local __VALUE__="$1"
  if [ -z "$__VALUE__" ]; then
    /bin/echo ""
    return 0
  fi
  if [ ${#__VALUE__} -le 2 ]; then
    /bin/echo "**********"
    return 0
  fi
  /bin/echo "${__VALUE__:0:1}$(printf '*%.0s' {1..8})${__VALUE__:${#__VALUE__}-1:1}"
}

secure_input() {
  local __PASSWORD__=""
  local __CHAR_COUNT__=0
  local __PROMPT__=$1

  while IFS= read -p "$__PROMPT__" -r -s -n 1 -u 3 __CHAR__
  do
    if [[ $__CHAR__ == $'\0' ]]; then
      break
    fi
    if [[ $__CHAR__ == $'\177' ]]; then
      if [ $__CHAR_COUNT__ -gt 0 ]; then
        __CHAR_COUNT__=$((__CHAR_COUNT__-1))
        __PROMPT__=$'\b \b'
        __PASSWORD__="${__PASSWORD__%?}"
      else
        __PROMPT__=''
      fi
    else
      __CHAR_COUNT__=$((__CHAR_COUNT__+1))
      __PROMPT__='*'
      __PASSWORD__+="$__CHAR__"
    fi
  done

  printf '%s\n' "$__PASSWORD__"
}

ask() {
  local __PROMPT__="$1"
  local __TARGET__="$2"
  local __VALUE__=""

  if ! IFS= read -rp "$__PROMPT__" -u 3 __VALUE__; then
    /bin/echo
    red "Input stream closed -- this installer needs an interactive terminal."
    red "Run it from a terminal:"
    red "  curl -sSL $INSTALLER_URL | bash"
    exit 1
  fi

  printf -v "$__TARGET__" '%s' "$__VALUE__"
}

prompt_confirmation() {
  while true; do
    local __CONFIRMATION__
    ask "$1? [y/n]: " __CONFIRMATION__
    case $__CONFIRMATION__ in
      [yY]) return 0 ;;
      [nN]) return 1 ;;
      *) red "invalid input" ;;
    esac
  done
}

sed_escape() {
  printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'
}

render() {
  local __FILE__="$1"
  local __TMP__="$__FILE__.rendering"
  sed \
    -e "s|__DOMAIN__|$(sed_escape "$DOMAIN")|g" \
    -e "s|__PUBLIC_IP__|$(sed_escape "$PUBLIC_IP")|g" \
    -e "s|__EXTERNAL_IP__|$(sed_escape "$EXTERNAL_IP")|g" \
    -e "s|__TURN_USER__|$(sed_escape "$TURN_USER")|g" \
    -e "s|__TURN_CREDENTIAL__|$(sed_escape "$TURN_CREDENTIAL")|g" \
    -e "s|__TURN_PORT__|$(sed_escape "$TURN_PORT")|g" \
    -e "s|__TURN_MIN_PORT__|$(sed_escape "$TURN_MIN_PORT")|g" \
    -e "s|__TURN_MAX_PORT__|$(sed_escape "$TURN_MAX_PORT")|g" \
    -e "s|__PROJECT_NAME__|$(sed_escape "$PROJECT_NAME")|g" \
    "$__FILE__" > "$__TMP__"
  mv "$__TMP__" "$__FILE__"
}


base64url() {
  base64 | tr -d '\n' | tr '+/' '-_' | tr -d '='
}

hex_to_binary() {
  printf '%b' "$(printf '%s' "$1" | sed 's/../\\x&/g')"
}

generate_vapid_keys_with_openssl() {
  local __PEM__ __TEXT__ __PRIV_HEX__ __PUB_HEX__

  __PEM__=$(openssl ecparam -name prime256v1 -genkey -noout 2>/dev/null) || return 1
  __TEXT__=$(printf '%s\n' "$__PEM__" | openssl ec -text -noout 2>/dev/null) || return 1

  __PRIV_HEX__=$(printf '%s\n' "$__TEXT__" | awk '/^ *priv:/{f=1;next} /^ *pub:/{f=0} f' | tr -d ' :\n')
  __PUB_HEX__=$(printf '%s\n' "$__TEXT__" | awk '/^ *pub:/{f=1;next} /^ *ASN1 OID:/{f=0} f' | tr -d ' :\n')

  __PRIV_HEX__=$(printf '%064s' "$__PRIV_HEX__" | tr ' ' '0')
  __PRIV_HEX__=${__PRIV_HEX__: -64}

  [ ${#__PUB_HEX__} -eq 130 ] || return 1
  [ "${__PUB_HEX__:0:2}" = "04" ] || return 1

  VAPID_PUBLIC_KEY=$(hex_to_binary "$__PUB_HEX__" | base64url)
  VAPID_PRIVATE_KEY=$(hex_to_binary "$__PRIV_HEX__" | base64url)
  return 0
}

generate_vapid_keys_with_docker() {
  local __JSON__
  __JSON__=$("${DOCKER[@]}" docker run --rm node:lts-alpine sh -c 'npx --yes web-push generate-vapid-keys --json' < /dev/null 2>/dev/null) || return 1
  VAPID_PUBLIC_KEY=$(printf '%s' "$__JSON__"  | sed -n 's/.*"publicKey" *: *"\([^"]*\)".*/\1/p')
  VAPID_PRIVATE_KEY=$(printf '%s' "$__JSON__" | sed -n 's/.*"privateKey" *: *"\([^"]*\)".*/\1/p')
  return 0
}

assert_vapid_keys() {
  if [ ${#VAPID_PUBLIC_KEY} -ne 87 ] || [ "${VAPID_PUBLIC_KEY:0:1}" != "B" ]; then
    red "Generated VAPID public key looks wrong (expected 87 characters starting with 'B', got ${#VAPID_PUBLIC_KEY})."
    return 1
  fi
  if [ ${#VAPID_PRIVATE_KEY} -ne 43 ]; then
    red "Generated VAPID private key looks wrong (expected 43 characters, got ${#VAPID_PRIVATE_KEY})."
    return 1
  fi
  return 0
}

generate_vapid_keys() {
  /bin/echo "Generating VAPID key pair..."

  if command -v openssl > /dev/null 2>&1 && generate_vapid_keys_with_openssl; then
    :
  else
    yellow "openssl unavailable or unusable, falling back to a throwaway node container..."
    generate_vapid_keys_with_docker || handle_error
  fi

  assert_vapid_keys || handle_error
  green "Done."
}

generate_random_token() {
  LC_ALL=C head -c 4096 /dev/urandom | LC_ALL=C tr -dc 'A-Za-z0-9' | cut -c1-40
}


resolve_client_release() {
  CLIENT_TAG=""
  CLIENT_ASSET_URL=""

  local __JSON__

  if [ "$CLIENT_CHANNEL" = "stable" ]; then
    __JSON__=$(download_to_stdout "https://api.github.com/repos/$CLIENT_REPO/releases/latest" 2>/dev/null) || return 1
    CLIENT_TAG=$(printf '%s' "$__JSON__" | sed -n 's/.*"tag_name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n1)
  else
    __JSON__=$(download_to_stdout "https://api.github.com/repos/$CLIENT_REPO/releases?per_page=100") || return 1
    CLIENT_TAG=$(printf '%s' "$__JSON__" \
      | grep -o '"tag_name"[[:space:]]*:[[:space:]]*"[^"]*"\|"draft"[[:space:]]*:[[:space:]]*[a-z]*\|"prerelease"[[:space:]]*:[[:space:]]*[a-z]*' \
      | awk '
          /"tag_name"/   { t=$0; sub(/^.*:[[:space:]]*"/,"",t); sub(/"$/,"",t); tag=t; next }
          /"draft"/      { d=($0 ~ /true/); next }
          /"prerelease"/ { if (tag != "" && !d && $0 ~ /true/) { print tag; exit } }
        ')
  fi

  [ -n "$CLIENT_TAG" ] || return 1

  CLIENT_ASSET_URL=$(download_to_stdout "https://api.github.com/repos/$CLIENT_REPO/releases/tags/$CLIENT_TAG" \
    | grep -o "\"browser_download_url\"[[:space:]]*:[[:space:]]*\"[^\"]*${CLIENT_ASSET_PATTERN}[^\"]*\.zip\"" \
    | sed 's/.*"\(https[^"]*\)"$/\1/' | head -n1)

  [ -n "$CLIENT_ASSET_URL" ] || return 1
  return 0
}

install_client() {
  [ "$INSTALL_CLIENT" = "1" ] || return 0

  /bin/echo
  /bin/echo "Downloading the web client $CLIENT_TAG..."

  local __ZIP__
  __ZIP__=$(mktemp) || return 1

  if ! download_to_stdout "$CLIENT_ASSET_URL" > "$__ZIP__"; then
    rm -f "$__ZIP__"
    red "Could not download $CLIENT_ASSET_URL"
    return 1
  fi

  if command -v unzip > /dev/null 2>&1; then
    unzip -q -o "$__ZIP__" -d "./$DIRECTORY/www" || { rm -f "$__ZIP__"; return 1; }
  else
    "${DOCKER[@]}" docker run --rm \
      --user "$(id -u):$(id -g)" \
      -v "$__ZIP__:/tmp/client.zip:ro" \
      -v "$(cd "./$DIRECTORY/www" && pwd):/out" \
      alpine:latest unzip -q -o /tmp/client.zip -d /out < /dev/null || { rm -f "$__ZIP__"; return 1; }
  fi
  rm -f "$__ZIP__"

  if [ ! -f "./$DIRECTORY/www/index.html" ]; then
    red "The downloaded archive has no index.html at its top level."
    return 1
  fi

  chmod -R a+rX "./$DIRECTORY/www"

  green "Done."
}

generate_turn_credentials() {
  /bin/echo "Generating TURN credentials..."
  TURN_USER=$(generate_random_token)
  TURN_CREDENTIAL=$(generate_random_token)
  if [ ${#TURN_USER} -ne 40 ] || [ ${#TURN_CREDENTIAL} -ne 40 ]; then
    red "Unable to generate TURN credentials."
    return 1
  fi
  green "Done."
}


detect_public_ip() {
  local __IP__ __ENDPOINT__
  for __ENDPOINT__ in "https://api.ipify.org" "https://ifconfig.me/ip" "https://icanhazip.com"; do
    if command -v curl > /dev/null 2>&1; then
      __IP__=$(curl -fsS --max-time 5 "$__ENDPOINT__" 2>/dev/null | tr -d '[:space:]') || __IP__=""
    elif command -v wget > /dev/null 2>&1; then
      __IP__=$(wget -qO- --timeout=5 "$__ENDPOINT__" 2>/dev/null | tr -d '[:space:]') || __IP__=""
    else
      __IP__=""
    fi
    if [[ $__IP__ =~ ^[0-9]{1,3}(\.[0-9]{1,3}){3}$ ]]; then
      /bin/echo "$__IP__"
      return 0
    fi
  done
  /bin/echo ""
}

resolve_external_ip() {
  EXTERNAL_IP="$PUBLIC_IP"
}


prompt_directory() {
  local __NAME_REGEX__='^[A-Za-z0-9._-]+$'
  local __INPUT__

  while true; do
    ask "Enter installation directory ('$DIRECTORY' by default): " __INPUT__
    if [ -n "$__INPUT__" ]; then
      DIRECTORY="$__INPUT__"
    fi

    if ! [[ $DIRECTORY =~ $__NAME_REGEX__ ]] || [ "$DIRECTORY" = "." ] || [ "$DIRECTORY" = ".." ]; then
      red "Use a simple directory name -- letters, digits, dot, dash and underscore only."
      DIRECTORY="ephemon-$NOW"
      continue
    fi

    if [ -e "./$DIRECTORY" ]; then
      red "./$DIRECTORY already exists. Pick another name."
      DIRECTORY="ephemon-$NOW"
      continue
    fi

    return 0
  done
}

prompt_domain() {
  local __HOSTNAME_REGEX__='^([a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$'

  DOMAIN=""
  ACME_EMAIL=""

  /bin/echo
  /bin/echo "A domain enables HTTPS (Let's Encrypt, auto-renewed). Without one the server is"
  /bin/echo "reachable over plain http:// on its IP address, which browsers treat as an insecure"
  /bin/echo "origin -- see the warning printed at the end."
  if ! prompt_confirmation "Do you have a domain pointing at this host"; then
    return 0
  fi

  while true; do
    ask "Enter domain (e.g. ephemon.example.com): " DOMAIN
    if [[ $DOMAIN =~ $__HOSTNAME_REGEX__ ]]; then
      break
    fi
    red "invalid input"
  done

  ask "Enter email for Let's Encrypt expiry notices (or press enter to skip): " ACME_EMAIL
}

prompt_client() {
  INSTALL_CLIENT=0
  CLIENT_CHANNEL=""
  CLIENT_TAG=""
  CLIENT_ASSET_URL=""

  /bin/echo
  /bin/echo "This host can serve the Ephemon web client itself, alongside the signalling server,"
  /bin/echo "so there is one address to hand out and the client is already pointed at it."
  /bin/echo "Say no to run a protocol-only server and use a client hosted elsewhere."
  if ! prompt_confirmation "Serve the web client from this host too"; then
    return 0
  fi

  INSTALL_CLIENT=1

  while true; do
    /bin/echo
    /bin/echo "  1) latest stable release  (default)"
    /bin/echo "  2) latest pre-release     (newer, less tested)"
    local __CHOICE__
    ask "Which build? [1/2]: " __CHOICE__

    case "${__CHOICE__:-1}" in
      1) CLIENT_CHANNEL="stable" ;;
      2) CLIENT_CHANNEL="prerelease" ;;
      *) red "invalid input"; continue ;;
    esac

    /bin/echo "Looking up the $CLIENT_CHANNEL release of $CLIENT_REPO..."
    if resolve_client_release; then
      green "Found $CLIENT_TAG."
      return 0
    fi

    if [ "$CLIENT_CHANNEL" = "stable" ]; then
      yellow "$CLIENT_REPO has no stable release yet -- only pre-releases."
      if prompt_confirmation "Use the latest pre-release instead"; then
        CLIENT_CHANNEL="prerelease"
        if resolve_client_release; then
          green "Found $CLIENT_TAG."
          return 0
        fi
      fi
    fi

    red "Could not resolve a downloadable build."
    if ! prompt_confirmation "Try again"; then
      INSTALL_CLIENT=0
      CLIENT_CHANNEL=""
      return 0
    fi
  done
}

prompt_public_ip() {
  local __DETECTED__
  /bin/echo
  /bin/echo "Detecting this host's public IP address..."
  __DETECTED__=$(detect_public_ip)

  while true; do
    if [ -n "$__DETECTED__" ]; then
      ask "Enter public IP address ('$__DETECTED__' by default): " PUBLIC_IP
      if [[ $PUBLIC_IP == "" ]]; then
        PUBLIC_IP="$__DETECTED__"
      fi
    else
      yellow "Could not detect it automatically."
      ask "Enter public IP address: " PUBLIC_IP
    fi
    if [[ $PUBLIC_IP =~ ^[0-9]{1,3}(\.[0-9]{1,3}){3}$ ]]; then
      return 0
    fi
    red "invalid input"
  done
}

prompt_vapid() {
  local __SUBJECT_REGEX__='^(mailto:[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}|https://[A-Za-z0-9./_-]+)$'
  local __DEFAULT_SUBJECT__

  /bin/echo
  /bin/echo "Web Push needs a VAPID key pair. Reuse an existing pair only if you are reinstalling"
  /bin/echo "and want the push subscriptions your users already granted to keep working."
  if prompt_confirmation "Reuse an existing VAPID key pair"; then
    while true; do
      ask "Enter VAPID public key: " VAPID_PUBLIC_KEY
      VAPID_PRIVATE_KEY=$(secure_input "Enter VAPID private key: ")
      /bin/echo
      if assert_vapid_keys; then
        break
      fi
    done
  else
    generate_vapid_keys
  fi

  if [ -n "$DOMAIN" ]; then
    __DEFAULT_SUBJECT__="mailto:admin@$DOMAIN"
  elif [ -n "$ACME_EMAIL" ]; then
    __DEFAULT_SUBJECT__="mailto:$ACME_EMAIL"
  else
    __DEFAULT_SUBJECT__="mailto:admin@example.com"
  fi

  /bin/echo
  /bin/echo "Push services (Apple, Google, Mozilla) require a contact address in every push"
  /bin/echo "request, so they can reach you if your server misbehaves."
  while true; do
    ask "Enter VAPID subject ('$__DEFAULT_SUBJECT__' by default): " VAPID_SUBJECT
    if [[ $VAPID_SUBJECT == "" ]]; then
      VAPID_SUBJECT="$__DEFAULT_SUBJECT__"
    fi
    if [[ $VAPID_SUBJECT =~ $__SUBJECT_REGEX__ ]]; then
      return 0
    fi
    red "invalid input -- use mailto:you@example.com or https://example.com"
  done
}

prompt_cors() {
  local __ORIGINS_REGEX__='^[A-Za-z0-9:/._,; -]*$'

  /bin/echo
  /bin/echo "CORS decides which web origins may talk to this server. Leaving it empty allows any"
  /bin/echo "origin, which is what you want if you or your users run the client from anywhere."
  /bin/echo "Restricting it means only the listed client deployments can connect."
  while true; do
    ask "Enter allowed origins, comma separated (or press enter to allow any): " CORS
    if [[ $CORS =~ $__ORIGINS_REGEX__ ]]; then
      return 0
    fi
    red "invalid input -- e.g. https://ephemon.example.com,https://localhost:8081"
  done
}

prompt_input() {
  prompt_directory
  prompt_domain
  prompt_client
  prompt_public_ip
  prompt_vapid
  prompt_cors
}

print_configuration() {
  /bin/echo "--------------------------------------------------------------------------------"
  /bin/echo "Your configuration:"
  /bin/echo "> Installation directory: $DIRECTORY"
  if [ -n "$DOMAIN" ]; then
    /bin/echo "> Domain: $DOMAIN (HTTPS via Let's Encrypt, auto-renewed)"
    /bin/echo "> Let's Encrypt email: ${ACME_EMAIL:-[none -- registering without an email]}"
  else
    /bin/echo "> Domain: [none -- plain HTTP on the IP address]"
  fi
  /bin/echo "> Public IP: $PUBLIC_IP"
  if [ "$INSTALL_CLIENT" = "1" ]; then
    /bin/echo "> Web client: $CLIENT_TAG ($CLIENT_CHANNEL)"
  else
    /bin/echo "> Web client: [not served from this host]"
  fi
  /bin/echo "> VAPID subject: $VAPID_SUBJECT"
  /bin/echo "> VAPID public key: $VAPID_PUBLIC_KEY"
  /bin/echo "> VAPID private key: $(mask "$VAPID_PRIVATE_KEY")"
  /bin/echo "> TURN username: $TURN_USER"
  /bin/echo "> TURN credential: $(mask "$TURN_CREDENTIAL")"
  /bin/echo "> CORS origins: ${CORS:-[any]}"
  /bin/echo "--------------------------------------------------------------------------------"
}

unset_variables() {
  unset INSTALL_CLIENT
  unset CLIENT_CHANNEL
  unset CLIENT_TAG
  unset CLIENT_ASSET_URL
  unset DOMAIN
  unset ACME_EMAIL
  unset PUBLIC_IP
  unset EXTERNAL_IP
  unset VAPID_SUBJECT
  unset VAPID_PUBLIC_KEY
  unset VAPID_PRIVATE_KEY
  unset CORS
}

prompt_input_double_checked() {
  while true; do
    unset_variables
    prompt_input
    print_configuration
    if prompt_confirmation "Is everything correct"; then
      break
    fi
  done
  resolve_external_ip
}


derive_project_name() {
  local __BASE__
  __BASE__=$(printf '%s' "$DIRECTORY" | tr '[:upper:]' '[:lower:]' | tr -c 'a-z0-9_-' '-')
  __BASE__=$(printf '%s' "$__BASE__" | sed -e 's/^[^a-z0-9]*//' -e 's/[^a-z0-9]*$//')
  [ -n "$__BASE__" ] || __BASE__="ephemon"

  case "$__BASE__" in
    *"$NOW") PROJECT_NAME="$__BASE__" ;;
    *)       PROJECT_NAME="$__BASE__-$NOW" ;;
  esac
}

create_directories() {
  mkdir "./$DIRECTORY"
  DIRECTORY_CREATED=1
  derive_project_name
  mkdir -p "./$DIRECTORY/nginx/conf"
  mkdir -p "./$DIRECTORY/coturn"
  mkdir -p "./$DIRECTORY/www"
  if [ -n "$DOMAIN" ]; then
    mkdir -p "./$DIRECTORY/certbot/conf"
    mkdir -p "./$DIRECTORY/certbot/www"
  fi
}

generate_server_environment_file() {
  /bin/echo "Generating server environment file..."

  local __FILE__="./$DIRECTORY/server.env"

  : > "$__FILE__"
  chmod 600 "$__FILE__"

  {
    printf "NOTIFICATION_SUBJECT='%s'\n"     "$VAPID_SUBJECT"
    printf "NOTIFICATION_PUBLIC_KEY='%s'\n"  "$VAPID_PUBLIC_KEY"
    printf "NOTIFICATION_PRIVATE_KEY='%s'\n" "$VAPID_PRIVATE_KEY"
    printf "CORS='%s'\n"                     "$CORS"
    printf "PATH_BASE=''\n"
  } >> "$__FILE__"

  green "Done."
}

generate_docker_compose() {
  /bin/echo "Generating docker-compose.yml..."

  local __FILE__="./$DIRECTORY/docker-compose.yml"

  cat > "$__FILE__" <<'EOF'
name: __PROJECT_NAME__

x-logging: &default-logging
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"

services:
  redis:
    image: redis:7-alpine
    restart: unless-stopped
    logging: *default-logging
    command: ["redis-server", "--appendonly", "yes"]
    volumes:
      - redis-data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5
    networks:
      - ephemon-lan

  server:
    image: ephemon/server:latest
    restart: unless-stopped
    logging: *default-logging
    depends_on:
      redis:
        condition: service_healthy
    env_file:
      - path: server.env
        required: true
    environment:
      PORT: "80"
      REDIS_CONNECTION_STRING: "redis:6379"
    expose:
      - "80"
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://127.0.0.1:80/health"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 20s
    networks:
      - ephemon-lan

  coturn:
    image: coturn/coturn:latest
    restart: unless-stopped
    logging: *default-logging
    network_mode: host
    volumes:
      - ./coturn/turnserver.conf:/etc/coturn/turnserver.conf:ro
    tmpfs:
      - /var/lib/coturn
    command: ["-c", "/etc/coturn/turnserver.conf"]

  nginx:
    image: nginx:alpine
    restart: unless-stopped
    logging: *default-logging
    depends_on:
      - server
    ports:
      - "80:80"
EOF

  if [ -n "$DOMAIN" ]; then
    cat >> "$__FILE__" <<'EOF'
      - "443:443"
EOF
  fi

  cat >> "$__FILE__" <<'EOF'
    volumes:
      - ./nginx/conf:/etc/nginx/conf.d:ro
      - ./www:/var/www/ephemon:ro
EOF

  if [ -n "$DOMAIN" ]; then
    cat >> "$__FILE__" <<'EOF'
      - ./certbot/conf:/etc/nginx/ssl:ro
      - ./certbot/www:/var/www/certbot:ro
    command: ["/bin/sh", "-c", "while :; do sleep 6h & wait $${!}; nginx -s reload; done & exec nginx -g 'daemon off;'"]
EOF
  fi

  cat >> "$__FILE__" <<'EOF'
    networks:
      - ephemon-lan
EOF

  if [ -n "$DOMAIN" ]; then
    cat >> "$__FILE__" <<'EOF'

  certbot:
    image: certbot/certbot:latest
    restart: unless-stopped
    logging: *default-logging
    volumes:
      - ./certbot/conf:/etc/letsencrypt:rw
      - ./certbot/www:/var/www/certbot:rw
    entrypoint: ["/bin/sh", "-c", "trap exit TERM; while :; do certbot renew --webroot --webroot-path /var/www/certbot --quiet; sleep 12h & wait $${!}; done"]
EOF
  fi

  cat >> "$__FILE__" <<'EOF'

volumes:
  redis-data:

networks:
  ephemon-lan:
    driver: bridge
EOF

  render "$__FILE__"

  green "Done."
}

generate_coturn_configuration() {
  /bin/echo "Generating coturn configuration..."

  cat > "./$DIRECTORY/coturn/turnserver.conf" <<'EOF'

listening-port=__TURN_PORT__
listening-ip=0.0.0.0

external-ip=__EXTERNAL_IP__

min-port=__TURN_MIN_PORT__
max-port=__TURN_MAX_PORT__

realm=ephemon
server-name=ephemon

fingerprint
lt-cred-mech
user=__TURN_USER__:__TURN_CREDENTIAL__

no-tls
no-dtls

no-tcp-relay


no-multicast-peers

pidfile=/var/tmp/turnserver.pid

denied-peer-ip=0.0.0.0-0.255.255.255
denied-peer-ip=10.0.0.0-10.255.255.255
denied-peer-ip=100.64.0.0-100.127.255.255
denied-peer-ip=127.0.0.0-127.255.255.255
denied-peer-ip=169.254.0.0-169.254.255.255
denied-peer-ip=172.16.0.0-172.31.255.255
denied-peer-ip=192.0.0.0-192.0.0.255
denied-peer-ip=192.168.0.0-192.168.255.255
denied-peer-ip=198.18.0.0-198.19.255.255
denied-peer-ip=224.0.0.0-255.255.255.255
denied-peer-ip=__PUBLIC_IP__
denied-peer-ip=::1
denied-peer-ip=fc00::-fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff
denied-peer-ip=fe80::-febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff

total-quota=100

log-file=stdout
simple-log
EOF

  render "./$DIRECTORY/coturn/turnserver.conf"

  green "Done."
}


append_proxy_defaults() {
  cat >> "$1" <<'EOF'

    proxy_http_version 1.1;
    proxy_set_header Host              $host;
    proxy_set_header X-Real-IP         $remote_addr;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header Upgrade           $http_upgrade;
    proxy_set_header Connection        $connection_upgrade;

    proxy_buffering off;
    proxy_read_timeout 100s;
    proxy_send_timeout 100s;
EOF
}

append_common_locations() {
  cat >> "$1" <<'EOF'

    location = /config.json {
        default_type application/json;
        charset utf-8;
        add_header Cache-Control "no-store" always;
        add_header Access-Control-Allow-Origin "*" always;
    }

    location = /health {
        proxy_pass http://ephemon_backend;
    }

    location /api/v1/ {
        proxy_pass http://ephemon_backend;
    }

    location /signal/v1 {
        proxy_pass http://ephemon_backend;
    }

    location = /metrics {
        return 404;
    }
EOF
}

append_root_location() {
  if [ "$INSTALL_CLIENT" = "1" ]; then
    cat >> "$1" <<'EOF'

    location / {
        try_files $uri $uri/ /index.html;
        add_header Cache-Control "no-cache" always;
    }
EOF
  else
    cat >> "$1" <<'EOF'

    location = / {
        default_type text/plain;
        return 200 "Ephemon signalling server. Client configuration: /config.json";
    }

    location / {
        return 404;
    }
EOF
  fi
}

generate_nginx_configuration() {
  /bin/echo "Generating nginx configuration..."

  local __FILE__="./$DIRECTORY/nginx/conf/ephemon.conf"

  cat > "$__FILE__" <<'EOF'

map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}

upstream ephemon_backend {
    server server:80;
}

server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;
    server_tokens off;
    root /var/www/ephemon;
EOF

  append_proxy_defaults "$__FILE__"

  if [ -n "$DOMAIN" ]; then
    cat >> "$__FILE__" <<'EOF'

    location ^~ /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }
EOF
  fi

  append_common_locations "$__FILE__"
  append_root_location "$__FILE__"

  cat >> "$__FILE__" <<'EOF'
}
EOF

  green "Done."
}

update_nginx_configuration() {
  /bin/echo "Updating nginx configuration for $DOMAIN..."

  local __FILE__="./$DIRECTORY/nginx/conf/ephemon.conf"

  cat > "$__FILE__" <<'EOF'

map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}

upstream ephemon_backend {
    server server:80;
}

server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name __DOMAIN__;
    server_tokens off;

    location ^~ /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    location / {
        return 301 https://__DOMAIN__$request_uri;
    }
}

server {
    listen 443 ssl default_server;
    listen [::]:443 ssl default_server;
    http2 on;
    server_name __DOMAIN__;
    server_tokens off;
    root /var/www/ephemon;

    ssl_certificate     /etc/nginx/ssl/live/__DOMAIN__/fullchain.pem;
    ssl_certificate_key /etc/nginx/ssl/live/__DOMAIN__/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 1d;

    add_header Strict-Transport-Security "max-age=31536000" always;
EOF

  append_proxy_defaults "$__FILE__"
  append_common_locations "$__FILE__"
  append_root_location "$__FILE__"

  cat >> "$__FILE__" <<'EOF'
}
EOF

  render "$__FILE__"

  green "Done."
}

generate_client_configuration() {
  /bin/echo "Generating client configuration..."

  local __ICE_HOST__

  if [ -n "$DOMAIN" ]; then
    SERVER_URL="https://$DOMAIN"
    __ICE_HOST__="$DOMAIN"
  else
    SERVER_URL="http://$PUBLIC_IP"
    __ICE_HOST__="$PUBLIC_IP"
  fi

  cat > "./$DIRECTORY/www/config.json" <<EOF
{
  "serverUrl": "$SERVER_URL",
  "vapidKey": "$VAPID_PUBLIC_KEY",
  "iceServers": [
    {
      "urls": "stun:$__ICE_HOST__:$TURN_PORT"
    },
    {
      "urls": "turn:$__ICE_HOST__:$TURN_PORT",
      "username": "$TURN_USER",
      "credential": "$TURN_CREDENTIAL"
    },
    {
      "urls": "turn:$__ICE_HOST__:$TURN_PORT?transport=tcp",
      "username": "$TURN_USER",
      "credential": "$TURN_CREDENTIAL"
    }
  ]
}
EOF

  green "Done."
}

generate_readme() {
  local __FILE__="./$DIRECTORY/README.md"

  cat > "$__FILE__" <<EOF
# Ephemon -- self-hosted instance

Generated by \`deployment/install.sh\` on $(date -u '+%Y-%m-%d %H:%M:%S UTC').

- Signalling server: $SERVER_URL
- Client configuration: $SERVER_URL/config.json (also at \`www/config.json\`)
- TURN/STUN: $([ -n "$DOMAIN" ] && echo "$DOMAIN" || echo "$PUBLIC_IP"):$TURN_PORT
EOF

  if [ -n "$DOMAIN" ]; then
    cat >> "$__FILE__" <<'EOF'

## TLS

The `certbot` container wakes twice a day and renews the certificate once it is within
30 days of expiry; nginx reloads itself every 6 hours to pick up a new one. Nothing to
schedule, but **port 80 has to stay open** -- renewal revalidates over HTTP, not just at
install time, so closing it once HTTPS works is how certificates quietly expire.

```bash
docker compose logs certbot
docker compose run --rm --entrypoint certbot certbot certificates
```
EOF
  fi

  cat >> "$__FILE__" <<'EOF'

## Everyday operations

```bash
docker compose ps
docker compose logs -f server
docker compose logs -f coturn
docker compose restart nginx
docker compose pull && docker compose up -d
docker compose down
```

## Files

| Path | What it is |
| --- | --- |
| `docker-compose.yml` | the stack |
| `server.env` | VAPID keys and CORS -- **secret** |
| `nginx/conf/ephemon.conf` | routing and TLS |
| `coturn/turnserver.conf` | TURN credentials and relay range |
| `www/` | the web client, plus `config.json` -- served as-is by nginx |
EOF

  if [ -n "$DOMAIN" ]; then
    cat >> "$__FILE__" <<'EOF'
| `certbot/conf` | Let's Encrypt certificates and renewal state |
EOF
  fi

  cat >> "$__FILE__" <<'EOF'

## Changing the client configuration

Edit `www/config.json`. nginx serves it straight from disk with `Cache-Control: no-store`,
so the new version is live immediately -- no restart needed.

A client served from this host reads that document on first run and configures itself from
it, which is why it needs no setup screen. Devices that were already configured keep their
own settings, so changing this file only affects newcomers; existing ones are repointed from
the client's own settings screen.

## Upgrading the web client

Download a newer release from https://github.com/ephemonapp/client/releases and
unpack it over `www/`, keeping `config.json`:

```bash
curl -sSLO <asset-url>
unzip -o ephemon-client-oss-*.zip -d www/
```

## Rotating the VAPID keys

Don't, unless you have to: every push subscription your users granted is bound to the
public key, and they will all have to re-subscribe. If you must, change both keys in
`server.env`, mirror the public one into `www/config.json`, and `docker compose up -d server`.
EOF
}


compose() {
  "${DOCKER[@]}" docker compose -f "./$DIRECTORY/docker-compose.yml" "$@" < /dev/null
}

start_stack() {
  /bin/echo
  /bin/echo "Pulling images..."
  compose pull || handle_error
  /bin/echo "Starting the stack..."
  compose up -d --remove-orphans || handle_error
  green "Done."
}

wait_for_server() {
  /bin/echo "Waiting for the signalling server to report healthy..."
  local __ATTEMPT__=0
  while [ $__ATTEMPT__ -lt 60 ]; do
    if compose ps --format '{{.Service}} {{.Health}}' 2>/dev/null | grep -q '^server healthy$'; then
      green "Done."
      return 0
    fi
    __ATTEMPT__=$((__ATTEMPT__+1))
    sleep 2
  done
  red "The signalling server did not become healthy in time. Recent logs:"
  compose logs --tail 40 server || true
  /bin/echo
  yellow "The stack is up and will keep restarting the server by itself."
  if prompt_confirmation "Continue with the installation anyway"; then
    return 0
  fi
  PRESERVE_DIRECTORY=1
  return 1
}

verify_coturn() {
  /bin/echo "Checking the TURN server..."

  sleep 3

  if compose logs --tail 50 coturn 2>/dev/null | grep -q "Cannot bind local socket"; then
    red "coturn cannot bind port $TURN_PORT -- something else on this host already has it."
    red "A distro coturn/turnserver.service and a second Ephemon install are the usual causes:"
    red "  systemctl status coturn"
    red "  ss -lnup | grep $TURN_PORT"
    /bin/echo
    yellow "Everything else works; only relayed calls (peers behind symmetric NAT) would fail."
    if prompt_confirmation "Continue anyway"; then
      return 0
    fi
    PRESERVE_DIRECTORY=1
    return 1
  fi

  green "Done."
  return 0
}

preflight_domain() {
  local __RESOLVED__=""

  /bin/echo "Checking that $DOMAIN resolves to this host..."

  if command -v getent > /dev/null 2>&1; then
    __RESOLVED__=$(getent ahostsv4 "$DOMAIN" 2>/dev/null | awk '{print $1; exit}')
  fi
  if [ -z "$__RESOLVED__" ] && command -v dig > /dev/null 2>&1; then
    __RESOLVED__=$(dig +short A "$DOMAIN" 2>/dev/null | tail -n1)
  fi
  if [ -z "$__RESOLVED__" ] && command -v host > /dev/null 2>&1; then
    __RESOLVED__=$(host -t A "$DOMAIN" 2>/dev/null | awk '/has address/{print $4; exit}')
  fi

  if [ -z "$__RESOLVED__" ]; then
    yellow "Could not resolve $DOMAIN. If the DNS record is new it may not have propagated yet."
    prompt_confirmation "Try to issue a certificate anyway" || return 1
    return 0
  fi

  if [ "$__RESOLVED__" != "$PUBLIC_IP" ]; then
    yellow "$DOMAIN resolves to $__RESOLVED__, but this host's public IP is $PUBLIC_IP."
    yellow "Let's Encrypt will fail unless the record points here (a proxy such as Cloudflare"
    yellow "also has to be in DNS-only mode for the challenge to reach this server)."
    prompt_confirmation "Try to issue a certificate anyway" || return 1
    return 0
  fi

  green "Done."
  return 0
}

generate_certificates() {
  /bin/echo
  /bin/echo "Issuing a certificate for $DOMAIN..."

  local -a __ARGS__
  if [ -n "$ACME_EMAIL" ]; then
    __ARGS__=(--email "$ACME_EMAIL")
  else
    __ARGS__=(--register-unsafely-without-email)
  fi

  if ! compose run --rm --entrypoint certbot certbot certonly --webroot --webroot-path /var/www/certbot \
       --agree-tos --no-eff-email "${__ARGS__[@]}" --dry-run -d "$DOMAIN"; then
    return 1
  fi

  if ! compose run --rm --entrypoint certbot certbot certonly --webroot --webroot-path /var/www/certbot \
       --agree-tos --no-eff-email "${__ARGS__[@]}" -d "$DOMAIN"; then
    return 1
  fi

  green "Done."
  return 0
}

fall_back_to_http() {
  yellow "Falling back to plain HTTP on $PUBLIC_IP."
  DOMAIN=""
  generate_docker_compose
  generate_nginx_configuration
  generate_client_configuration
  compose up -d --remove-orphans || handle_error
}

setup_tls() {
  [ -n "$DOMAIN" ] || return 0

  if ! preflight_domain; then
    red "Certificate issuance cancelled."
    fall_back_to_http
    return 0
  fi

  if ! generate_certificates; then
    red "Certbot could not issue a certificate for $DOMAIN."
    red "The usual causes are a DNS record that does not point here, port 80 blocked by a"
    red "firewall, or a Let's Encrypt rate limit."
    if prompt_confirmation "Continue with plain HTTP instead"; then
      fall_back_to_http
      return 0
    fi
    return 1
  fi

  PRESERVE_DIRECTORY=1

  update_nginx_configuration
  compose up -d --remove-orphans || handle_error
  compose restart nginx || handle_error
  verify_nginx
}

verify_nginx() {
  /bin/echo "Verifying the web server..."

  local __ATTEMPT__=0
  while [ $__ATTEMPT__ -lt 15 ]; do
    if compose ps --format '{{.Service}} {{.State}}' 2>/dev/null | grep -q '^nginx running$'; then
      green "Done."
      return 0
    fi
    __ATTEMPT__=$((__ATTEMPT__+1))
    sleep 2
  done

  red "nginx is not running. Recent logs:"
  compose logs --tail 30 nginx || true
  /bin/echo
  if [ -d "./$DIRECTORY/certbot/conf/live" ]; then
    yellow "The certificate was issued and is safe in ./$DIRECTORY/certbot."
  fi
  if prompt_confirmation "Continue anyway"; then
    return 0
  fi
  PRESERVE_DIRECTORY=1
  return 1
}


firewall_ports() {
  printf '  %-24s %s\n' "80/tcp" "HTTP (client config, signalling, ACME challenge)"
  if [ -n "$DOMAIN" ]; then
    printf '  %-24s %s\n' "443/tcp" "HTTPS"
  fi
  printf '  %-24s %s\n' "$TURN_PORT/tcp, $TURN_PORT/udp" "STUN/TURN"
  printf '  %-24s %s\n' "$TURN_MIN_PORT-$TURN_MAX_PORT/udp" "TURN relay range"
}

configure_firewall() {
  command -v ufw > /dev/null 2>&1 || [ -x /usr/sbin/ufw ] || return 0

  local -a __UFW__
  __UFW=(ufw)

  if ! ufw status > /dev/null 2>&1; then
    [ "$PRIVILEGE_ESCALATION_CHECKED" = "1" ] || detect_privilege_escalation || true
    if [ ${#SUDO[@]} -eq 0 ]; then
      yellow "Could not read ufw status as the current user. Open these ports manually if ufw is active:"
      firewall_ports
      return 0
    fi

    yellow "Could not read ufw status as the current user."
    if ! prompt_confirmation "Retry ufw status with sudo"; then
      yellow "Skipping ufw configuration. Open these ports manually if ufw is active:"
      firewall_ports
      return 0
    fi
    __UFW=("${SUDO[@]}" ufw)
  fi

  "${__UFW[@]}" status 2>/dev/null | grep -q "Status: active" || return 0

  /bin/echo
  yellow "ufw is active on this host. Ephemon needs these ports:"
  firewall_ports

  if ! prompt_confirmation "Open them in ufw now"; then
    return 0
  fi

  "${__UFW[@]}" allow 80/tcp > /dev/null
  if [ -n "$DOMAIN" ]; then
    "${__UFW[@]}" allow 443/tcp > /dev/null
  fi
  "${__UFW[@]}" allow "$TURN_PORT/tcp" > /dev/null
  "${__UFW[@]}" allow "$TURN_PORT/udp" > /dev/null
  "${__UFW[@]}" allow "$TURN_MIN_PORT:$TURN_MAX_PORT/udp" > /dev/null
  green "Done."
}


print_result() {
  /bin/echo
  /bin/echo "================================================================================"
  green "Ephemon is running."
  /bin/echo "================================================================================"
  /bin/echo
  bold "Client configuration"
  /bin/echo
  cat "./$DIRECTORY/www/config.json"
  /bin/echo
  /bin/echo "Saved to:  ./$DIRECTORY/www/config.json"
  /bin/echo "Served at: $SERVER_URL/config.json"
  /bin/echo
  if [ "$INSTALL_CLIENT" = "1" ]; then
    bold "Open the messenger"
    /bin/echo
    /bin/echo "  $SERVER_URL"
    /bin/echo
    /bin/echo "There is nothing to configure by hand. The client ships without a server baked in"
    /bin/echo "and reads the document above from /config.json on first run, so it arrives already"
    /bin/echo "pointed at this host. A device someone had already pointed elsewhere keeps its own"
    /bin/echo "settings; it can be repointed from the settings screen."
    /bin/echo
    /bin/echo "Web client version: $CLIENT_TAG ($CLIENT_CHANNEL)"
  else
    /bin/echo "Load it into an Ephemon client built without a preset (the OSS build): open the"
    /bin/echo "setup screen and either drop the file in, or paste the JSON above. To fetch it"
    /bin/echo "from another machine:"
    /bin/echo
    /bin/echo "  curl -sSL $SERVER_URL/config.json -o ephemon-config.json"
  fi
  /bin/echo
  /bin/echo "--------------------------------------------------------------------------------"
  /bin/echo "Ports that must be reachable from the internet:"
  firewall_ports
  /bin/echo
  if [ -n "$DOMAIN" ]; then
    yellow "Leave port 80 open permanently. Renewal revalidates over HTTP every 60 days, so"
    yellow "closing it once HTTPS works is the classic way to have a certificate expire."
    /bin/echo
  fi
  /bin/echo "Manage the stack from ./$DIRECTORY:"
  /bin/echo
  /bin/echo "  docker compose ps"
  /bin/echo "  docker compose logs -f server"
  /bin/echo "  docker compose down"
  /bin/echo
  /bin/echo "--------------------------------------------------------------------------------"

  if [ -z "$DOMAIN" ]; then
    /bin/echo
    yellow "One caveat about running without a domain:"
    /bin/echo
    /bin/echo "This instance answers over plain http://, and browsers only grant service workers"
    /bin/echo "and the Push API to secure origins. In practice:"
    /bin/echo
    /bin/echo "  works  -- a client served from http://localhost (localhost counts as secure),"
    /bin/echo "            messaging, and peer-to-peer connections including TURN relaying"
    /bin/echo "  broken -- push notifications from any client not on localhost, and any client"
    /bin/echo "            served over https:// at all, since calling http:// from an https://"
    /bin/echo "            page is blocked as mixed content"
    /bin/echo
    /bin/echo "That makes this mode right for a local trial or a LAN, and a domain the answer"
    /bin/echo "for anything else. Re-run this script with one when you are ready."
    /bin/echo
  fi
}


main() {
  bold "Ephemon installer"
  /bin/echo

  check_dependencies
  generate_turn_credentials
  prompt_input_double_checked

  create_directories
  generate_server_environment_file
  generate_docker_compose
  generate_coturn_configuration
  generate_nginx_configuration
  install_client
  generate_client_configuration

  configure_firewall

  start_stack
  wait_for_server
  verify_nginx
  verify_coturn
  setup_tls

  generate_client_configuration
  generate_readme

  print_result
}

main
