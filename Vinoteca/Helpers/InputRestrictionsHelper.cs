using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Vinoteca.Helpers
{
	public static class InputRestrictionsHelper
	{
		private const string ModoSinEspacios = "SinEspacios";
		private const string ModoLetrasConEspacios = "LetrasConEspacios";
		private const string ModoSoloNumeros = "SoloNumeros";

		public static void AplicarSinEspaciosNiEnter(DependencyObject parent)
		{
			int total = VisualTreeHelper.GetChildrenCount(parent);

			for (int i = 0; i < total; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);

				if (child is TextBox textBox)
				{
					ConfigurarTextBox(textBox);
				}
				else if (child is PasswordBox passwordBox)
				{
					ConfigurarPasswordBox(passwordBox);
				}

				AplicarSinEspaciosNiEnter(child);
			}
		}

		private static void ConfigurarTextBox(TextBox textBox)
		{
			textBox.Tag = ModoSinEspacios;
			textBox.KeyDown -= TextBox_KeyDown;
			textBox.KeyDown += TextBox_KeyDown;

			textBox.TextChanged -= TextBox_TextChanged;
			textBox.TextChanged += TextBox_TextChanged;
		}

		private static void ConfigurarPasswordBox(PasswordBox passwordBox)
		{
			passwordBox.Tag = ModoSinEspacios;
			passwordBox.KeyDown -= PasswordBox_KeyDown;
			passwordBox.KeyDown += PasswordBox_KeyDown;

			passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
			passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
		}

		public static void AplicarSoloLetrasConEspacios(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoLetrasConEspacios;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		public static void AplicarSinEspacios(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoSinEspacios;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		public static void AplicarSoloNumeros(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoSoloNumeros;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		private static void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not TextBox textBox)
			{
				return;
			}

			string modo = textBox.Tag?.ToString() ?? ModoSinEspacios;
			if (e.Key == VirtualKey.Enter || ((modo == ModoSinEspacios || modo == ModoSoloNumeros) && e.Key == VirtualKey.Space))
			{
				e.Handled = true;
			}
		}

		private static void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
			}
		}

		private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is not TextBox textBox || textBox.FocusState == FocusState.Unfocused)
			{
				return;
			}

			string modo = textBox.Tag?.ToString() ?? ModoSinEspacios;
			string limpio = modo switch
			{
				ModoLetrasConEspacios => LimpiarLetrasConEspacios(textBox.Text),
				ModoSoloNumeros => LimpiarSoloNumeros(textBox.Text),
				_ => QuitarEspaciosEnBlanco(textBox.Text)
			};
			if (limpio == textBox.Text)
			{
				return;
			}

			int nuevoInicio = textBox.SelectionStart > 0 ? textBox.SelectionStart - 1 : 0;
			textBox.Text = limpio;
			textBox.SelectionStart = nuevoInicio <= textBox.Text.Length ? nuevoInicio : textBox.Text.Length;
		}

		private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (sender is not PasswordBox passwordBox || passwordBox.FocusState == FocusState.Unfocused)
			{
				return;
			}

			string limpio = QuitarEspaciosEnBlanco(passwordBox.Password);
			if (limpio != passwordBox.Password)
			{
				passwordBox.Password = limpio;
			}
		}

		private static string QuitarEspaciosEnBlanco(string texto)
		{
			return string.Concat(texto.Where(c => !char.IsWhiteSpace(c)));
		}

		private static string LimpiarLetrasConEspacios(string texto)
		{
			string soloLetras = string.Concat(texto.Where(c => char.IsLetter(c) || c == ' '));
			return Regex.Replace(soloLetras, @" {2,}", " ").TrimStart();
		}

		private static string LimpiarSoloNumeros(string texto)
		{
			return string.Concat(texto.Where(char.IsDigit));
		}
	}
}
