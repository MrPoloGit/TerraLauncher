Drop category icons in this folder — they are picked up automatically
(anything under Resources/ is bundled as an Avalonia asset).

Expected filenames (PNG, ideally 64-128 px; pixel art is fine — the app
renders them with nearest-neighbor scaling):

  Terraria.png     - Terraria Versions tile + instance entries
  TModLoader.png   - tModLoader tile + instance entries
  TAPI.png         - tAPI tile + instance entries
  TConfig.png      - tConfig tile + instance entries
  StandAlone.png   - StandAlone tile + instance entries
  Custom.png       - Custom tile + custom instance entries

Any file that is missing simply falls back to the current built-in icon.
