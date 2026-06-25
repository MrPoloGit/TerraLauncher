using System.Text.Json.Serialization;

namespace TerraLauncher.Instances;

public class VersionEntry {
	[JsonPropertyName("name")]        public string   Name        { get; set; } = "";
	[JsonPropertyName("version")]     public string   Version     { get; set; } = "";
	[JsonPropertyName("description")] public string   Description { get; set; } = "";
	[JsonPropertyName("date")]        public string   Date        { get; set; } = "";
	[JsonPropertyName("author")]      public string   Author      { get; set; } = "";
	[JsonPropertyName("platforms")]   public string[] Platforms   { get; set; } = [];
	[JsonPropertyName("url")]         public string?  Url         { get; set; }

	[JsonPropertyName("depotIds")]    public DepotIds?    DepotIds    { get; set; }
	[JsonPropertyName("manifestIds")] public ManifestIds? ManifestIds { get; set; }

	[JsonPropertyName("requiresTerrariaVersion")]
	public string? RequiresTerrariaVersion { get; set; }
}

public class DepotIds {
	[JsonPropertyName("windows")] public long Windows { get; set; }
	[JsonPropertyName("mac")]     public long Mac     { get; set; }
	[JsonPropertyName("linux")]   public long Linux   { get; set; }
}

public class ManifestIds {
	[JsonPropertyName("windows")] public string? Windows { get; set; }
	[JsonPropertyName("mac")]     public string? Mac     { get; set; }
	[JsonPropertyName("linux")]   public string? Linux   { get; set; }
}
