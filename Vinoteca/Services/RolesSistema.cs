namespace Vinoteca.Services
{
	public static class RolesSistema
	{
		public const string Administrador = "Administrador";
		public const string Empleado = "Empleado";
		public const string Cliente = Empleado;
		public const string Supervisor = "Supervisor";

		public static string Normalizar(string? rol)
		{
			return rol switch
			{
				Administrador => Administrador,
				Empleado => Empleado,
				"Cliente" => Empleado,
				Supervisor => Supervisor,
				_ => Empleado
			};
		}
	}
}
