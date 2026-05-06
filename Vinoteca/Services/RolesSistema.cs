namespace Vinoteca.Services
{
	public static class RolesSistema
	{
		public const string Administrador = "Administrador";
		public const string Cliente = "Cliente";
		public const string Supervisor = "Supervisor";

		public static string Normalizar(string? rol)
		{
			return rol switch
			{
				Administrador => Administrador,
				Supervisor => Supervisor,
				_ => Cliente
			};
		}
	}
}
