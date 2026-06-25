using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using TerraLauncher.Windows;

namespace TerraLauncher.Setups;

public class Tool : Setup {
	public override string Arguments { get; set; } = "";
	public string ProjectPath { get; set; } = "";
	public string ProjectDirectory => Path.GetDirectoryName(ProjectPath) ?? "";

	protected override string TypeName => "Tool";
	protected override string DefaultIcon => "Tool";

	public override SetupOption[] Options {
		get {
			var opts = new List<SetupOption> {
				new("Launch Tool", "Launch", Launch),
				new("Open Tool Folder", "Home", OpenExeFolder)
			};
			if (!string.IsNullOrWhiteSpace(ProjectPath)) {
				opts.Add(new("Open Project", "Hammer", OpenProject));
				opts.Add(new("Open Project Folder", "Folder", OpenProjectFolder));
			}
			opts.Add(new("Edit Tool Setup", "Gear", EditTool));
			opts.Add(new("Remove Entry", "ToolRemove", Delete));
			return opts.ToArray();
		}
	}

	public Tool() {
		Name = "New Tool";
		Icon = "Tool";
	}

	public override ISetup Clone() {
		var t = new Tool();
		CloneBase(t);
		t.Arguments = Arguments;
		t.ProjectPath = ProjectPath;
		return t;
	}

	protected override void ReadSetup(XmlElement setup) {
		var node = setup.SelectSingleNode("Arguments");
		if (node != null) Arguments = node.InnerText;
		node = setup.SelectSingleNode("ProjectPath");
		if (node != null) ProjectPath = node.InnerText;
	}

	protected override void WriteSetup(XmlElement setup, XmlDocument doc) {
		void AppendText(string name, string value) {
			var el = doc.CreateElement(name);
			el.AppendChild(doc.CreateTextNode(value));
			setup.AppendChild(el);
		}
		if (!string.IsNullOrWhiteSpace(Arguments)) AppendText("Arguments", Arguments);
		if (!string.IsNullOrWhiteSpace(ProjectPath)) AppendText("ProjectPath", ProjectPath);
	}

	public void OpenProject() {
		Sounds.PlayOpen();
		try {
			if (File.Exists(ProjectPath)) {
				var start = new ProcessStartInfo {
					FileName = ProjectPath,
					WindowStyle = ProcessWindowStyle.Normal,
					CreateNoWindow = true,
					UseShellExecute = true,
					WorkingDirectory = ExeDirectory
				};
				Process.Start(start);
			}
		}
		catch { }
	}

	public void OpenProjectFolder() {
		Sounds.PlayOpen();
		try {
			if (Directory.Exists(ProjectDirectory))
				OpenFolder(ProjectDirectory);
		}
		catch { }
	}

	public void EditTool() {
		if (Config.MainWindow == null) return;
		Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => {
			if (await EditToolWindow.ShowDialogAsync(Config.MainWindow, this)) {
				Entry?.Update();
				Config.Modified = true;
				Config.SaveConfig();
			}
		});
	}
}
