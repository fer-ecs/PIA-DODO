using System;
using System.Collections.Generic;

namespace Vinoteca.Models
{
	public class Venta
	{
		public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
		public DateTime Fecha { get; set; } = DateTime.Now;
		public string UsuarioId { get; set; } = string.Empty;
		public string NombreCliente { get; set; } = string.Empty;
		public string CorreoCliente { get; set; } = string.Empty;
		public string RolUsuario { get; set; } = string.Empty;
		public List<CarritoItem> Productos { get; set; } = new List<CarritoItem>();
		public double Total { get; set; }
	}
}
