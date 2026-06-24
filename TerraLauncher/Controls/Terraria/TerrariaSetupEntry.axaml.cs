using System;
using Avalonia.Media;
using TerraLauncher.Setups;

namespace TerraLauncher.Controls.Terraria;

public partial class TerrariaSetupEntry : Avalonia.Controls.UserControl {
	private readonly Setup _setup;

	public TerrariaSetupEntry(Setup setup) {
		InitializeComponent();
		_setup = setup;
		setup.Entry = this;
		BuildOptions();
		UpdateData();
	}

	public override void Render(DrawingContext context) {
		CroppedFrames.EnsureInitialized();
		DrawCropped.DrawFrame(context, CroppedFrames.SetupFrame, Bounds.Width, Bounds.Height);
		base.Render(context);
	}

	public void Update() {
		stackPanelOptions.Children.Clear();
		BuildOptions();
		UpdateData();
	}

	public void Launch() {
		if (stackPanelOptions.Children.Count > 0)
			((TerrariaSetupOptionButton)stackPanelOptions.Children[0]).Action();
	}

	private void BuildOptions() {
		foreach (var option in _setup.Options) {
			stackPanelOptions.Children.Add(new TerrariaSetupOptionButton(option));
		}
	}

	private void UpdateData() {
		labelName.Text = _setup.Name;
		labelDetails.Text = _setup.Details;
		var bitmap = _setup.LoadIcon();
		if (bitmap != null) {
			imageIcon.Source = bitmap;
			imageIcon.Width = Math.Min(68, bitmap.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bitmap.PixelSize.Height);
		}
	}
}
