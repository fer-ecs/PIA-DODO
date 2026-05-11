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
		private const int MaxIntentos = 3;
		private const int MinutosBloqueo = 5;

		public LoginView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			txtCorreo.Focus(FocusState.Programmatic);
		}

		private void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				txtError.Visibility = Visibility.Collapsed;

				string correo = txtCorreo.Text;
				string password = txtPassword.Password;

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

				if (password != password.Trim())
				{
					MostrarError("La contrasena no debe tener espacios al inicio o al final");
					return;
				}

				if (correo.Any(char.IsWhiteSpace))
				{
					MostrarError("El correo no debe contener espacios");
					return;
				}

				if (password.Any(char.IsWhiteSpace))
				{
					MostrarError("La contrasena no debe contener espacios");
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
					RegistrarIntentoFallido(usuario);
					return;
				}

				if (usuario.BloqueadoHasta.HasValue)
				{
					if (usuario.BloqueadoHasta.Value > DateTime.Now)
					{
						int minutos = Math.Max(1, (int)Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.Now).TotalMinutes));
						MostrarError($"Cuenta bloqueada temporalmente, espere {minutos} minuto(s)");
						return;
					}

					LimpiarBloqueo(usuario);
				}

				if (!usuario.Activo)
				{
					MostrarError("Esta cuenta ha sido desactivada, contacte al administrador");
					return;
				}

				LimpiarBloqueo(usuario);
				SessionService.IniciarSesion(usuario);

				if (usuario.Rol == RolesSistema.Administrador)
				{
					Frame.Navigate(typeof(AdminMenuView));
				}
				else if (usuario.Rol == RolesSistema.Supervisor)
				{
					Frame.Navigate(typeof(SupervisorMenuView));
				}
				else
				{
					Frame.Navigate(typeof(UsuarioMenuView));
				}
			}
			catch (Exception ex)
			{
				App.RegistrarError("LOGIN_CLICK", ex.ToString());
				MostrarError($"Error en login: {ex.Message}");
			}
		}

		private bool EsContrasenaFuerte(string password)
		{
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s])[^\s]{6,}$";
			return Regex.IsMatch(password, patron);
		}

		private void RegistrarIntentoFallido(Models.Usuario? usuario)
		{
			if (usuario == null)
			{
				MostrarError("Credenciales incorrectas");
				return;
			}

			if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.Now)
			{
				int minutos = Math.Max(1, (int)Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.Now).TotalMinutes));
				MostrarError($"Cuenta bloqueada temporalmente, espere {minutos} minuto(s)");
				return;
			}

			if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value <= DateTime.Now)
			{
				usuario.IntentosFallidosLogin = 0;
				usuario.BloqueadoHasta = null;
			}

			usuario.IntentosFallidosLogin++;
			if (usuario.IntentosFallidosLogin >= MaxIntentos)
			{
				usuario.BloqueadoHasta = DateTime.Now.AddMinutes(MinutosBloqueo);
				DataService.ActualizarUsuario(usuario);
				MostrarError("Demasiados intentos, espere 5 minutos");
				return;
			}

			DataService.ActualizarUsuario(usuario);
			MostrarError($"Credenciales incorrectas intento {usuario.IntentosFallidosLogin}/{MaxIntentos}");
		}

		private void LimpiarBloqueo(Models.Usuario usuario)
		{
			if (usuario.IntentosFallidosLogin == 0 && usuario.BloqueadoHasta == null)
			{
				return;
			}

			usuario.IntentosFallidosLogin = 0;
			usuario.BloqueadoHasta = null;
			DataService.ActualizarUsuario(usuario);
		}

		private void MostrarError(string mensaje)
		{
			txtError.Text = mensaje;
			txtError.Visibility = Visibility.Visible;
		}

		private void BtnIrRegistro_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(RegisterView));
		}

	}
}
