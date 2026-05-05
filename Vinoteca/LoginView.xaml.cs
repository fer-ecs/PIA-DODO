using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Helpers;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class LoginView : Page
	{
		private int intentosFallidos = 0;
		private int maxIntentos = 3;
		private DateTime? ultimoIntento;

		public LoginView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			txtCorreo.Focus(FocusState.Programmatic);
		}

		private void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			txtError.Visibility = Visibility.Collapsed;

			string correo = txtCorreo.Text;
			string password = txtPassword.Password;

			if (intentosFallidos >= maxIntentos)
			{
				if (ultimoIntento.HasValue && DateTime.Now.Subtract(ultimoIntento.Value).TotalMinutes < 5)
				{
					MostrarError("Demasiados intentos, espere 5 minutos");
					return;
				}

				intentosFallidos = 0;
			}

			if (string.IsNullOrWhiteSpace(correo))
			{
				MostrarError("El correo es obligatorio");
				return;
			}

			if (string.IsNullOrWhiteSpace(password))
			{
				MostrarError("La contrasena es obligatoria");
				return;
			}

			if (correo != correo.Trim() || password != password.Trim())
			{
				MostrarError("El correo y la contrasena no deben tener espacios al inicio o al final");
				return;
			}

			string patternEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			if (!Regex.IsMatch(correo, patternEmail))
			{
				MostrarError("Ingresa un formato valido para el correo electronico");
				return;
			}

			if (correo.Any(char.IsWhiteSpace) || password.Any(char.IsWhiteSpace))
			{
				MostrarError("El correo y la contrasena no deben contener espacios");
				return;
			}

			if (password.Length < 6)
			{
				MostrarError("La contrasena debe contener al menos 6 caracteres");
				return;
			}

			if (!EsContrasenaFuerte(password))
			{
				MostrarError("Contrasena debil, use mayusculas, numeros y caracteres especiales");
				return;
			}

			var usuarios = DataService.ObtenerUsuarios();
			var usuario = usuarios.FirstOrDefault(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));

			if (usuario == null || usuario.Contrasena != password)
			{
				intentosFallidos++;
				ultimoIntento = DateTime.Now;
				MostrarError($"Credenciales incorrectas. Intento {intentosFallidos}/{maxIntentos}");
				return;
			}

			if (!usuario.Activo)
			{
				MostrarError("Esta cuenta ha sido desactivada, contacte al administrador");
				return;
			}

			intentosFallidos = 0;
			SessionService.IniciarSesion(usuario);

			if (usuario.EsAdmin)
			{
				Frame.Navigate(typeof(AdminMenuView));
			}
			else
			{
				Frame.Navigate(typeof(UsuarioMenuView));
			}
		}

		private bool EsContrasenaFuerte(string password)
		{
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s])[^\s]{6,}$";
			return Regex.IsMatch(password, patron);
		}

		private void MostrarError(string mensaje)
		{
			txtError.Text = mensaje;
			txtError.Visibility = Visibility.Visible;
		}

	}
}
