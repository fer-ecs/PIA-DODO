using Vinoteca.Models;

namespace Vinoteca.Services
{
	public static class SessionService
	{
		public static Usuario? UsuarioActivo { get; private set; }
		public static bool HaySesionActiva => UsuarioActivo != null;
		public static bool EsAdminActivo => UsuarioActivo?.EsAdmin == true;

		public static void IniciarSesion(Usuario usuario)
		{
			UsuarioActivo = usuario;
		}

		public static void CerrarSesion()
		{
			UsuarioActivo = null;
		}
	}
}
