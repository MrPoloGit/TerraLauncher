using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace TerraLauncher.Controls;

public class ValueChangedEventArgs<T> : EventArgs {
	public T Previous;
	public T New;
	public ValueChangedEventArgs(T previous, T newVal) { Previous = previous; New = newVal; }
}

public partial class IntSpinner : UserControl {
	private int _errorValue = 0;
	private int _number = 1;
	private int _maximum = int.MaxValue;
	private int _minimum = int.MinValue;
	private int _increment = 1;
	private bool _updating;

	public event EventHandler<ValueChangedEventArgs<int>>? ValueChanged;

	public int ErrorValue {
		get => _errorValue;
		set {
			if (value < _minimum || value > _maximum) throw new ArgumentOutOfRangeException(nameof(ErrorValue));
			_errorValue = value;
		}
	}

	public int Value {
		get => _number;
		set {
			if (value < _minimum || value > _maximum) throw new ArgumentOutOfRangeException(nameof(Value));
			if (_number != value) {
				int old = _number;
				_number = value;
				UpdateButtons();
				UpdateTextBox();
				ValueChanged?.Invoke(this, new ValueChangedEventArgs<int>(old, _number));
			}
		}
	}

	public int Maximum {
		get => _maximum;
		set {
			if (value < _minimum) throw new ArgumentOutOfRangeException(nameof(Maximum));
			_maximum = value;
			if (_number > _maximum) Value = _maximum;
			else UpdateButtons();
		}
	}

	public int Minimum {
		get => _minimum;
		set {
			if (value > _maximum) throw new ArgumentOutOfRangeException(nameof(Minimum));
			_minimum = value;
			if (_number < _minimum) Value = _minimum;
			else UpdateButtons();
		}
	}

	public int Increment { get => _increment; set => _increment = value; }

	public IntSpinner() {
		InitializeComponent();
		UpdateButtons();
		UpdateTextBox();
	}

	private void UpdateButtons() {
		btnUp.IsEnabled = _number < _maximum;
		btnDown.IsEnabled = _number > _minimum;
	}

	private void UpdateTextBox() {
		if (_updating) return;
		_updating = true;
		int caret = textBox.CaretIndex;
		textBox.Text = _number.ToString();
		textBox.CaretIndex = Math.Min(textBox.Text.Length, caret);
		UpdateTextBoxError();
		_updating = false;
	}

	private void UpdateTextBoxError() {
		bool error = false;
		if (int.TryParse(textBox.Text, out int parsed)) {
			if (parsed < _minimum || parsed > _maximum) error = true;
		}
		else if (textBox.Text is not ("" or "-")) {
			error = true;
		}
		textBox.Foreground = error ? new SolidColorBrush(Color.FromRgb(220, 0, 0)) : null;
	}

	private void OnTextChanged(object? sender, TextChangedEventArgs e) {
		if (_updating) return;
		var text = textBox.Text ?? "";
		if (text == "" || text == "-") {
			_number = _errorValue;
			UpdateButtons();
			UpdateTextBoxError();
			return;
		}
		if (int.TryParse(text, out int n)) {
			if (n > _maximum) { n = _maximum; }
			else if (n < _minimum) { n = _minimum; }
			int old = _number;
			_number = n;
			UpdateButtons();
			if (_number != old)
				ValueChanged?.Invoke(this, new ValueChangedEventArgs<int>(old, _number));
		}
		UpdateTextBoxError();
	}

	private void OnIncrease(object? sender, RoutedEventArgs e) {
		int old = _number;
		_number = Math.Min(_maximum, _number + _increment);
		if (_number != old) {
			UpdateButtons();
			UpdateTextBox();
			ValueChanged?.Invoke(this, new ValueChangedEventArgs<int>(old, _number));
		}
	}

	private void OnDecrease(object? sender, RoutedEventArgs e) {
		int old = _number;
		_number = Math.Max(_minimum, _number - _increment);
		if (_number != old) {
			UpdateButtons();
			UpdateTextBox();
			ValueChanged?.Invoke(this, new ValueChangedEventArgs<int>(old, _number));
		}
	}

	private void OnFocusLost(object? sender, RoutedEventArgs e) {
		_updating = false;
		UpdateTextBox();
	}
}
