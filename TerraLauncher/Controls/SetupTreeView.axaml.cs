using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TerraLauncher.Setups;
using TerraLauncher.Windows;

namespace TerraLauncher.Controls;

public partial class SetupTreeView : UserControl {
	static Bitmap? iconGame, iconGameTMod, iconServer, iconTool, iconFolderOpen, iconFolderClosed;
	static Bitmap? iconAddGame, iconAddServer, iconAddTool, iconAddFolder;
	static Bitmap? iconRemoveGame, iconRemoveServer, iconRemoveTool, iconRemove2;

	private SetupTypes _setupType;
	public bool Modified { get; private set; }

	public SetupTreeView() {
		InitializeComponent();
		if (iconGame == null) {
			string uri = "avares://TerraLauncher/Resources/Icons/";
			static Bitmap Load(string u) => new(AssetLoader.Open(new Uri(u)));
			iconGame = Load(uri + "TreeView/TreeViewGame.png");
			iconGameTMod = Load(uri + "TreeView/TreeViewGameTMod.png");
			iconServer = Load(uri + "TreeView/TreeViewServer.png");
			iconTool = Load(uri + "TreeView/TreeViewTool.png");
			iconFolderOpen = Load(uri + "TreeView/TreeViewFolderOpen.png");
			iconFolderClosed = Load(uri + "TreeView/TreeViewFolderClosed.png");
			iconAddGame = Load(uri + "GameAdd.png");
			iconAddServer = Load(uri + "ServerAdd.png");
			iconAddTool = Load(uri + "ToolAdd.png");
			iconAddFolder = Load(uri + "FolderAdd.png");
			iconRemoveGame = Load(uri + "GameRemove.png");
			iconRemoveServer = Load(uri + "ServerRemove.png");
			iconRemoveTool = Load(uri + "ToolRemove.png");
			iconRemove2 = Load(uri + "Remove.png");
		}
	}

	public SetupFolder GenerateHierarchy() {
		var rootItem = (TreeViewItem)treeView.Items[0]!;
		var rootFolder = (SetupFolder)rootItem.Tag!;
		PopulateHierarchy(rootItem, rootFolder);
		return rootFolder;
	}

	private static void PopulateHierarchy(TreeViewItem parent, SetupFolder folder) {
		folder.Entries.Clear();
		foreach (var itemObj in parent.Items) {
			var item = (TreeViewItem)itemObj!;
			var setup = (ISetup)item.Tag!;
			folder.Entries.Add(setup);
			if (setup is SetupFolder sub) {
				sub.Parent = folder;
				PopulateHierarchy(item, sub);
			}
		}
	}

	public void Populate(SetupFolder folder, SetupTypes setupType) {
		folder = folder.CloneFolder();
		_setupType = setupType;
		treeView.Items.Clear();
		var root = MakeFolderItem(folder, true);
		treeView.Items.Add(root);
		PopulateTree(root, folder);
		imageAddSetup.Source = setupType switch {
			SetupTypes.Game => iconAddGame,
			SetupTypes.Server => iconAddServer,
			_ => iconAddTool
		};
		ToolTip.SetTip(buttonAddSetup, $"Add {setupType} Setup");
		UpdateButtons();
	}

	private void PopulateTree(TreeViewItem parent, SetupFolder folder) {
		foreach (var o in folder.Entries) {
			if (o is SetupFolder sub) {
				var item = MakeFolderItem(sub);
				parent.Items.Add(item);
				PopulateTree(item, sub);
			}
			else if (o is Setup s) {
				parent.Items.Add(MakeSetupItem(s));
			}
		}
	}

