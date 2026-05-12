using Vinoteca.Models;

namespace Vinoteca.Services
{
	// esta seccion sirve para agrupar la sesion del usuario y dejar esa responsabilidad en un solo archivo - SessionService
	public static class SessionService
	{
		public static Usuario? UsuarioActivo { get; private set; }
		public static bool HaySesionActiva => UsuarioActivo != null;
		public static string RolActivo => RolesSistema.Normalizar(UsuarioActivo?.Rol);
		public static bool EsAdministradorActivo => RolActivo == RolesSistema.Administrador;
		public static bool EsSupervisorActivo => RolActivo == RolesSistema.Supervisor;
		public static bool EsEmpleadoActivo => RolActivo == RolesSistema.Empleado;
		public static bool EsClienteActivo => EsEmpleadoActivo;
		public static bool PuedeGestionarUsuarios => EsAdministradorActivo;
		public static bool PuedeGestionarInventario => EsAdministradorActivo;
		public static bool PuedeVerInformacionOperativa => EsAdministradorActivo;
		public static bool PuedeVerReportes => EsAdministradorActivo;
		public static bool PuedeVerAnalisisSupervisor => EsSupervisorActivo;
		public static bool PuedeComprar => EsEmpleadoActivo;
		public static bool PuedeProcesarVentas => EsEmpleadoActivo;

		// esta seccion sirve para controlar el acceso del usuario y dejar clara la sesion activa - IniciarSesion
		public static void IniciarSesion(Usuario usuario)
		{
			if (UsuarioActivo?.Id != usuario.Id)
			{
				CarritoService.LimpiarCarrito();
			}

			usuario.Rol = RolesSistema.Normalizar(usuario.Rol);
			UsuarioActivo = usuario;
		}

		// esta seccion sirve para controlar el acceso del usuario y dejar clara la sesion activa - CerrarSesion
		public static void CerrarSesion()
		{
			CarritoService.LimpiarCarrito();
			UsuarioActivo = null;
		}
	}
}
