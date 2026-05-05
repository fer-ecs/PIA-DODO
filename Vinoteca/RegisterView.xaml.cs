using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class RegisterView : Page
	{
		public RegisterView()
		{
			this.InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			txtNombre.Focus(FocusState.Programmatic);
		}

		private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
		{
			OcultarMensajes();

			string nombre = txtNombre.Text;
			string correo = txtCorreo.Text;
			string password = txtPassword.Password;
			string confirmarPassword = txtConfirmarPassword.Password;

			if (string.IsNullOrWhiteSpace(nombre))
			{
				MostrarError("El nombre es obligatorio");
				return;
			}

			if (nombre != nombre.Trim())
			{
				MostrarError("El nombre no debe tener espacios al inicio o al final");
				return;
			}

			if (nombre.Length < 3)
			{
				MostrarError("El nombre debe tener al menos 3 caracteres");
				return;
			}

			if (nombre.Any(char.IsDigit))
			{
				MostrarError("El nombre no debe contener numeros");
				return;
			}

			if (string.IsNullOrWhiteSpace(correo))
			{
				MostrarError("El correo es obligatorio");
				return;
			}

			if (correo != correo.Trim())
			{
				MostrarError("El correo no debe tener espacios al inicio o al final");
				return;
			}

			if (nombre.Contains("  "))
			{
				MostrarError("El nombre no debe contener espacios dobles");
				return;
			}

			if (correo.Any(char.IsWhiteSpace))
			{
				MostrarError("El correo no debe contener espacios");
				return;
			}

			string patternEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			if (!Regex.IsMatch(correo, patternEmail))
			{
				MostrarError("Ingresa un formato valido para el correo electronico");
				return;
			}

			if (string.IsNullOrWhiteSpace(password))
			{
				MostrarError("La contrasena es obligatoria");
				return;
			}

			if (string.IsNullOrWhiteSpace(confirmarPassword))
			{
				MostrarError("Debes confirmar la contrasena");
				return;
			}

			if (password != password.Trim() || confirmarPassword != confirmarPassword.Trim())
			{
				MostrarError("Las contrasenas no deben tener espacios al inicio o al final");
				return;
			}

			if (password.Any(char.IsWhiteSpace) || confirmarPassword.Any(char.IsWhiteSpace))
			{
				MostrarError("Las contrasenas no deben contener espacios");
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

			if (password != confirmarPassword)
			{
				MostrarError("Las contrasenas no coinciden");
				return;
			}

			if (DataService.ObtenerUsuarioPorCorreo(correo) != null)
			{
				MostrarError("Ya existe una cuenta registrada con ese correo");
				return;
			}

			var nuevoUsuario = new Usuario
			{
				Id = Guid.NewGuid().ToString(),
				Nombre = nombre,
				Correo = correo,
				Contrasena = password,
				EsAdmin = false,
				Activo = true
			};

			bool guardado = DataService.GuardarUsuario(nuevoUsuario);
			if (!guardado)
			{
				MostrarError("No se pudo crear la cuenta, intenta nuevamente");
				return;
			}

			txtExito.Text = "Cuenta creada correctamente, ahora puedes iniciar sesion";
			txtExito.Visibility = Visibility.Visible;

			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
		}

		private bool EsContrasenaFuerte(string password)
		{
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s])[^\s]{6,}$";
			return Regex.IsMatch(password, patron);
		}

		private void BtnVolverLogin_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(LoginView));
		}

		private void MostrarError(string mensaje)
		{
			txtError.Text = mensaje;
			txtError.Visibility = Visibility.Visible;
			txtExito.Visibility = Visibility.Collapsed;
		}

		private void OcultarMensajes()
		{
			txtError.Visibility = Visibility.Collapsed;
			txtExito.Visibility = Visibility.Collapsed;
		}
	}
}
