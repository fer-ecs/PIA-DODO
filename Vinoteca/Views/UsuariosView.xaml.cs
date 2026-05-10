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
	public sealed partial class UsuariosView : Page, ICambiosPendientes
	{
		private const string CorreoAdminPrincipal = "admin@vinoteca.com";

		private Usuario? usuarioSeleccionado;
		private bool ignorarCambioSeleccion;

		public ObservableCollection<UsuarioItemViewModel> Usuarios { get; } = new();

		public UsuariosView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			InputRestrictionsHelper.AplicarSoloLetrasConEspacios(txtNombre);
			lvUsuarios.ItemsSource = Usuarios;
			ConfigurarRoles();

			if (!SessionService.PuedeVerInformacionOperativa)
			{
				BloquearAccesoNoOperativo();
				return;
			}

			if (!SessionService.PuedeGestionarUsuarios)
			{
				ConfigurarModoSoloLectura();
			}

			CargarUsuarios();
			ActualizarModoFormulario();
		}

		public bool TieneCambiosPendientes => SessionService.PuedeGestionarUsuarios && FormularioTieneCambios();

		public string ObtenerMensajeCambiosPendientes()
		{
			return usuarioSeleccionado == null
				? "Hay un usuario nuevo sin guardar"
				: "Hay cambios sin guardar en el usuario seleccionado";
		}

		private void ConfigurarRoles()
		{
			cmbRol.SelectedIndex = 2;
			chkActivo.IsChecked = true;
		}

		private void BloquearAccesoNoOperativo()
		{
			txtNombre.IsEnabled = false;
			txtCorreo.IsEnabled = false;
			txtPassword.IsEnabled = false;
			txtConfirmarPassword.IsEnabled = false;
			cmbRol.IsEnabled = false;
			chkActivo.IsEnabled = false;
			btnGuardarUsuario.IsEnabled = false;
			btnLimpiarUsuario.IsEnabled = false;
			btnEliminarUsuario.IsEnabled = false;
			lvUsuarios.IsEnabled = false;
			MostrarError("Solo personal autorizado puede revisar usuarios");
		}

		private void ConfigurarModoSoloLectura()
		{
			txtDescripcionFormulario.Text = "Vista de solo lectura para supervision de cuentas.";
			txtDescripcionLista.Text = "Selecciona una cuenta para revisar su informacion.";
			txtNombre.IsEnabled = false;
			txtCorreo.IsEnabled = false;
			txtPassword.IsEnabled = false;
			txtConfirmarPassword.IsEnabled = false;
			cmbRol.IsEnabled = false;
			chkActivo.IsEnabled = false;
			btnGuardarUsuario.IsEnabled = false;
			btnLimpiarUsuario.IsEnabled = false;
			btnEliminarUsuario.IsEnabled = false;
		}

		private void CargarUsuarios()
		{
			Usuarios.Clear();

			foreach (var usuario in DataService.ObtenerUsuarios().OrderBy(u => u.Nombre))
			{
				Usuarios.Add(new UsuarioItemViewModel(usuario));
			}
		}

		private async void lvUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ignorarCambioSeleccion)
			{
				return;
			}

			if (lvUsuarios.SelectedItem is not UsuarioItemViewModel item)
			{
				return;
			}

			if (usuarioSeleccionado?.Id == item.Usuario.Id)
			{
				return;
			}

			if (TieneCambiosPendientes)
			{
				bool puedeCambiar = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
					XamlRoot,
					this,
					"cambiar de usuario",
					false);
				if (!puedeCambiar)
				{
					RestaurarSeleccionAnterior();
					return;
				}
			}

			CargarUsuarioEnFormulario(item.Usuario);
			ActualizarModoFormulario();
			OcultarMensaje();
		}

		private void btnGuardarUsuario_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarUsuarios)
			{
				MostrarError("Solo el administrador puede guardar usuarios");
				return;
			}

			OcultarMensaje();

			string nombre = txtNombre.Text;
			string correo = txtCorreo.Text;
			string password = txtPassword.Password;
			string confirmarPassword = txtConfirmarPassword.Password;
			string rol = ObtenerRolActual();

			if (!ValidarFormulario(nombre, correo, password, confirmarPassword, rol))
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

			if (!esNuevoUsuario && EsAdminPrincipal(usuario) && rol != RolesSistema.Administrador)
			{
				MostrarError("El administrador principal debe conservar su rol");
				return;
			}

			if (!esNuevoUsuario && EsAdminPrincipal(usuario) && chkActivo.IsChecked != true)
			{
				MostrarError("El administrador principal debe permanecer activo");
				return;
			}

			usuario.Nombre = nombre.Trim();
			usuario.Correo = correo.Trim();
			usuario.Contrasena = password;
			usuario.Rol = RolesSistema.Normalizar(rol);
			usuario.Activo = chkActivo.IsChecked == true;

			if (usuario.Rol == RolesSistema.Administrador && !usuario.Activo)
			{
				MostrarError("Un administrador no puede quedar inactivo desde el formulario");
				return;
			}

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

			LimpiarFormularioInterno();
			CargarUsuarios();
		}

		private bool ValidarFormulario(string nombre, string correo, string password, string confirmarPassword, string rol)
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

			if (!FormValidationHelper.EsTextoConLetrasYEspacios(nombre))
			{
				MostrarError("El nombre solo debe contener letras y espacios entre palabras");
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

			if (!FormValidationHelper.EsCorreoValido(correo))
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

			if (string.IsNullOrWhiteSpace(rol))
			{
				MostrarError("Selecciona un rol");
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

		private async void btnEliminarUsuario_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarUsuarios)
			{
				MostrarError("Solo el administrador puede eliminar usuarios");
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

			if (usuarioSeleccionado.Rol == RolesSistema.Administrador && usuarioSeleccionado.Activo && DataService.ContarAdministradoresActivos() <= 1)
			{
				MostrarError("Debe existir al menos un administrador activo");
				return;
			}

			bool confirmarEliminacion = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Eliminar usuario",
				"Deseas eliminar el usuario seleccionado?",
				"Eliminar");
			if (!confirmarEliminacion)
			{
				return;
			}

			if (!DataService.EliminarUsuario(usuarioSeleccionado.Id))
			{
				MostrarError("No se pudo eliminar el usuario");
				return;
			}

			MostrarExito("Usuario eliminado correctamente");
			LimpiarFormularioInterno();
			CargarUsuarios();
		}

		private void btnCambiarEstado_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarUsuarios)
			{
				MostrarError("Solo el administrador puede cambiar el estado de usuarios");
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

			if (usuario.Rol == RolesSistema.Administrador && usuario.Activo && DataService.ContarAdministradoresActivos() <= 1)
			{
				MostrarError("Debe existir al menos un administrador activo");
				return;
			}

			usuario.Activo = !usuario.Activo;
			DataService.ActualizarUsuario(usuario);
			MostrarExito(usuario.Activo ? "Usuario activado correctamente" : "Usuario desactivado correctamente");

			if (usuarioSeleccionado?.Id == usuario.Id)
			{
				CargarUsuarioEnFormulario(usuario);
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

		private async void btnLimpiarUsuario_Click(object sender, RoutedEventArgs e)
		{
			if (TieneCambiosPendientes)
			{
				bool puedeLimpiar = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
					XamlRoot,
					this,
					"limpiar el formulario",
					false);
				if (!puedeLimpiar)
				{
					return;
				}
			}

			LimpiarFormularioInterno();
			OcultarMensaje();
		}

		private void LimpiarFormularioInterno()
		{
			usuarioSeleccionado = null;
			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
			SeleccionarRol(RolesSistema.Cliente);
			chkActivo.IsChecked = true;

			ignorarCambioSeleccion = true;
			lvUsuarios.SelectedItem = null;
			ignorarCambioSeleccion = false;

			ActualizarModoFormulario();
		}

		private void CargarUsuarioEnFormulario(Usuario usuario)
		{
			usuarioSeleccionado = usuario;
			txtNombre.Text = usuario.Nombre ?? string.Empty;
			txtCorreo.Text = usuario.Correo ?? string.Empty;
			txtPassword.Password = usuario.Contrasena ?? string.Empty;
			txtConfirmarPassword.Password = usuario.Contrasena ?? string.Empty;
			SeleccionarRol(usuario.Rol);
			chkActivo.IsChecked = usuario.Activo;
		}

		private void RestaurarSeleccionAnterior()
		{
			ignorarCambioSeleccion = true;
			lvUsuarios.SelectedItem = usuarioSeleccionado == null
				? null
				: Usuarios.FirstOrDefault(u => u.Usuario.Id == usuarioSeleccionado.Id);
			ignorarCambioSeleccion = false;
		}

		private bool FormularioTieneCambios()
		{
			if (usuarioSeleccionado == null)
			{
				return !FormularioVacio();
			}

			return !FormularioCoincideConUsuario(usuarioSeleccionado);
		}

		private bool FormularioVacio()
		{
			return string.IsNullOrWhiteSpace(txtNombre.Text) &&
				string.IsNullOrWhiteSpace(txtCorreo.Text) &&
				string.IsNullOrWhiteSpace(txtPassword.Password) &&
				string.IsNullOrWhiteSpace(txtConfirmarPassword.Password) &&
				ObtenerRolActual() == RolesSistema.Cliente &&
				chkActivo.IsChecked == true;
		}

		private bool FormularioCoincideConUsuario(Usuario usuario)
		{
			return string.Equals((txtNombre.Text ?? string.Empty).Trim(), usuario.Nombre ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals((txtCorreo.Text ?? string.Empty).Trim(), usuario.Correo ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals(txtPassword.Password, usuario.Contrasena ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals(txtConfirmarPassword.Password, usuario.Contrasena ?? string.Empty, StringComparison.Ordinal) &&
				ObtenerRolActual() == RolesSistema.Normalizar(usuario.Rol) &&
				chkActivo.IsChecked == usuario.Activo;
		}

		private string ObtenerRolActual()
		{
			return (cmbRol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? RolesSistema.Cliente;
		}

		private void SeleccionarRol(string rol)
		{
			string rolNormalizado = RolesSistema.Normalizar(rol);
			foreach (ComboBoxItem item in cmbRol.Items)
			{
				if (item.Content?.ToString() == rolNormalizado)
				{
					cmbRol.SelectedItem = item;
					return;
				}
			}

			cmbRol.SelectedIndex = 2;
		}

		private void ActualizarModoFormulario()
		{
			bool edicion = usuarioSeleccionado != null;
			txtModoFormulario.Text = edicion ? "Editar usuario seleccionado" : "Nuevo usuario";
			btnGuardarUsuario.Content = edicion ? "Guardar cambios" : "Guardar usuario";
			btnEliminarUsuario.IsEnabled = edicion && SessionService.PuedeGestionarUsuarios;
		}

		private void MostrarError(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = (SolidColorBrush)Application.Current.Resources["WineDangerBrush"];
			txtMensaje.Visibility = Visibility.Visible;
		}

		private void MostrarExito(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = (SolidColorBrush)Application.Current.Resources["WineSuccessBrush"];
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
		public string RolTexto => $"Rol: {RolesSistema.Normalizar(Usuario.Rol)}";
		public string EstadoTexto => Usuario.Activo ? "Activo" : "Inactivo";
		public string AccionEstadoTexto => Usuario.Activo ? "Desactivar" : "Activar";

		public UsuarioItemViewModel(Usuario usuario)
		{
			Usuario = usuario;
		}
	}
}
