using System.Text.Json.Serialization;

namespace TerraLauncher.Instances {
	// One selectable download in the version-picker list.
	public class VersionEntry {
		[JsonPropertyName("name")]        public string Name        { get; set; } = "";
		[JsonPropertyName("version")]     public string Version     { get; set; } = "";
		[JsonPropertyName("description")] public string Description { get; set; } = "";
		[JsonPropertyName("date")]        public string Date        { get; set; } = "";
		[JsonPropertyName("author")]      public string Author      { get; set; } = "";

		// Direct HTTP(S) download URL — used by every category except Terraria,
		// which is fetched from Steam via DepotDownloader instead.
		[JsonPropertyName("url")] public string Url { get; set; } = "";

		// Optional second archive extracted on top of Url's contents (overwriting
		// matching files) after the base install finishes — used to represent a
		// version that's really "base build + a hotfix/replacement exe applied",
		// like Avalon 1.8.4 (base + hotfix) or 1.8.2 (1.8.1 base + exe-only patch).
		[JsonPropertyName("patchUrl")] public string PatchUrl { get; set; } = "";

		// Steam depot/manifest for Terraria only.
		[JsonPropertyName("depotId")]    public long   DepotId    { get; set; }
		[JsonPropertyName("manifestId")] public string ManifestId { get; set; } = "";

		[JsonPropertyName("requiresTerrariaVersion")]
		public string RequiresTerrariaVersion { get; set; } = "";

		// Which mod/game a Stand Alone entry belongs to (e.g. "Avalon", "N Terraria") —
		// Stand Alone bundles several unrelated standalone games under one category,
		// so the version picker filters on this instead of treating them as one list.
		[JsonPropertyName("type")] public string Type { get; set; } = "";
	}
}
