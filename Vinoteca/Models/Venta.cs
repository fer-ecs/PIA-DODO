using System;
using System.Collections.Generic;

namespace Vinoteca.Models
{
	public class Venta
	{
		public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
		public DateTime Fecha { get; set; } = DateTime.Now;
		public List<CarritoItem> Productos { get; set; } = new List<CarritoItem>();
		public double Total { get; set; }
	}
}