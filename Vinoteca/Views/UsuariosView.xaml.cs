using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class UsuariosView : Page
	{
		private const string CorreoAdminPrincipal = "admin@vinoteca.com";

		public ObservableCollection<UsuarioItemViewModel> Usuarios { get; } = new();

		public UsuariosView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			lvUsuarios.ItemsSource = Usuarios;

			if (!SessionService.EsAdminActivo)
			{
				BloquearAccesoNoAdmin();
				return;
			}

			CargarUsuarios();
		}

		private void BloquearAccesoNoAdmin()
		{
			txtNombre.IsEnabled = false;
			txtCorreo.IsEnabled = false;
			txtPassword.IsEnabled = false;
			txtConfirmarPassword.IsEnabled = false;
			chkEsAdmin.IsEnabled = false;
			lvUsuarios.IsEnabled = false;
			MostrarError("Solo un administrador puede gestionar usuarios");
		}

		private void CargarUsuarios()
		{
			Usuarios.Clear();

			foreach (var usuario in DataService.ObtenerUsuarios().OrderBy(u => u.Nombre))
			{
				Usuarios.Add(new UsuarioItemViewModel(usuario));
			}
		}

		private void btnCrearUsuario_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.EsAdminActivo)
			{
				MostrarError("Solo un administrador puede crear usuarios");
				return;
			}

			OcultarMensaje();

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

			if (nombre.Length > 50)
			{
				MostrarError("El nombre no debe exceder 50 caracteres");
				return;
			}

			if (nombre.Contains("  "))
			{
				MostrarError("El nombre no debe contener espacios dobles");
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

			if (correo.Length > 80)
			{
				MostrarError("El correo no debe exceder 80 caracteres");
				return;
			}

			if (correo.Any(char.IsWhiteSpace))
			{
				MostrarError("El correo no debe contener espacios");
				return;
			}

			string patternEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			if (!Regex.IsMatch(correo, patternEmail, RegexOptions.IgnoreCase))
			{
				MostrarError("Ingresa un correo valido");
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

			if (password.Length < 8)
			{
				MostrarError("La contrasena debe contener al menos 8 caracteres");
				return;
			}

			if (password.Length > 20)
			{
				MostrarError("La contrasena no debe exceder 20 caracteres");
				return;
			}

			if (!EsContrasenaFuerte(password))
			{
				MostrarError("La contrasena debe incluir mayuscula, minuscula, numero y caracter especial");
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
				EsAdmin = chkEsAdmin.IsChecked == true,
				Activo = true
			};

			if (!DataService.GuardarUsuario(nuevoUsuario))
			{
				MostrarError("No se pudo crear el usuario");
				return;
			}

			MostrarExito("Usuario creado correctamente");
			LimpiarFormulario();
			CargarUsuarios();
		}

		private void btnCambiarEstado_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.EsAdminActivo)
			{
				MostrarError("Solo un administrador puede cambiar el estado de usuarios");
				return;
			}

			if (sender is not Button button || button.Tag is not UsuarioItemViewModel item)
			{
				return;
			}

			var usuario = item.Usuario;
			if (EsAdminPrincipal(usuario))
			{
				MostrarError("El administrador principal no se puede desactivar");
				return;
			}

			if (EsUsuarioActual(usuario))
			{
				MostrarError("No puedes desactivar tu propia cuenta");
				return;
			}

			if (usuario.EsAdmin && usuario.Activo && DataService.ContarAdministradoresActivos() <= 1)
			{
				MostrarError("Debe existir al menos un administrador activo");
				return;
			}

			usuario.Activo = !usuario.Activo;
			DataService.ActualizarUsuario(usuario);
			MostrarExito(usuario.Activo ? "Usuario activado correctamente" : "Usuario desactivado correctamente");
			CargarUsuarios();
		}

		private void btnCambiarRol_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.EsAdminActivo)
			{
				MostrarError("Solo un administrador puede gestionar roles");
				return;
			}

			if (sender is not Button button || button.Tag is not UsuarioItemViewModel item)
			{
				return;
			}

			var usuario = item.Usuario;
			if (EsAdminPrincipal(usuario))
			{
				MostrarError("El rol del administrador principal no se puede modificar");
				return;
			}

			if (EsUsuarioActual(usuario))
			{
				MostrarError("No puedes cambiar tu propio rol");
				return;
			}

			if (usuario.EsAdmin && DataService.ContarAdministradoresActivos() <= 1)
			{
				MostrarError("Debe existir al menos un administrador activo");
				return;
			}

			usuario.EsAdmin = !usuario.EsAdmin;
			DataService.ActualizarUsuario(usuario);
			MostrarExito(usuario.EsAdmin ? "Rol actualizado a administrador" : "Rol actualizado a usuario");
			CargarUsuarios();
		}

		private static bool EsContrasenaFuerte(string password)
		{
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s])[^\s]{8,20}$";
			return Regex.IsMatch(password, patron);
		}

		private bool EsAdminPrincipal(Usuario usuario)
		{
			return !string.IsNullOrWhiteSpace(usuario.Correo) &&
				usuario.Correo.Equals(CorreoAdminPrincipal, StringComparison.OrdinalIgnoreCase);
		}

		private bool EsUsuarioActual(Usuario usuario)
		{
			return SessionService.UsuarioActivo != null &&
				usuario.Id == SessionService.UsuarioActivo.Id;
		}

		private void LimpiarFormulario()
		{
			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
			chkEsAdmin.IsChecked = false;
		}

		private void MostrarError(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
			txtMensaje.Visibility = Visibility.Visible;
		}

		private void MostrarExito(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
			txtMensaje.Visibility = Visibility.Visible;
		}

		private void OcultarMensaje()
		{
			txtMensaje.Visibility = Visibility.Collapsed;
		}
	}

	public class UsuarioItemViewModel
	{
		public Usuario Usuario { get; }
		public string Nombre => Usuario.Nombre ?? string.Empty;
		public string Correo => Usuario.Correo ?? string.Empty;
		public string RolTexto => Usuario.EsAdmin ? "Rol: Admin" : "Rol: Usuario";
		public string EstadoTexto => Usuario.Activo ? "Activo" : "Inactivo";
		public string AccionEstadoTexto => Usuario.Activo ? "Desactivar" : "Activar";
		public string AccionRolTexto => Usuario.EsAdmin ? "Hacer usuario" : "Hacer admin";

		public UsuarioItemViewModel(Usuario usuario)
		{
			Usuario = usuario;
		}
	}
}
