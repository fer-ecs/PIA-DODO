using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Vinoteca.Helpers
{
	// esta seccion sirve para agrupar las validaciones y apoyos de entrada y dejar esa responsabilidad en un solo archivo - InputRestrictionsHelper
	public static class InputRestrictionsHelper
	{
		private const string ModoSinEspacios = "SinEspacios";
		private const string ModoLetrasConEspacios = "LetrasConEspacios";
		private const string ModoLetrasNumeros = "LetrasNumeros";
		private const string ModoSoloNumeros = "SoloNumeros";
		private const string ModoSoloDecimal = "SoloDecimal";
		private const string ModoTextoLibre = "TextoLibre";
		private const string ModoCorreoLocal = "CorreoLocal";
		private const string ModoCorreoCompleto = "CorreoCompleto";
		private const string ModoDominioCorreo = "DominioCorreo";

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarSinEspaciosNiEnter
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

		// esta seccion sirve para manejar las validaciones y apoyos de entrada y concentrar aqui esta parte del flujo - ConfigurarTextBox
		private static void ConfigurarTextBox(TextBox textBox)
		{
			textBox.Tag = ModoSinEspacios;
			textBox.KeyDown -= TextBox_KeyDown;
			textBox.KeyDown += TextBox_KeyDown;

			textBox.TextChanged -= TextBox_TextChanged;
			textBox.TextChanged += TextBox_TextChanged;
		}

		// esta seccion sirve para manejar las validaciones y apoyos de entrada y concentrar aqui esta parte del flujo - ConfigurarPasswordBox
		private static void ConfigurarPasswordBox(PasswordBox passwordBox)
		{
			passwordBox.Tag = ModoSinEspacios;
			passwordBox.KeyDown -= PasswordBox_KeyDown;
			passwordBox.KeyDown += PasswordBox_KeyDown;

			passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
			passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
		}

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarSoloLetrasConEspacios
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

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarSoloLetrasNumeros
		public static void AplicarSoloLetrasNumeros(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoLetrasNumeros;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarSinEspacios
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

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarSoloNumeros
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

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarSoloDecimal
		public static void AplicarSoloDecimal(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoSoloDecimal;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarTextoLibreSinEnter
		public static void AplicarTextoLibreSinEnter(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoTextoLibre;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarCorreoLocal
		public static void AplicarCorreoLocal(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoCorreoLocal;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarCorreoCompleto
		public static void AplicarCorreoCompleto(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoCorreoCompleto;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		// esta seccion sirve para ordenar y ajustar datos de las validaciones y apoyos de entrada para trabajar con valores limpios - AplicarDominioCorreo
		public static void AplicarDominioCorreo(params TextBox[] textBoxes)
		{
			foreach (var textBox in textBoxes)
			{
				textBox.Tag = ModoDominioCorreo;
				textBox.KeyDown -= TextBox_KeyDown;
				textBox.KeyDown += TextBox_KeyDown;
				textBox.TextChanged -= TextBox_TextChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}

		// esta seccion sirve para manejar las validaciones y apoyos de entrada y concentrar aqui esta parte del flujo - TextBox_KeyDown
		private static void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not TextBox textBox)
			{
				return;
			}

			string modo = textBox.Tag?.ToString() ?? ModoSinEspacios;
			if (e.Key == VirtualKey.Enter || ((modo == ModoSinEspacios || modo == ModoLetrasNumeros || modo == ModoSoloNumeros || modo == ModoSoloDecimal || modo == ModoCorreoLocal || modo == ModoCorreoCompleto || modo == ModoDominioCorreo) && e.Key == VirtualKey.Space))
			{
				e.Handled = true;
			}
		}

		// esta seccion sirve para manejar las validaciones y apoyos de entrada y concentrar aqui esta parte del flujo - PasswordBox_KeyDown
		private static void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
			}
		}

		// esta seccion sirve para responder a la accion del usuario en las validaciones y apoyos de entrada y mover el flujo al siguiente paso - TextBox_TextChanged
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
				ModoLetrasNumeros => LimpiarSoloLetrasNumeros(textBox.Text),
				ModoSoloNumeros => LimpiarSoloNumeros(textBox.Text),
				ModoSoloDecimal => LimpiarSoloDecimal(textBox.Text),
				ModoCorreoLocal => LimpiarCorreoLocal(textBox.Text),
				ModoCorreoCompleto => LimpiarCorreoCompleto(textBox.Text),
				ModoDominioCorreo => LimpiarDominioCorreo(textBox.Text),
				ModoTextoLibre => textBox.Text,
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

		// esta seccion sirve para responder a la accion del usuario en las validaciones y apoyos de entrada y mover el flujo al siguiente paso - PasswordBox_PasswordChanged
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

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - QuitarEspaciosEnBlanco
		private static string QuitarEspaciosEnBlanco(string texto)
		{
			return string.Concat(texto.Where(c => !char.IsWhiteSpace(c)));
		}

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - LimpiarLetrasConEspacios
		private static string LimpiarLetrasConEspacios(string texto)
		{
			string soloLetras = string.Concat(texto.Where(c => char.IsLetter(c) || c == ' '));
			return Regex.Replace(soloLetras, @" {2,}", " ").TrimStart();
		}

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - LimpiarSoloNumeros
		private static string LimpiarSoloNumeros(string texto)
		{
			return string.Concat(texto.Where(char.IsDigit));
		}

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - LimpiarSoloLetrasNumeros
		private static string LimpiarSoloLetrasNumeros(string texto)
		{
			return Regex.Replace(texto, "[^A-Za-z0-9]", string.Empty);
		}

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - LimpiarSoloDecimal
		private static string LimpiarSoloDecimal(string texto)
		{
			var resultado = new StringBuilder();
			bool tieneSeparador = false;

			foreach (char caracter in texto.Replace(',', '.'))
			{
				if (char.IsDigit(caracter))
				{
					resultado.Append(caracter);
				}
				else if (caracter == '.' && !tieneSeparador)
				{
					resultado.Append(caracter);
					tieneSeparador = true;
				}
			}

			return resultado.ToString();
		}

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - LimpiarCorreoLocal
		private static string LimpiarCorreoLocal(string texto)
		{
			return string.Concat(texto.Where(c =>
				char.IsLetterOrDigit(c) ||
				c == '_' ||
				c == '-' ||
				c == '.'));
		}

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - LimpiarCorreoCompleto
		private static string LimpiarCorreoCompleto(string texto)
		{
			var resultado = new StringBuilder();
			bool tieneArroba = false;

			foreach (char caracter in texto.ToLowerInvariant())
			{
				if (!tieneArroba)
				{
					if (char.IsLetterOrDigit(caracter) || caracter == '_' || caracter == '-' || caracter == '.')
					{
						resultado.Append(caracter);
					}
					else if (caracter == '@' && resultado.Length > 0)
					{
						resultado.Append(caracter);
						tieneArroba = true;
					}
				}
				else if (char.IsLetterOrDigit(caracter) || caracter == '.')
				{
					resultado.Append(caracter);
				}
			}

			return resultado.ToString();
		}

		// esta seccion sirve para quitar informacion de las validaciones y apoyos de entrada y dejar el estado consistente - LimpiarDominioCorreo
		private static string LimpiarDominioCorreo(string texto)
		{
			return string.Concat(texto.ToLowerInvariant().Where(c =>
				char.IsLetterOrDigit(c) ||
				c == '.'));
		}
	}
}
