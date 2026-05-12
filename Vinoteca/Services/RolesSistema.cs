namespace Vinoteca.Services
{
	// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - RolesSistema
	public static class RolesSistema
	{
		public const string Administrador = "Administrador";
		public const string Empleado = "Empleado";
		public const string Cliente = Empleado;
		public const string Supervisor = "Supervisor";

		// esta seccion sirve para ordenar y ajustar datos de la parte del sistema para trabajar con valores limpios - Normalizar
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
