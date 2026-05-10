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
	public sealed partial class RegisterView : Page, ICambiosPendientes
	{
		private const string CACHE_KEY_NOMBRE = "Register_Nombre";
		private const string CACHE_KEY_CORREO = "Register_Correo";
		private const string CACHE_KEY_PASSWORD = "Register_Password";
		private const string CACHE_KEY_CONFIRMAR = "Register_ConfirmarPassword";

		public RegisterView()
		{
			this.InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);

			// Cargar valores del cache
			CargarValoresDelCache();

			// Suscribirse a cambios de texto para guardar en cache
			txtNombre.TextChanged += (s, e) => GuardarEnCache(CACHE_KEY_NOMBRE, txtNombre.Text);
			txtCorreo.TextChanged += (s, e) => GuardarEnCache(CACHE_KEY_CORREO, txtCorreo.Text);
			txtPassword.PasswordChanged += (s, e) => GuardarEnCache(CACHE_KEY_PASSWORD, txtPassword.Password);
			txtConfirmarPassword.PasswordChanged += (s, e) => GuardarEnCache(CACHE_KEY_CONFIRMAR, txtConfirmarPassword.Password);

			txtNombre.Focus(FocusState.Programmatic);
		}

		private void CargarValoresDelCache()
		{
			var nombre = App.FormCacheService.GetValue(CACHE_KEY_NOMBRE);
			if (!string.IsNullOrEmpty(nombre))
				txtNombre.Text = nombre;

			var correo = App.FormCacheService.GetValue(CACHE_KEY_CORREO);
			if (!string.IsNullOrEmpty(correo))
				txtCorreo.Text = correo;

			var password = App.FormCacheService.GetValue(CACHE_KEY_PASSWORD);
			if (!string.IsNullOrEmpty(password))
				txtPassword.Password = password;

			var confirmar = App.FormCacheService.GetValue(CACHE_KEY_CONFIRMAR);
			if (!string.IsNullOrEmpty(confirmar))
				txtConfirmarPassword.Password = confirmar;
		}

		private void GuardarEnCache(string clave, string valor)
		{
			if (!string.IsNullOrEmpty(valor))
				App.FormCacheService.SetValue(clave, valor);
		}

		private void LimpiarCache()
		{
			App.FormCacheService.ClearAll();
			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
			OcultarMensajes();
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
				Rol = RolesSistema.Cliente,
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

			// Limpiar el cache después de registrarse correctamente
			LimpiarCache();
		}

		private bool EsContrasenaFuerte(string password)
		{
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s])[^\s]{6,}$";
			return Regex.IsMatch(password, patron);
		}

		public bool TieneCambiosPendientes =>
			!string.IsNullOrWhiteSpace(txtNombre.Text) ||
			!string.IsNullOrWhiteSpace(txtCorreo.Text) ||
			!string.IsNullOrWhiteSpace(txtPassword.Password) ||
			!string.IsNullOrWhiteSpace(txtConfirmarPassword.Password);

		public string ObtenerMensajeCambiosPendientes()
		{
			return "Hay datos del registro sin terminar.";
		}

		private async void BtnVolverLogin_Click(object sender, RoutedEventArgs e)
		{
			bool puedeSalir = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
				XamlRoot,
				this,
				"volver al login",
				false);
			if (!puedeSalir)
			{
				return;
			}

			Frame.Navigate(typeof(LoginView));
		}

		private void MostrarError(string mensaje)
		{
			txtError.Text = mensaje;
			txtError.Visibility = Visibility.Visible;
			txtExito.Visibility = Visibility.Collapsed;

            btnRegistrar.IsEnabled = true;
        }

        private void OcultarMensajes()
		{
			txtError.Visibility = Visibility.Collapsed;
			txtExito.Visibility = Visibility.Collapsed;

            btnRegistrar.IsEnabled = true;
        }

        private void BtnVerPassword_Checked(object sender, RoutedEventArgs e)
        {
            txtPassword.PasswordRevealMode = PasswordRevealMode.Visible;
            iconoOjoPassword.Glyph = "\uED1A";
        }

        private void BtnVerPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            txtPassword.PasswordRevealMode = PasswordRevealMode.Hidden;
            iconoOjoPassword.Glyph = "\uE7B3";
        }

        private void BtnVerConfirmar_Checked(object sender, RoutedEventArgs e)
        {
            txtConfirmarPassword.PasswordRevealMode = PasswordRevealMode.Visible;
            iconoOjoConfirmar.Glyph = "\uED1A";
        }

        private void BtnVerConfirmar_Unchecked(object sender, RoutedEventArgs e)
        {
            txtConfirmarPassword.PasswordRevealMode = PasswordRevealMode.Hidden;
            iconoOjoConfirmar.Glyph = "\uE7B3";
        }

        private void BtnLimpiarCache_Click(object sender, RoutedEventArgs e)
        {
            LimpiarCache();
        }
    }

}