	private TreeViewItem MakeFolderItem(SetupFolder folder, bool root = false) {
		var item = new TreeViewItem { Tag = folder, IsExpanded = true };
		item.PropertyChanged += (_, e) => { if (e.Property == TreeViewItem.IsExpandedProperty) UpdateItem(item); };
		var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(2) };
		panel.Children.Add(new Image { Source = iconFolderOpen, Width = 16, Height = 16, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
		panel.Children.Add(new TextBlock { Text = folder.Name, Margin = new Thickness(5, 1, 2, 1), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
		item.Header = panel;
		return item;
	}

	private TreeViewItem MakeSetupItem(Setup setup) {
		var item = new TreeViewItem { Tag = setup };
		var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(2) };
		var icon = new Image { Width = 16, Height = 16, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
		icon.Source = _setupType switch {
			SetupTypes.Game => ((Game)setup).IsTMod ? iconGameTMod : iconGame,
			SetupTypes.Server => iconServer,
			_ => iconTool
		};
		panel.Children.Add(icon);
		panel.Children.Add(new TextBlock { Text = setup.Name, Margin = new Thickness(5, 1, 2, 1), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
		panel.Children.Add(new TextBlock {
			Text = !string.IsNullOrWhiteSpace(setup.Details) ? " (" + setup.Details + ")" : "",
			FontSize = 10, Margin = new Thickness(2, 1), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
		});
		item.DoubleTapped += (_, _) => OnEdit(null, null!);
		item.Header = panel;
		return item;
	}

	private void UpdateItem(TreeViewItem item) {
		if (item.Tag is SetupFolder folder) {
			var panel = (StackPanel)item.Header!;
			((Image)panel.Children[0]).Source = item.IsExpanded ? iconFolderOpen : iconFolderClosed;
			((TextBlock)panel.Children[1]).Text = folder.Name;
		}
		else if (item.Tag is Setup setup) {
			var panel = (StackPanel)item.Header!;
			if (_setupType == SetupTypes.Game)
				((Image)panel.Children[0]).Source = ((Game)setup).IsTMod ? iconGameTMod : iconGame;
			((TextBlock)panel.Children[1]).Text = setup.Name;
			((TextBlock)panel.Children[2]).Text = !string.IsNullOrWhiteSpace(setup.Details) ? " (" + setup.Details + ")" : "";
		}
	}

	private TreeViewItem? SelectedItem => treeView.SelectedItem as TreeViewItem;

	private void OnAddFolder(object? sender, RoutedEventArgs e) {
		var parent = GetInsertParentAndIndex(out int index);
		if (parent == null) return;
		var folder = new SetupFolder();
		var item = MakeFolderItem(folder);
		parent.Items.Insert(index, item);
		treeView.SelectedItem = item;
		UpdateButtons();
		Modified = true;
	}

	private void OnAddSetup(object? sender, RoutedEventArgs e) {
		var parent = GetInsertParentAndIndex(out int index);
		if (parent == null) return;
		Setup setup = _setupType switch {
			SetupTypes.Game => new Game(),
			SetupTypes.Server => new Server(),
			_ => new Tool()
		};
		var item = MakeSetupItem(setup);
		parent.Items.Insert(index, item);
		treeView.SelectedItem = item;
		UpdateButtons();
		Modified = true;
	}

	private TreeViewItem? GetInsertParentAndIndex(out int index) {
		index = 0;
		if (treeView.Items.Count == 0) return null;
		var sel = SelectedItem;
		var parent = sel != null ? (sel.Parent as TreeViewItem ?? treeView.Items[0] as TreeViewItem)! : (TreeViewItem)treeView.Items[0]!;
		if (sel?.Tag is Setup) {
			index = parent.Items.IndexOf(sel) + 1;
		}
		return parent;
	}

	private async void OnRemove(object? sender, RoutedEventArgs e) {
		var item = SelectedItem;
		if (item == null) return;
		var parent = item.Parent as TreeViewItem;
		if (parent == null) return;
		string type = item.Tag is Setup ? _setupType.ToString() : "Folder";
		var win = TopLevel.GetTopLevel(this) as Window;
		if (win == null) return;
		var result = await TriggerMessageBox.ShowAsync(win, MessageIcon.Question,
			$"Are you sure you want to remove this {type.ToLower()}?", "Remove " + type, MsgBoxButton.YesNo);
		if (result == MsgBoxResult.Yes) {
			parent.Items.Remove(item);
			UpdateButtons();
			Modified = true;
		}
	}

	private async void OnEdit(object? sender, RoutedEventArgs e) {
		var item = SelectedItem;
		if (item == null || item == treeView.Items[0]) return;
		var win = (TopLevel.GetTopLevel(this) as Window)!;
		bool changed = false;
		if (item.Tag is SetupFolder folder)
			changed = await EditFolderWindow.ShowDialogAsync(win, folder);
		else if (item.Tag is Game game)
			changed = await EditGameWindow.ShowDialogAsync(win, game);
		else if (item.Tag is Server server)
			changed = await EditServerWindow.ShowDialogAsync(win, server);
		else if (item.Tag is Tool tool)
			changed = await EditToolWindow.ShowDialogAsync(win, tool);
		if (changed) {
			UpdateItem(item);
			Modified = true;
		}
	}

	private void OnMoveUp(object? sender, RoutedEventArgs e) => MoveSelected(-1);
	private void OnMoveDown(object? sender, RoutedEventArgs e) => MoveSelected(1);

	private void MoveSelected(int delta) {
		var item = SelectedItem;
		if (item?.Parent is not TreeViewItem parent) return;
		int idx, newIdx;
		var newParent = FindMoveLocation(item, delta, out idx);
		if (newParent == null) return;
		parent.Items.Remove(item);
		newParent.Items.Insert(idx, item);
		treeView.SelectedItem = item;
		UpdateButtons();
		Modified = true;
	}

	private void OnTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateButtons();

	private void UpdateButtons() {
		var item = SelectedItem;
		var parent = item?.Parent as TreeViewItem;
		bool hasParent = item != null && parent != null;
		buttonRemove.IsEnabled = hasParent;
		buttonEdit.IsEnabled = hasParent;
		buttonMoveUp.IsEnabled = hasParent && FindMoveLocation(item!, -1, out _) != null;
		buttonMoveDown.IsEnabled = hasParent && FindMoveLocation(item!, 1, out _) != null;
		imageRemove.Source = item?.Tag is Setup ? _setupType switch {
			SetupTypes.Game => iconRemoveGame,
			SetupTypes.Server => iconRemoveServer,
			_ => iconRemoveTool
		} : iconRemove2;
	}

	private TreeViewItem? FindMoveLocation(TreeViewItem item, int distance, out int index) {
		var parent = item.Parent as TreeViewItem;
		index = -1;
		if (parent == null) return null;
		int oldIndex = parent.Items.IndexOf(item);
		while (distance != 0) {
			if ((distance < 0 && oldIndex == 0) || (distance > 0 && oldIndex == parent.Items.Count - 1)) {
				var gp = parent.Parent as TreeViewItem;
				if (gp == null) return null;
				oldIndex = gp.Items.IndexOf(parent) + (distance > 0 ? 1 : 0);
				index = oldIndex;
				distance -= Math.Sign(distance);
				parent = gp;
			}
			else {
				oldIndex += Math.Sign(distance);
				if (parent.Items[oldIndex] is TreeViewItem newItem && newItem.Tag is SetupFolder && newItem.IsExpanded) {
					oldIndex = distance < 0 ? newItem.Items.Count : 0;
					parent = newItem;
				}
				index = oldIndex;
				distance -= Math.Sign(distance);
			}
		}
		return index == -1 ? null : parent;
	}
}
