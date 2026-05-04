using Vinoteca.Models;

namespace Vinoteca.Services
{
	public static class SessionService
	{
		public static Usuario UsuarioActivo { get; private set; }

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