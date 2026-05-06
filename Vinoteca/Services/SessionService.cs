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
		public static bool EsClienteActivo => RolActivo == RolesSistema.Cliente;
		public static bool PuedeGestionarUsuarios => EsAdministradorActivo;
		public static bool PuedeGestionarInventario => EsAdministradorActivo;
		public static bool PuedeVerInformacionOperativa => EsAdministradorActivo || EsSupervisorActivo;
		public static bool PuedeVerReportes => EsAdministradorActivo || EsSupervisorActivo;
		public static bool PuedeProcesarVentas => EsAdministradorActivo;

		public static void IniciarSesion(Usuario usuario)
		{
			usuario.Rol = RolesSistema.Normalizar(usuario.Rol);
			UsuarioActivo = usuario;
		}

		public static void CerrarSesion()
		{
			UsuarioActivo = null;
		}
	}
}
