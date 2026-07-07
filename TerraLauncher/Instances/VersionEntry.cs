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

		// Steam depot/manifest for Terraria only.
		[JsonPropertyName("depotId")]    public long   DepotId    { get; set; }
		[JsonPropertyName("manifestId")] public string ManifestId { get; set; } = "";

		[JsonPropertyName("requiresTerrariaVersion")]
		public string RequiresTerrariaVersion { get; set; } = "";
	}
}
