namespace Vinoteca.Models
{
	public class Usuario
	{
		public string Id { get; set; } = string.Empty;
		public string? Nombre { get; set; } // El '?' permite que sea nulo
		public string? Correo { get; set; }
		public string? Contrasena { get; set; }
		public bool EsAdmin { get; set; }
		public bool Activo { get; set; }
	}
}