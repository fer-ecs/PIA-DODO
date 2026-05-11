using System;
using System.Collections.Generic;

namespace Vinoteca.Models
{
	public class Venta
	{
		public string Id { get; set; } = string.Empty;
		public DateTime Fecha { get; set; } = DateTime.Now;
		public string UsuarioId { get; set; } = string.Empty;
		public string EmpleadoId { get; set; } = string.Empty;
		public string NombreEmpleado { get; set; } = string.Empty;
		public string CorreoEmpleado { get; set; } = string.Empty;
		public string NombreCliente { get; set; } = string.Empty;
		public string CorreoCliente { get; set; } = string.Empty;
		public string RolUsuario { get; set; } = string.Empty;
		public string MetodoPago { get; set; } = "Efectivo";
		public double MontoRecibido { get; set; }
		public double Cambio { get; set; }
		public string ReferenciaPago { get; set; } = string.Empty;
		public List<CarritoItem> Productos { get; set; } = new List<CarritoItem>();
		public double Total { get; set; }
	}
}
