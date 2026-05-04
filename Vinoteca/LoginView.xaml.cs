using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;
using System.Text.RegularExpressions;

namespace Vinoteca.Views
{
	public sealed partial class LoginView : Page
	{
		private int intentosFallidos = 0;
		private int maxIntentos = 3;
		private DateTime? ultimoIntento;

		public LoginView()
		{
			this.InitializeComponent();
			txtCorreo.Focus(FocusState.Programmatic);
		}

		private void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			// Ocultar error previo
			txtError.Visibility = Visibility.Collapsed;

			string correo = txtCorreo.Text.Trim();
			string password = txtPassword.Password.Trim();

			// Validar intentos fallidos
			if (intentosFallidos >= maxIntentos)
			{
				if (ultimoIntento.HasValue && DateTime.Now.Subtract(ultimoIntento.Value).TotalMinutes < 5)
				{
					MostrarError("Demasiados intentos. Espere 5 minutos.");
					return;
				}
				else
				{
					intentosFallidos = 0;
				}
			}

			// Validar campos vacíos
			if (string.IsNullOrEmpty(correo))
			{
				MostrarError("El correo es obligatorio.");
				return;
			}

			if (string.IsNullOrEmpty(password))
			{
				MostrarError("La contraseña es obligatoria.");
				return;
			}

			// Validar formato de correo
			string patternEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			if (!Regex.IsMatch(correo, patternEmail))
			{
				MostrarError("Ingresa un formato válido para el correo electrónico.");
				return;
			}

			// Validar que no contengan espacios
			if (correo.Contains(" ") || password.Contains(" "))
			{
				MostrarError("El correo y la contraseña no deben contener espacios.");
				return;
			}

			// Validar longitud mínima de contraseña
			if (password.Length < 6)
			{
				MostrarError("La contraseña debe contener al menos 6 caracteres.");
				return;
			}

			// Validar contraseña fuerte
			if (!EsContraseñaFuerte(password))
			{
				MostrarError("Contraseña débil. Use mayúsculas, números y caracteres especiales.");
				return;
			}

			// Validar contra el JSON
			var usuarios = DataService.ObtenerUsuarios();
			var usuario = usuarios.FirstOrDefault(u => u.Correo == correo);

			// Validar si el usuario existe y las credenciales son correctas
			if (usuario == null || usuario.Contrasena != password)
			{
				intentosFallidos++;
				ultimoIntento = DateTime.Now;
				MostrarError($"Credenciales incorrectas. Intento {intentosFallidos}/{maxIntentos}");
				return;
			}

			// Validar que el usuario esté activo
			if (!usuario.Activo)
			{
				MostrarError("Esta cuenta ha sido desactivada. Contacte al administrador.");
				return;
			}

			// Login exitoso - resetear intentos
			intentosFallidos = 0;
			SessionService.IniciarSesion(usuario);

			// Redirección por rol
			if (usuario.EsAdmin)
			{
				this.Frame.Navigate(typeof(AdminMenuView));
			}
			else
			{
				this.Frame.Navigate(typeof(UsuarioMenuView));
			}
		}

		private bool EsContraseñaFuerte(string password)
		{
			// Debe cumplir:
			// - Al menos 6 caracteres
			// - Una mayúscula
			// - Una minúscula
			// - Un número
			// - Un carácter especial
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$";
			return Regex.IsMatch(password, patron);
		}

		private void MostrarError(string mensaje)
		{
			txtError.Text = mensaje;
			txtError.Visibility = Visibility.Visible;
		}
	}
}