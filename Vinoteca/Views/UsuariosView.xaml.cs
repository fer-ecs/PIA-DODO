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

		private Usuario? usuarioSeleccionado;

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
			ActualizarModoFormulario();
		}

		private void BloquearAccesoNoAdmin()
		{
			txtNombre.IsEnabled = false;
			txtCorreo.IsEnabled = false;
			txtPassword.IsEnabled = false;
			txtConfirmarPassword.IsEnabled = false;
			chkEsAdmin.IsEnabled = false;
			btnGuardarUsuario.IsEnabled = false;
			btnLimpiarUsuario.IsEnabled = false;
			btnEliminarUsuario.IsEnabled = false;
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

		private void lvUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lvUsuarios.SelectedItem is not UsuarioItemViewModel item)
			{
				return;
			}

			usuarioSeleccionado = item.Usuario;
			txtNombre.Text = item.Usuario.Nombre ?? string.Empty;
			txtCorreo.Text = item.Usuario.Correo ?? string.Empty;
			txtPassword.Password = item.Usuario.Contrasena ?? string.Empty;
			txtConfirmarPassword.Password = item.Usuario.Contrasena ?? string.Empty;
			chkEsAdmin.IsChecked = item.Usuario.EsAdmin;
			ActualizarModoFormulario();
			OcultarMensaje();
		}

		private void btnGuardarUsuario_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.EsAdminActivo)
			{
				MostrarError("Solo un administrador puede guardar usuarios");
				return;
			}

			OcultarMensaje();

			string nombre = txtNombre.Text;
			string correo = txtCorreo.Text;
			string password = txtPassword.Password;
			string confirmarPassword = txtConfirmarPassword.Password;

			if (!ValidarFormulario(nombre, correo, password, confirmarPassword))
			{
				return;
			}

			bool esNuevoUsuario = usuarioSeleccionado == null;
			var usuario = usuarioSeleccionado ?? new Usuario { Id = Guid.NewGuid().ToString() };

			if (ExisteCorreoDuplicado(correo, usuario.Id))
			{
				MostrarError("Ya existe una cuenta registrada con ese correo");
				return;
			}

			if (!esNuevoUsuario && EsAdminPrincipal(usuario) && chkEsAdmin.IsChecked != true)
			{
				MostrarError("El administrador principal debe conservar su rol");
				return;
			}

			usuario.Nombre = nombre.Trim();
			usuario.Correo = correo.Trim();
			usuario.Contrasena = password;
			usuario.EsAdmin = chkEsAdmin.IsChecked == true;
			usuario.Activo = usuarioSeleccionado?.Activo ?? true;

			if (esNuevoUsuario)
			{
				if (!DataService.GuardarUsuario(usuario))
				{
					MostrarError("No se pudo crear el usuario");
					return;
				}

				MostrarExito("Usuario creado correctamente");
			}
			else
			{
				DataService.ActualizarUsuario(usuario);
				MostrarExito("Usuario actualizado correctamente");
			}

			LimpiarFormulario();
			CargarUsuarios();
		}

		private bool ValidarFormulario(string nombre, string correo, string password, string confirmarPassword)
		{
			if (string.IsNullOrWhiteSpace(nombre))
			{
				MostrarError("El nombre es obligatorio");
				return false;
			}

			if (nombre != nombre.Trim())
			{
				MostrarError("El nombre no debe tener espacios al inicio o al final");
				return false;
			}

			if (nombre.Length < 3 || nombre.Length > 50)
			{
				MostrarError("El nombre debe tener entre 3 y 50 caracteres");
				return false;
			}

			if (nombre.Contains("  "))
			{
				MostrarError("El nombre no debe contener espacios dobles");
				return false;
			}

			if (nombre.Any(char.IsDigit))
			{
				MostrarError("El nombre no debe contener numeros");
				return false;
			}

			if (string.IsNullOrWhiteSpace(correo))
			{
				MostrarError("El correo es obligatorio");
				return false;
			}

			if (correo != correo.Trim())
			{
				MostrarError("El correo no debe tener espacios al inicio o al final");
				return false;
			}

			if (correo.Length > 80 || correo.Any(char.IsWhiteSpace))
			{
				MostrarError("Ingresa un correo valido sin espacios y de maximo 80 caracteres");
				return false;
			}

			string patternEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			if (!Regex.IsMatch(correo, patternEmail, RegexOptions.IgnoreCase))
			{
				MostrarError("Ingresa un correo valido");
				return false;
			}

			if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmarPassword))
			{
				MostrarError("Debes escribir y confirmar la contrasena");
				return false;
			}

			if (password != password.Trim() || confirmarPassword != confirmarPassword.Trim())
			{
				MostrarError("Las contrasenas no deben tener espacios al inicio o al final");
				return false;
			}

			if (password.Any(char.IsWhiteSpace) || confirmarPassword.Any(char.IsWhiteSpace))
			{
				MostrarError("Las contrasenas no deben contener espacios");
				return false;
			}

			if (password.Length < 8 || password.Length > 20)
			{
				MostrarError("La contrasena debe tener entre 8 y 20 caracteres");
				return false;
			}

			if (!EsContrasenaFuerte(password))
			{
				MostrarError("La contrasena debe incluir mayuscula, minuscula, numero y caracter especial");
				return false;
			}

			if (password != confirmarPassword)
			{
				MostrarError("Las contrasenas no coinciden");
				return false;
			}

			return true;
		}

		private bool ExisteCorreoDuplicado(string correo, string idActual)
		{
			return DataService.ObtenerUsuarios().Any(u =>
				u.Id != idActual &&
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));
		}

		private void btnEliminarUsuario_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.EsAdminActivo)
			{
				MostrarError("Solo un administrador puede eliminar usuarios");
				return;
			}

			if (usuarioSeleccionado == null)
			{
				MostrarError("Selecciona un usuario para eliminar");
				return;
			}

			if (EsAdminPrincipal(usuarioSeleccionado))
			{
				MostrarError("El administrador principal no se puede eliminar");
				return;
			}

			if (EsUsuarioActual(usuarioSeleccionado))
			{
				MostrarError("No puedes eliminar tu propia cuenta");
				return;
			}

			if (usuarioSeleccionado.EsAdmin && usuarioSeleccionado.Activo && DataService.ContarAdministradoresActivos() <= 1)
			{
				MostrarError("Debe existir al menos un administrador activo");
				return;
			}

			if (!DataService.EliminarUsuario(usuarioSeleccionado.Id))
			{
				MostrarError("No se pudo eliminar el usuario");
				return;
			}

			MostrarExito("Usuario eliminado correctamente");
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

			if (usuarioSeleccionado?.Id == usuario.Id)
			{
				usuarioSeleccionado = usuario;
			}

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

			if (usuarioSeleccionado?.Id == usuario.Id)
			{
				usuarioSeleccionado = usuario;
				chkEsAdmin.IsChecked = usuario.EsAdmin;
			}

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

		private void btnLimpiarUsuario_Click(object sender, RoutedEventArgs e)
		{
			LimpiarFormulario();
			OcultarMensaje();
		}

		private void LimpiarFormulario()
		{
			usuarioSeleccionado = null;
			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
			chkEsAdmin.IsChecked = false;
			lvUsuarios.SelectedItem = null;
			ActualizarModoFormulario();
		}

		private void ActualizarModoFormulario()
		{
			bool edicion = usuarioSeleccionado != null;
			txtModoFormulario.Text = edicion ? "Editar usuario seleccionado" : "Nuevo usuario";
			btnGuardarUsuario.Content = edicion ? "Guardar cambios" : "Guardar usuario";
			btnEliminarUsuario.IsEnabled = edicion;
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
