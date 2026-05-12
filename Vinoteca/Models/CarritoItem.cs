namespace Vinoteca.Models
{
	// esta seccion sirve para manejar el modelo de datos y concentrar aqui esta parte del flujo - CarritoItem
	public class CarritoItem
	{
		// esta seccion sirve para manejar el modelo de datos y concentrar aqui esta parte del flujo - Producto
		public Producto Producto { get; set; } = new Producto();
		public int Cantidad { get; set; }

		// Este total se recalcula cada vez para reflejar cantidad por precio
		public double Subtotal => Producto.PrecioVenta * Cantidad;
	}
}
