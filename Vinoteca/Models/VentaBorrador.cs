using System;
using System.Collections.Generic;

namespace Vinoteca.Models
{
	public class VentaBorrador
	{
		public string UsuarioId { get; set; } = string.Empty;
		public string MetodoPago { get; set; } = "Efectivo";
		public double MontoRecibido { get; set; }
		public string ReferenciaPago { get; set; } = string.Empty;
		public List<CarritoItem> Productos { get; set; } = new();
		public DateTime FechaActualizacion { get; set; } = DateTime.Now;
	}
}
