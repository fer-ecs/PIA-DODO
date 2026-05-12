using System;
using System.Collections.Generic;
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
	// esta seccion sirve para agrupar la administracion de cuentas y dejar esa responsabilidad en un solo archivo - UsuariosView
	public sealed partial class UsuariosView : Page, ICambiosPendientes, IDescartaCambiosPendientes
	{
		private const string CorreoAdminPrincipal = "admin@vinoteca.com";
		private const string CachePrefixUsuarioNuevo = "Usuarios_Nuevo_";
		private const string CacheNombre = "Usuarios_Nuevo_Nombre";
		private const string CacheCorreo = "Usuarios_Nuevo_Correo";
		private const string CacheDominio = "Usuarios_Nuevo_Dominio";
		private const string CachePassword = "Usuarios_Nuevo_Password";
		private const string CacheConfirmar = "Usuarios_Nuevo_Confirmar";
		private const string CacheRol = "Usuarios_Nuevo_Rol";
		private const string CacheActivo = "Usuarios_Nuevo_Activo";

		private Usuario? usuarioSeleccionado;
		private bool ignorarCambioSeleccion;
		private bool cargandoCacheUsuario;
		private List<Usuario> todosLosUsuarios = new();
		private List<string> dominiosCorreoPermitidos = new();

		public ObservableCollection<UsuarioItemViewModel> Usuarios { get; } = new();

		// esta seccion sirve para agrupar la administracion de cuentas y dejar esa responsabilidad en un solo archivo - UsuariosView
		public UsuariosView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			InputRestrictionsHelper.AplicarSoloLetrasConEspacios(txtNombre);
			InputRestrictionsHelper.AplicarCorreoLocal(txtCorreo);
			InputRestrictionsHelper.AplicarDominioCorreo(txtNuevoDominio);
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtBuscarUsuario);
			lvUsuarios.ItemsSource = Usuarios;
			ConfigurarRoles();
			ConfigurarDominiosCorreo();
			ConfigurarFiltros();

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
			ConfigurarCacheFormulario();
			CargarFormularioNuevoDesdeCache();
		}

		public bool TieneCambiosPendientes => SessionService.PuedeGestionarUsuarios && FormularioTieneCambios();

		// esta seccion sirve para leer informacion de la administracion de cuentas y regresarla lista para usarse - ObtenerMensajeCambiosPendientes
		public string ObtenerMensajeCambiosPendientes()
		{
			return usuarioSeleccionado == null
				? "Hay un usuario nuevo sin guardar"
				: "Hay cambios sin guardar en el usuario seleccionado";
		}

		public void DescartarCambiosPendientes()
		{
			LimpiarCacheFormularioNuevo();
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - ConfigurarRoles
		private void ConfigurarRoles()
		{
			cmbRol.SelectedIndex = 2;
			chkActivo.IsChecked = true;
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - BloquearAccesoNoOperativo
		private void BloquearAccesoNoOperativo()
		{
			txtNombre.IsEnabled = false;
			txtCorreo.IsEnabled = false;
			cmbDominioCorreo.IsEnabled = false;
			txtNuevoDominio.IsEnabled = false;
			btnAgregarDominio.IsEnabled = false;
			btnEliminarDominio.IsEnabled = false;
			txtPassword.IsEnabled = false;
			txtConfirmarPassword.IsEnabled = false;
			cmbRol.IsEnabled = false;
			chkActivo.IsEnabled = false;
			btnGuardarUsuario.IsEnabled = false;
			btnLimpiarUsuario.IsEnabled = false;
			btnEliminarUsuario.IsEnabled = false;
			lvUsuarios.IsEnabled = false;
			txtBuscarUsuario.IsEnabled = false;
			cmbFiltroRol.IsEnabled = false;
			cmbFiltroEstado.IsEnabled = false;
			cmbOrdenUsuarios.IsEnabled = false;
			MostrarError("Solo personal autorizado puede revisar usuarios");
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - ConfigurarModoSoloLectura
		private void ConfigurarModoSoloLectura()
		{
			txtDescripcionFormulario.Text = "Vista de solo lectura para supervision de cuentas";
			txtDescripcionLista.Text = "Selecciona una cuenta para revisar su informacion";
			txtNombre.IsEnabled = false;
			txtCorreo.IsEnabled = false;
			cmbDominioCorreo.IsEnabled = false;
			txtNuevoDominio.IsEnabled = false;
			btnAgregarDominio.IsEnabled = false;
			btnEliminarDominio.IsEnabled = false;
			txtPassword.IsEnabled = false;
			txtConfirmarPassword.IsEnabled = false;
			cmbRol.IsEnabled = false;
			chkActivo.IsEnabled = false;
			btnGuardarUsuario.IsEnabled = false;
			btnLimpiarUsuario.IsEnabled = false;
			btnEliminarUsuario.IsEnabled = false;
		}

		// esta seccion sirve para cargar informacion de la administracion de cuentas y preparar lo que se muestra en pantalla - CargarUsuarios
		private void CargarUsuarios()
		{
			todosLosUsuarios = DataService.ObtenerUsuarios().ToList();
			AplicarFiltroUsuarios();
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - ConfigurarFiltros
		private void ConfigurarFiltros()
		{
			cmbFiltroRol.SelectedIndex = 0;
			cmbFiltroEstado.SelectedIndex = 0;
			cmbOrdenUsuarios.SelectedIndex = 0;
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - ConfigurarDominiosCorreo
		private void ConfigurarDominiosCorreo()
		{
			dominiosCorreoPermitidos = DataService.ObtenerDominiosCorreo();
			cmbDominioCorreo.Items.Clear();
			foreach (string dominio in dominiosCorreoPermitidos)
			{
				cmbDominioCorreo.Items.Add(dominio);
			}

			cmbDominioCorreo.SelectedIndex = cmbDominioCorreo.Items.Count > 0 ? 0 : -1;
		}

		// esta seccion sirve para ordenar y ajustar datos de la administracion de cuentas para trabajar con valores limpios - AplicarFiltroUsuarios
		private void AplicarFiltroUsuarios()
		{
			string busqueda = txtBuscarUsuario.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			string rol = ObtenerContenidoCombo(cmbFiltroRol);
			string estado = ObtenerContenidoCombo(cmbFiltroEstado);

			var filtrados = todosLosUsuarios.Where(u =>
				(string.IsNullOrWhiteSpace(busqueda) ||
				(u.Id?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(u.Nombre?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(u.Correo?.ToLowerInvariant().Contains(busqueda) ?? false)) &&
				(rol == "Todos" || RolesSistema.Normalizar(u.Rol) == rol) &&
				(estado == "Todos" || (estado == "Activos" && u.Activo) || (estado == "Inactivos" && !u.Activo)));

			filtrados = ObtenerContenidoCombo(cmbOrdenUsuarios) switch
			{
				"Nombre Z-A" => filtrados.OrderByDescending(u => u.Nombre),
				"Rol" => filtrados.OrderBy(u => RolesSistema.Normalizar(u.Rol)).ThenBy(u => u.Nombre),
				"Estado" => filtrados.OrderByDescending(u => u.Activo).ThenBy(u => u.Nombre),
				"ID" => filtrados.OrderBy(u => ObtenerIdNumerico(u.Id)),
				_ => filtrados.OrderBy(u => u.Nombre)
			};

			Usuarios.Clear();
			foreach (var usuario in filtrados.Select(u => new UsuarioItemViewModel(u)))
			{
				Usuarios.Add(usuario);
			}

			txtResumenUsuarios.Text = $"{Usuarios.Count} de {todosLosUsuarios.Count} cuentas";
		}

		// esta seccion sirve para leer informacion de la administracion de cuentas y regresarla lista para usarse - ObtenerContenidoCombo
		private static string ObtenerContenidoCombo(ComboBox combo)
		{
			return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
		}

		// esta seccion sirve para leer informacion de la administracion de cuentas y regresarla lista para usarse - ObtenerIdNumerico
		private static int ObtenerIdNumerico(string? id)
		{
			return int.TryParse(id, out int valor) ? valor : int.MaxValue;
		}

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - lvUsuarios_SelectionChanged
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

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - btnGuardarUsuario_Click
		private void btnGuardarUsuario_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarUsuarios)
			{
				MostrarError("Solo el administrador puede guardar usuarios");
				return;
			}

			OcultarMensaje();

			string nombre = txtNombre.Text;
			string correoLocal = txtCorreo.Text;
			string dominioCorreo = ObtenerDominioCorreoActual();
			string password = txtPassword.Password;
			string confirmarPassword = txtConfirmarPassword.Password;
			string rol = ObtenerRolActual();
			bool esNuevoUsuario = usuarioSeleccionado == null;
			string correo = esNuevoUsuario
				? ConstruirCorreo(correoLocal, dominioCorreo)
				: usuarioSeleccionado?.Correo ?? string.Empty;

			if (!ValidarFormulario(nombre, correoLocal, dominioCorreo, correo, password, confirmarPassword, rol, esNuevoUsuario))
			{
				return;
			}

			var usuario = usuarioSeleccionado ?? new Usuario();

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
			if (esNuevoUsuario)
			{
				usuario.Correo = correo.Trim();
			}

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

		// esta seccion sirve para revisar reglas de la administracion de cuentas y evitar que pase un dato incorrecto - ValidarFormulario
		private bool ValidarFormulario(string nombre, string correoLocal, string dominioCorreo, string correo, string password, string confirmarPassword, string rol, bool validarCorreo)
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

			if (validarCorreo && string.IsNullOrWhiteSpace(correoLocal))
			{
				MostrarError("El nombre del correo es obligatorio");
				return false;
			}

			if (validarCorreo && correoLocal != correoLocal.Trim())
			{
				MostrarError("El nombre del correo no debe tener espacios al inicio o al final");
				return false;
			}

			if (validarCorreo && correoLocal.Contains('@'))
			{
				MostrarError("Escribe solo el nombre del correo, sin @");
				return false;
			}

			if (validarCorreo && (correoLocal.Length > 40 || correoLocal.Any(char.IsWhiteSpace)))
			{
				MostrarError("El nombre del correo no debe tener espacios y debe ser maximo 40 caracteres");
				return false;
			}

			if (validarCorreo && !EsNombreCorreoValido(correoLocal))
			{
				MostrarError("El nombre del correo solo permite letras, numeros, punto, guion y guion bajo");
				return false;
			}

			if (validarCorreo && !dominiosCorreoPermitidos.Any(d => d.Equals(dominioCorreo, StringComparison.OrdinalIgnoreCase)))
			{
				MostrarError("Selecciona un dominio de correo valido");
				return false;
			}

			if (validarCorreo && (correo.Length > 80 || !FormValidationHelper.EsCorreoValido(correo)))
			{
				MostrarError("Ingresa un correo valido");
				return false;
			}

			if (!validarCorreo && string.IsNullOrWhiteSpace(correo))
			{
				MostrarError("El usuario seleccionado no tiene un correo valido");
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

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - ExisteCorreoDuplicado
		private bool ExisteCorreoDuplicado(string correo, string idActual)
		{
			return DataService.ObtenerUsuarios().Any(u =>
				u.Id != idActual &&
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));
		}

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - btnEliminarUsuario_Click
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

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - btnCambiarEstado_Click
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

		// esta seccion sirve para revisar reglas de la administracion de cuentas y evitar que pase un dato incorrecto - EsContrasenaFuerte
		private static bool EsContrasenaFuerte(string password)
		{
			string patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s])[^\s]{8,20}$";
			return Regex.IsMatch(password, patron);
		}

		// esta seccion sirve para revisar reglas de la administracion de cuentas y evitar que pase un dato incorrecto - EsAdminPrincipal
		private bool EsAdminPrincipal(Usuario usuario)
		{
			return !string.IsNullOrWhiteSpace(usuario.Correo) &&
				usuario.Correo.Equals(CorreoAdminPrincipal, StringComparison.OrdinalIgnoreCase);
		}

		// esta seccion sirve para revisar reglas de la administracion de cuentas y evitar que pase un dato incorrecto - EsUsuarioActual
		private bool EsUsuarioActual(Usuario usuario)
		{
			return SessionService.UsuarioActivo != null &&
				usuario.Id == SessionService.UsuarioActivo.Id;
		}

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - btnLimpiarUsuario_Click
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

		// esta seccion sirve para quitar informacion de la administracion de cuentas y dejar el estado consistente - LimpiarFormularioInterno
		private void LimpiarFormularioInterno()
		{
			usuarioSeleccionado = null;
			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
			SeleccionarRol(RolesSistema.Empleado);
			chkActivo.IsChecked = true;
			SeleccionarDominioCorreo(dominiosCorreoPermitidos.FirstOrDefault() ?? "gmail.com");

			ignorarCambioSeleccion = true;
			lvUsuarios.SelectedItem = null;
			ignorarCambioSeleccion = false;

			ActualizarModoFormulario();
			LimpiarCacheFormularioNuevo();
		}

		// esta seccion sirve para cargar informacion de la administracion de cuentas y preparar lo que se muestra en pantalla - CargarUsuarioEnFormulario
		private void CargarUsuarioEnFormulario(Usuario usuario)
		{
			LimpiarCacheFormularioNuevo();
			usuarioSeleccionado = usuario;
			txtNombre.Text = usuario.Nombre ?? string.Empty;
			SepararCorreo(usuario.Correo ?? string.Empty, out string nombreCorreo, out string dominioCorreo);
			txtCorreo.Text = nombreCorreo;
			SeleccionarDominioCorreo(dominioCorreo);
			txtPassword.Password = usuario.Contrasena ?? string.Empty;
			txtConfirmarPassword.Password = usuario.Contrasena ?? string.Empty;
			SeleccionarRol(usuario.Rol);
			chkActivo.IsChecked = usuario.Activo;
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - RestaurarSeleccionAnterior
		private void RestaurarSeleccionAnterior()
		{
			ignorarCambioSeleccion = true;
			lvUsuarios.SelectedItem = usuarioSeleccionado == null
				? null
				: Usuarios.FirstOrDefault(u => u.Usuario.Id == usuarioSeleccionado.Id);
			ignorarCambioSeleccion = false;
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - FormularioTieneCambios
		private bool FormularioTieneCambios()
		{
			if (usuarioSeleccionado == null)
			{
				return !FormularioVacio();
			}

			return !FormularioCoincideConUsuario(usuarioSeleccionado);
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - FormularioVacio
		private bool FormularioVacio()
		{
			return string.IsNullOrWhiteSpace(txtNombre.Text) &&
				string.IsNullOrWhiteSpace(txtCorreo.Text) &&
				string.IsNullOrWhiteSpace(txtPassword.Password) &&
				string.IsNullOrWhiteSpace(txtConfirmarPassword.Password) &&
				ObtenerRolActual() == RolesSistema.Empleado &&
				chkActivo.IsChecked == true;
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - FormularioCoincideConUsuario
		private bool FormularioCoincideConUsuario(Usuario usuario)
		{
			return string.Equals((txtNombre.Text ?? string.Empty).Trim(), usuario.Nombre ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals(ConstruirCorreo(txtCorreo.Text, ObtenerDominioCorreoActual()), usuario.Correo ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
				string.Equals(txtPassword.Password, usuario.Contrasena ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals(txtConfirmarPassword.Password, usuario.Contrasena ?? string.Empty, StringComparison.Ordinal) &&
				ObtenerRolActual() == RolesSistema.Normalizar(usuario.Rol) &&
				chkActivo.IsChecked == usuario.Activo;
		}

		// esta seccion sirve para leer informacion de la administracion de cuentas y regresarla lista para usarse - ObtenerRolActual
		private string ObtenerRolActual()
		{
			return (cmbRol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? RolesSistema.Empleado;
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - SeleccionarRol
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

		// esta seccion sirve para leer informacion de la administracion de cuentas y regresarla lista para usarse - ObtenerDominioCorreoActual
		private string ObtenerDominioCorreoActual()
		{
			return cmbDominioCorreo.SelectedItem?.ToString() ?? dominiosCorreoPermitidos.FirstOrDefault() ?? "gmail.com";
		}

		// esta seccion sirve para armar datos o contenido de la administracion de cuentas y devolverlo ya preparado - ConstruirCorreo
		private static string ConstruirCorreo(string correoLocal, string dominioCorreo)
		{
			return $"{(correoLocal ?? string.Empty).Trim()}@{(dominioCorreo ?? string.Empty).Trim()}";
		}

		// esta seccion sirve para revisar reglas de la administracion de cuentas y evitar que pase un dato incorrecto - EsNombreCorreoValido
		private static bool EsNombreCorreoValido(string correoLocal)
		{
			string nombre = correoLocal.Trim();
			return !nombre.Contains("..") &&
				Regex.IsMatch(nombre, @"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,38}[A-Za-z0-9])?$");
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - SepararCorreo
		private void SepararCorreo(string correo, out string correoLocal, out string dominioCorreo)
		{
			correoLocal = string.Empty;
			dominioCorreo = dominiosCorreoPermitidos.FirstOrDefault() ?? "gmail.com";

			int indiceArroba = correo.IndexOf('@');
			if (indiceArroba <= 0 || indiceArroba >= correo.Length - 1)
			{
				return;
			}

			correoLocal = correo[..indiceArroba];
			dominioCorreo = correo[(indiceArroba + 1)..];
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - SeleccionarDominioCorreo
		private void SeleccionarDominioCorreo(string dominio)
		{
			string dominioNormalizado = dominiosCorreoPermitidos
				.FirstOrDefault(d => d.Equals(dominio, StringComparison.OrdinalIgnoreCase)) ?? dominiosCorreoPermitidos.FirstOrDefault() ?? "gmail.com";
			cmbDominioCorreo.SelectedItem = dominioNormalizado;
		}

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - btnAgregarDominio_Click
		private void btnAgregarDominio_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarUsuarios)
			{
				MostrarError("Solo el administrador puede agregar dominios");
				return;
			}

			string dominio = txtNuevoDominio.Text?.Trim().TrimStart('@').ToLowerInvariant() ?? string.Empty;
			if (!DataService.EsDominioCorreoValido(dominio))
			{
				MostrarError("Ingresa un dominio aceptado, sin repetir extensiones, por ejemplo empresa.com o empresa.com.mx");
				ReiniciarEntradaDominioPersonalizado();
				return;
			}

			if (!DataService.GuardarDominioCorreo(dominio))
			{
				MostrarError("Ese dominio ya existe o no esta permitido");
				ReiniciarEntradaDominioPersonalizado();
				return;
			}

			txtNuevoDominio.Text = string.Empty;
			ConfigurarDominiosCorreo();
			SeleccionarDominioCorreo(dominio);
			MostrarExito("Dominio agregado correctamente");
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - ReiniciarEntradaDominioPersonalizado
		private void ReiniciarEntradaDominioPersonalizado()
		{
			txtNuevoDominio.Text = string.Empty;
			txtNuevoDominio.Focus(FocusState.Programmatic);
		}

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - btnEliminarDominio_Click
		private void btnEliminarDominio_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarUsuarios)
			{
				MostrarError("Solo el administrador puede eliminar dominios");
				return;
			}

			string dominio = ObtenerDominioCorreoActual();
			if (DataService.DominioCorreoEnUso(dominio))
			{
				MostrarError("No se puede eliminar un dominio usado por cuentas existentes");
				return;
			}

			if (!DataService.EliminarDominioCorreo(dominio))
			{
				MostrarError("No se pudo eliminar el dominio seleccionado");
				return;
			}

			ConfigurarDominiosCorreo();
			MostrarExito("Dominio eliminado correctamente");
		}

		// esta seccion sirve para actualizar la administracion de cuentas despues de un cambio y sincronizar la pantalla - ActualizarModoFormulario
		private void ActualizarModoFormulario()
		{
			bool edicion = usuarioSeleccionado != null;
			txtModoFormulario.Text = edicion ? "Editar usuario seleccionado" : "Nuevo usuario";
			btnGuardarUsuario.Content = edicion ? "Guardar cambios" : "Guardar usuario";
			btnEliminarUsuario.IsEnabled = edicion && SessionService.PuedeGestionarUsuarios;
			if (SessionService.PuedeGestionarUsuarios)
			{
				txtCorreo.IsEnabled = !edicion;
				cmbDominioCorreo.IsEnabled = !edicion;
			}
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - ConfigurarCacheFormulario
		private void ConfigurarCacheFormulario()
		{
			txtNombre.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtCorreo.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
			cmbDominioCorreo.SelectionChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtPassword.PasswordChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtConfirmarPassword.PasswordChanged += (s, e) => GuardarFormularioNuevoEnCache();
			cmbRol.SelectionChanged += (s, e) => GuardarFormularioNuevoEnCache();
			chkActivo.Checked += (s, e) => GuardarFormularioNuevoEnCache();
			chkActivo.Unchecked += (s, e) => GuardarFormularioNuevoEnCache();
		}

		// esta seccion sirve para cargar informacion de la administracion de cuentas y preparar lo que se muestra en pantalla - CargarFormularioNuevoDesdeCache
		private void CargarFormularioNuevoDesdeCache()
		{
			if (!SessionService.PuedeGestionarUsuarios || usuarioSeleccionado != null)
			{
				return;
			}

			cargandoCacheUsuario = true;
			txtNombre.Text = App.FormCacheService.GetValue(CacheNombre) ?? txtNombre.Text;
			txtCorreo.Text = App.FormCacheService.GetValue(CacheCorreo) ?? txtCorreo.Text;
			SeleccionarDominioCorreo(App.FormCacheService.GetValue(CacheDominio) ?? ObtenerDominioCorreoActual());
			txtPassword.Password = App.FormCacheService.GetValue(CachePassword) ?? txtPassword.Password;
			txtConfirmarPassword.Password = App.FormCacheService.GetValue(CacheConfirmar) ?? txtConfirmarPassword.Password;
			SeleccionarRol(App.FormCacheService.GetValue(CacheRol) ?? ObtenerRolActual());
			if (bool.TryParse(App.FormCacheService.GetValue(CacheActivo), out bool activo))
			{
				chkActivo.IsChecked = activo;
			}
			cargandoCacheUsuario = false;
		}

		// esta seccion sirve para guardar informacion de la administracion de cuentas y mantener los datos persistidos - GuardarFormularioNuevoEnCache
		private void GuardarFormularioNuevoEnCache()
		{
			if (cargandoCacheUsuario || !SessionService.PuedeGestionarUsuarios || usuarioSeleccionado != null)
			{
				return;
			}

			if (FormularioVacio())
			{
				LimpiarCacheFormularioNuevo();
				return;
			}

			GuardarValorCache(CacheNombre, txtNombre.Text);
			GuardarValorCache(CacheCorreo, txtCorreo.Text);
			GuardarValorCache(CacheDominio, ObtenerDominioCorreoActual());
			GuardarValorCache(CachePassword, txtPassword.Password);
			GuardarValorCache(CacheConfirmar, txtConfirmarPassword.Password);
			GuardarValorCache(CacheRol, ObtenerRolActual());
			GuardarValorCache(CacheActivo, (chkActivo.IsChecked == true).ToString());
		}

		// esta seccion sirve para guardar informacion de la administracion de cuentas y mantener los datos persistidos - GuardarValorCache
		private static void GuardarValorCache(string clave, string valor)
		{
			if (string.IsNullOrEmpty(valor))
			{
				App.FormCacheService.RemoveValue(clave);
				return;
			}

			App.FormCacheService.SetValue(clave, valor);
		}

		// esta seccion sirve para quitar informacion de la administracion de cuentas y dejar el estado consistente - LimpiarCacheFormularioNuevo
		private static void LimpiarCacheFormularioNuevo()
		{
			App.FormCacheService.ClearPrefix(CachePrefixUsuarioNuevo);
		}

		// esta seccion sirve para responder a la accion del usuario en la administracion de cuentas y mover el flujo al siguiente paso - FiltroUsuarios_Changed
		private void FiltroUsuarios_Changed(object sender, object e)
		{
			AplicarFiltroUsuarios();
		}

		// esta seccion sirve para mostrar mensajes o ventanas de la administracion de cuentas para que el usuario entienda el estado - MostrarError
		private void MostrarError(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = (SolidColorBrush)Application.Current.Resources["WineDangerBrush"];
			txtMensaje.Visibility = Visibility.Visible;
		}

		// esta seccion sirve para mostrar mensajes o ventanas de la administracion de cuentas para que el usuario entienda el estado - MostrarExito
		private void MostrarExito(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = (SolidColorBrush)Application.Current.Resources["WineSuccessBrush"];
			txtMensaje.Visibility = Visibility.Visible;
		}

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - OcultarMensaje
		private void OcultarMensaje()
		{
			txtMensaje.Visibility = Visibility.Collapsed;
		}
	}

	// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - UsuarioItemViewModel
	public class UsuarioItemViewModel
	{
		public Usuario Usuario { get; }
		public string Nombre => Usuario.Nombre ?? string.Empty;
		public string Correo => Usuario.Correo ?? string.Empty;
		public string IdTexto => $"ID: {(Usuario.Id?.Length > 8 ? Usuario.Id[..8] : Usuario.Id)}";
		public string RolTexto => $"Rol: {RolesSistema.Normalizar(Usuario.Rol)}";
		public string EstadoTexto => Usuario.Activo ? "Activo" : "Inactivo";
		public string AccionEstadoTexto => Usuario.Activo ? "Desactivar" : "Activar";

		// esta seccion sirve para manejar la administracion de cuentas y concentrar aqui esta parte del flujo - UsuarioItemViewModel
		public UsuarioItemViewModel(Usuario usuario)
		{
			Usuario = usuario;
		}
	}
}
