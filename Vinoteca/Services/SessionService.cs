using Vinoteca.Models;

namespace Vinoteca.Services
{
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

		public static void IniciarSesion(Usuario usuario)
		{
			if (UsuarioActivo?.Id != usuario.Id)
			{
				CarritoService.LimpiarCarrito();
			}

			usuario.Rol = RolesSistema.Normalizar(usuario.Rol);
			UsuarioActivo = usuario;
		}

		public static void CerrarSesion()
		{
			CarritoService.LimpiarCarrito();
			UsuarioActivo = null;
		}
	}
}
