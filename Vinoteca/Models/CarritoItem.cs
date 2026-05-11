namespace Vinoteca.Models
{
	public class CarritoItem
	{
		public Producto Producto { get; set; } = new Producto();
		public int Cantidad { get; set; }

		// Este total se recalcula cada vez para reflejar cantidad por precio
		public double Subtotal => Producto.PrecioVenta * Cantidad;
	}
}
