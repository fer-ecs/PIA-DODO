using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

	namespace Vinoteca.Views
	{
	// esta seccion sirve para agrupar el registro de usuarios y dejar esa responsabilidad en un solo archivo - RegisterView
	public sealed partial class RegisterView : Page, ICambiosPendientes, IDescartaCambiosPendientes
	{
		private const string CACHE_PREFIX = "Register_";
		private const string CACHE_KEY_NOMBRE = "Register_Nombre";
		private const string CACHE_KEY_CORREO = "Register_Correo";
		private const string CACHE_KEY_DOMINIO = "Register_DominioCorreo";
		private const string CACHE_KEY_PASSWORD = "Register_Password";
		private const string CACHE_KEY_CONFIRMAR = "Register_ConfirmarPassword";
		private List<string> dominiosCorreoPermitidos = new();

		// esta seccion sirve para agrupar el registro de usuarios y dejar esa responsabilidad en un solo archivo - RegisterView
		public RegisterView()
		{
			this.InitializeComponent();
			ConfigurarDominiosCorreo();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			InputRestrictionsHelper.AplicarSoloLetrasConEspacios(txtNombre);
			InputRestrictionsHelper.AplicarCorreoLocal(txtCorreo);

			// Recupera lo que el usuario ya habia escrito
			CargarValoresDelCache();

			// Va guardando cada cambio para no perder el formulario
			txtNombre.TextChanged += (s, e) => GuardarEnCache(CACHE_KEY_NOMBRE, txtNombre.Text);
			txtCorreo.TextChanged += (s, e) => GuardarEnCache(CACHE_KEY_CORREO, txtCorreo.Text);
			cmbDominioCorreo.SelectionChanged += (s, e) => GuardarEnCache(CACHE_KEY_DOMINIO, ObtenerDominioCorreoActual());
			txtPassword.PasswordChanged += (s, e) => GuardarEnCache(CACHE_KEY_PASSWORD, txtPassword.Password);
			txtConfirmarPassword.PasswordChanged += (s, e) => GuardarEnCache(CACHE_KEY_CONFIRMAR, txtConfirmarPassword.Password);

			txtNombre.Focus(FocusState.Programmatic);
		}

		// esta seccion sirve para cargar informacion de el registro de usuarios y preparar lo que se muestra en pantalla - CargarValoresDelCache
		private void CargarValoresDelCache()
		{
			var nombre = App.FormCacheService.GetValue(CACHE_KEY_NOMBRE);
			if (!string.IsNullOrEmpty(nombre))
				txtNombre.Text = nombre;

			var correo = App.FormCacheService.GetValue(CACHE_KEY_CORREO);
			if (!string.IsNullOrEmpty(correo))
			{
				SepararCorreo(correo, out string correoLocal, out string dominioCorreo);
				txtCorreo.Text = correoLocal;
				SeleccionarDominioCorreo(dominioCorreo);
			}

			var dominio = App.FormCacheService.GetValue(CACHE_KEY_DOMINIO);
			if (!string.IsNullOrEmpty(dominio))
				SeleccionarDominioCorreo(dominio);

			var password = App.FormCacheService.GetValue(CACHE_KEY_PASSWORD);
			if (!string.IsNullOrEmpty(password))
				txtPassword.Password = password;

			var confirmar = App.FormCacheService.GetValue(CACHE_KEY_CONFIRMAR);
			if (!string.IsNullOrEmpty(confirmar))
				txtConfirmarPassword.Password = confirmar;
		}

		// esta seccion sirve para guardar informacion de el registro de usuarios y mantener los datos persistidos - GuardarEnCache
		private void GuardarEnCache(string clave, string valor)
		{
			if (!string.IsNullOrEmpty(valor))
			{
				App.FormCacheService.SetValue(clave, valor);
			}
			else
			{
				App.FormCacheService.RemoveValue(clave);
			}
		}

		// esta seccion sirve para quitar informacion de el registro de usuarios y dejar el estado consistente - LimpiarCache
		private void LimpiarCache()
		{
			App.FormCacheService.ClearPrefix(CACHE_PREFIX);
			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			cmbDominioCorreo.SelectedIndex = 0;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
			OcultarMensajes();
		}

		public void DescartarCambiosPendientes()
		{
			LimpiarCache();
		}

		// esta seccion sirve para responder a la accion del usuario en el registro de usuarios y mover el flujo al siguiente paso - BtnRegistrar_Click
		private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
		{
			OcultarMensajes();

			string nombre = txtNombre.Text;
			string correoLocal = txtCorreo.Text;
			string dominioCorreo = ObtenerDominioCorreoActual();
			string correo = ConstruirCorreo(correoLocal, dominioCorreo);
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

			if (!FormValidationHelper.EsTextoConLetrasYEspacios(nombre))
			{
				MostrarError("El nombre solo debe contener letras y espacios entre palabras");
				return;
			}

			if (string.IsNullOrWhiteSpace(correoLocal))
			{
				MostrarError("El correo es obligatorio");
				return;
			}

			if (correoLocal != correoLocal.Trim())
			{
				MostrarError("El correo no debe tener espacios al inicio o al final");
				return;
			}

			if (nombre.Contains("  "))
			{
				MostrarError("El nombre no debe contener espacios dobles");
				return;
			}

			if (correoLocal.Contains("@"))
			{
				MostrarError("Escribe solo el nombre del correo, sin @");
				return;
			}

			if (correoLocal.Any(char.IsWhiteSpace))
			{
				MostrarError("El correo no debe contener espacios");
				return;
			}

			if (correoLocal.Length > 40)
			{
				MostrarError("El nombre del correo no debe exceder 40 caracteres");
				return;
			}

			if (!EsNombreCorreoValido(correoLocal))
			{
				MostrarError("El nombre del correo solo permite letras, numeros, punto, guion y guion bajo");
				return;
			}

			if (!dominiosCorreoPermitidos.Any(d => d.Equals(dominioCorreo, StringComparison.OrdinalIgnoreCase)))
			{
				MostrarError("Selecciona un dominio de correo valido");
				return;
			}

			if (!FormValidationHelper.EsCorreoValido(correo))
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
				Nombre = nombre,
				Correo = correo.Trim(),
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

			LimpiarCache();
			Frame.Navigate(typeof(LoginView));
		}

		// esta seccion sirve para revisar reglas de el registro de usuarios y evitar que pase un dato incorrecto - EsContrasenaFuerte
		private bool EsContrasenaFuerte(string password)
		{
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s])[^\s]{6,}$";
			return Regex.IsMatch(password, patron);
		}

		// esta seccion sirve para revisar reglas de el registro de usuarios y evitar que pase un dato incorrecto - EsNombreCorreoValido
		private static bool EsNombreCorreoValido(string correoLocal)
		{
			string nombre = correoLocal.Trim();
			return !nombre.Contains("..") &&
				Regex.IsMatch(nombre, @"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,38}[A-Za-z0-9])?$");
		}

		// esta seccion sirve para manejar el registro de usuarios y concentrar aqui esta parte del flujo - ConfigurarDominiosCorreo
		private void ConfigurarDominiosCorreo()
		{
			dominiosCorreoPermitidos = DataService.ObtenerDominiosCorreo();
			cmbDominioCorreo.Items.Clear();
			foreach (string dominio in dominiosCorreoPermitidos)
			{
				cmbDominioCorreo.Items.Add("@" + dominio);
			}

			cmbDominioCorreo.SelectedIndex = cmbDominioCorreo.Items.Count > 0 ? 0 : -1;
		}

		// esta seccion sirve para leer informacion de el registro de usuarios y regresarla lista para usarse - ObtenerDominioCorreoActual
		private string ObtenerDominioCorreoActual()
		{
			string dominioDefault = dominiosCorreoPermitidos.FirstOrDefault() ?? "gmail.com";
			string seleccionado = cmbDominioCorreo.SelectedItem?.ToString() ?? "@" + dominioDefault;
			return seleccionado.TrimStart('@');
		}

		// esta seccion sirve para armar datos o contenido de el registro de usuarios y devolverlo ya preparado - ConstruirCorreo
		private string ConstruirCorreo(string correoLocal, string dominioCorreo)
		{
			return $"{correoLocal.Trim()}@{dominioCorreo}";
		}

		// esta seccion sirve para manejar el registro de usuarios y concentrar aqui esta parte del flujo - SeleccionarDominioCorreo
		private void SeleccionarDominioCorreo(string dominioCorreo)
		{
			if (string.IsNullOrWhiteSpace(dominioCorreo))
			{
				cmbDominioCorreo.SelectedIndex = 0;
				return;
			}

			string dominio = dominioCorreo.Trim().TrimStart('@');
			int indice = dominiosCorreoPermitidos.FindIndex(d => d.Equals(dominio, StringComparison.OrdinalIgnoreCase));
			cmbDominioCorreo.SelectedIndex = indice >= 0 ? indice : 0;
		}

		// esta seccion sirve para manejar el registro de usuarios y concentrar aqui esta parte del flujo - SepararCorreo
		private void SepararCorreo(string correo, out string correoLocal, out string dominioCorreo)
		{
			correoLocal = correo;
			dominioCorreo = dominiosCorreoPermitidos.FirstOrDefault() ?? "gmail.com";

			if (string.IsNullOrWhiteSpace(correo))
			{
				correoLocal = string.Empty;
				return;
			}

			string[] partes = correo.Split('@', 2);
			if (partes.Length != 2)
			{
				return;
			}

			correoLocal = partes[0];
			dominioCorreo = partes[1];
		}

		public bool TieneCambiosPendientes =>
			!string.IsNullOrWhiteSpace(txtNombre.Text) ||
			!string.IsNullOrWhiteSpace(txtCorreo.Text) ||
			!string.IsNullOrWhiteSpace(txtPassword.Password) ||
			!string.IsNullOrWhiteSpace(txtConfirmarPassword.Password);

		// esta seccion sirve para leer informacion de el registro de usuarios y regresarla lista para usarse - ObtenerMensajeCambiosPendientes
		public string ObtenerMensajeCambiosPendientes()
		{
			return "Hay datos del registro sin terminar";
		}

		// esta seccion sirve para responder a la accion del usuario en el registro de usuarios y mover el flujo al siguiente paso - BtnVolverLogin_Click
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

		// esta seccion sirve para mostrar mensajes o ventanas de el registro de usuarios para que el usuario entienda el estado - MostrarError
		private void MostrarError(string mensaje)
		{
			txtError.Text = mensaje;
			txtError.Visibility = Visibility.Visible;
			txtExito.Visibility = Visibility.Collapsed;

            btnRegistrar.IsEnabled = true;
        }

        // esta seccion sirve para manejar el registro de usuarios y concentrar aqui esta parte del flujo - OcultarMensajes
        private void OcultarMensajes()
		{
			txtError.Visibility = Visibility.Collapsed;
			txtExito.Visibility = Visibility.Collapsed;

            btnRegistrar.IsEnabled = true;
        }

        // esta seccion sirve para responder a la accion del usuario en el registro de usuarios y mover el flujo al siguiente paso - BtnVerPassword_Checked
        private void BtnVerPassword_Checked(object sender, RoutedEventArgs e)
        {
            txtPassword.PasswordRevealMode = PasswordRevealMode.Visible;
            iconoOjoPassword.Glyph = "\uED1A";
        }

        // esta seccion sirve para responder a la accion del usuario en el registro de usuarios y mover el flujo al siguiente paso - BtnVerPassword_Unchecked
        private void BtnVerPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            txtPassword.PasswordRevealMode = PasswordRevealMode.Hidden;
            iconoOjoPassword.Glyph = "\uE7B3";
        }

        // esta seccion sirve para responder a la accion del usuario en el registro de usuarios y mover el flujo al siguiente paso - BtnVerConfirmar_Checked
        private void BtnVerConfirmar_Checked(object sender, RoutedEventArgs e)
        {
            txtConfirmarPassword.PasswordRevealMode = PasswordRevealMode.Visible;
            iconoOjoConfirmar.Glyph = "\uED1A";
        }

        // esta seccion sirve para responder a la accion del usuario en el registro de usuarios y mover el flujo al siguiente paso - BtnVerConfirmar_Unchecked
        private void BtnVerConfirmar_Unchecked(object sender, RoutedEventArgs e)
        {
            txtConfirmarPassword.PasswordRevealMode = PasswordRevealMode.Hidden;
            iconoOjoConfirmar.Glyph = "\uE7B3";
        }

        // esta seccion sirve para responder a la accion del usuario en el registro de usuarios y mover el flujo al siguiente paso - BtnLimpiarCache_Click
        private void BtnLimpiarCache_Click(object sender, RoutedEventArgs e)
        {
            LimpiarCache();
        }
    }

}

