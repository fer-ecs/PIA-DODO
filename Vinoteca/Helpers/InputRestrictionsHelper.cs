using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Vinoteca.Helpers
{
	public static class InputRestrictionsHelper
	{
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
			textBox.KeyDown -= TextBox_KeyDown;
			textBox.KeyDown += TextBox_KeyDown;

			textBox.TextChanged -= TextBox_TextChanged;
			textBox.TextChanged += TextBox_TextChanged;
		}

		private static void ConfigurarPasswordBox(PasswordBox passwordBox)
		{
			passwordBox.KeyDown -= PasswordBox_KeyDown;
			passwordBox.KeyDown += PasswordBox_KeyDown;

			passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
			passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
		}

		private static void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
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

			string limpio = QuitarEspaciosEnBlanco(textBox.Text);
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
	}
}
