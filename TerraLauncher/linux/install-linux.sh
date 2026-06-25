#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
INSTALL_DIR="$HOME/.local/share/TerraLauncher"
BIN_DIR="$HOME/.local/bin"
ICON_DIR="$HOME/.local/share/icons/hicolor/256x256/apps"
DESKTOP_DIR="$HOME/.local/share/applications"

echo "Installing TerraLauncher to $INSTALL_DIR ..."
mkdir -p "$INSTALL_DIR" "$BIN_DIR" "$ICON_DIR" "$DESKTOP_DIR"

cp -r "$SCRIPT_DIR"/. "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/TerraLauncher"

ln -sf "$INSTALL_DIR/TerraLauncher" "$BIN_DIR/TerraLauncher"

cp "$SCRIPT_DIR/TerraLauncher.png" "$ICON_DIR/TerraLauncher.png"

sed "s|Exec=TerraLauncher|Exec=$INSTALL_DIR/TerraLauncher|g" \
    "$SCRIPT_DIR/TerraLauncher.desktop" > "$DESKTOP_DIR/TerraLauncher.desktop"

update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
gtk-update-icon-cache -f -t "$HOME/.local/share/icons/hicolor" 2>/dev/null || true

echo "Done. Run 'TerraLauncher' from a terminal or find it in your applications menu."
