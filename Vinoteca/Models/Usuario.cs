using Vinoteca.Services;

namespace Vinoteca.Models
{
	public class Usuario
	{
		public string Id { get; set; } = string.Empty;
		public string? Nombre { get; set; }
		public string? Correo { get; set; }
		public string? Contrasena { get; set; }
		public string Rol { get; set; } = RolesSistema.Empleado;
		public bool EsAdmin
		{
			get => Rol == RolesSistema.Administrador;
			set
			{
				if (value)
				{
					Rol = RolesSistema.Administrador;
				}
				else if (Rol == RolesSistema.Administrador)
				{
					Rol = RolesSistema.Empleado;
				}
			}
		}
		public bool Activo { get; set; }
	}
}
