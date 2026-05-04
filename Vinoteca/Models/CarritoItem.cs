namespace Vinoteca.Models
{
	public class CarritoItem
	{
		public Producto Producto { get; set; } = new Producto();
		public int Cantidad { get; set; }

		// Propiedad calculada: multiplica el precio por la cantidad
		public double Subtotal => Producto.PrecioVenta * Cantidad;
	}
}