using System;
using System.Text.Json.Serialization;

namespace TerraLauncher.Instances;

public class InstanceRecord {
	[JsonPropertyName("id")]          public string   Id          { get; set; } = Guid.NewGuid().ToString();
	[JsonPropertyName("name")]        public string   Name        { get; set; } = "";
	[JsonPropertyName("version")]     public string   Version     { get; set; } = "";
	[JsonPropertyName("category")]    public InstanceCategory Category { get; set; }
	[JsonPropertyName("installPath")] public string   InstallPath { get; set; } = "";
	[JsonPropertyName("exePath")]     public string   ExePath     { get; set; } = "";

	[JsonPropertyName("linkedTerrariaInstanceId")]
	public string? LinkedTerrariaInstanceId { get; set; }
}
