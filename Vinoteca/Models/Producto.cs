using System;

namespace Vinoteca.Models
{
	// esta seccion sirve para manejar el modelo de datos y concentrar aqui esta parte del flujo - Producto
	public class Producto
	{
		public string Id { get; set; } = string.Empty;
		public string CodigoCorto => Id ?? string.Empty;
		public string? Nombre { get; set; }
		public string? Marca { get; set; }
		public string? Categoria { get; set; }
		public double PrecioCompra { get; set; }
		public double PrecioVenta { get; set; }
		public int Stock { get; set; }
		public string? Volumen { get; set; }
		public double PorcentajeAlcohol { get; set; }
		public string? ImagenPath { get; set; }
		public DateTime FechaRegistro { get; set; } = DateTime.Now;
		public bool Activo { get; set; } = true;
	}
}
