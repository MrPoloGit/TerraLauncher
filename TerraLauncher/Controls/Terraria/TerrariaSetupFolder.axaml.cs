using System;
using Avalonia.Media;
using TerraLauncher.Setups;

namespace TerraLauncher.Controls.Terraria;

public partial class TerrariaSetupFolder : Avalonia.Controls.UserControl {
	private readonly SetupFolder _folder;
	private readonly System.Action _navigate;

	public TerrariaSetupFolder(SetupFolder folder, bool isParent, System.Action navigate) {
		InitializeComponent();
		_folder = folder;
		_navigate = navigate;
		folder.Entry = this;

		if (isParent) {
			var opt = new SetupOption("Go Back", "FolderLeave", navigate) {
				Tooltip = "Go back to the parent folder"
			};
			stackPanelOptions.Children.Add(new TerrariaSetupOptionButton(opt));
			labelName.Text = "Go Back";
			labelEntries.Text = "Parent: " + folder.Name;
		}
		else {
			var enterOpt = new SetupOption("Open Folder", "FolderEnter", navigate) {
				Tooltip = "Open the subfolder"
			};
			stackPanelOptions.Children.Add(new TerrariaSetupOptionButton(enterOpt));

			var editOpt = new SetupOption("Edit Folder", "Gear", folder.EditFolder);
			stackPanelOptions.Children.Add(new TerrariaSetupOptionButton(editOpt));

			labelName.Text = folder.Name;
			labelEntries.Text = "Folder: " + folder.Entries.Count + " Entries";
		}

		var bitmap = folder.LoadIcon();
		if (bitmap != null) {
			imageIcon.Source = bitmap;
			imageIcon.Width = Math.Min(68, bitmap.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bitmap.PixelSize.Height);
		}
	}

	public override void Render(DrawingContext context) {
		CroppedFrames.EnsureInitialized();
		DrawCropped.DrawFrame(context, CroppedFrames.SetupFrame, Bounds.Width, Bounds.Height);
		base.Render(context);
	}

	public void Launch() {
		if (stackPanelOptions.Children.Count > 0)
			((TerrariaSetupOptionButton)stackPanelOptions.Children[0]).Action();
	}

	public void Update() {
		labelName.Text = _folder.Name;
		var bitmap = _folder.LoadIcon();
		if (bitmap != null) {
			imageIcon.Source = bitmap;
			imageIcon.Width = Math.Min(68, bitmap.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bitmap.PixelSize.Height);
		}
	}
}
