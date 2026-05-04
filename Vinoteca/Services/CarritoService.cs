using System.Collections.Generic;
using System.Linq;
using Vinoteca.Models;

namespace Vinoteca.Services
{
	public static class CarritoService
	{
		// Lista temporal en memoria (no se guarda en JSON porque es un carrito activo)
		private static List<CarritoItem> carrito = new List<CarritoItem>();

		public static void AgregarAlCarrito(Producto producto)
		{
			// Verificamos si el producto ya está en el carrito
			var itemExistente = carrito.FirstOrDefault(c => c.Producto.Id == producto.Id);

			if (itemExistente != null)
			{
				// Si ya existe, solo aumentamos la cantidad
				itemExistente.Cantidad++;
			}
			else
			{
				// Si es nuevo, lo agregamos con cantidad 1
				carrito.Add(new CarritoItem { Producto = producto, Cantidad = 1 });
			}
		}

		public static List<CarritoItem> ObtenerCarrito()
		{
			return carrito;
		}

		public static void LimpiarCarrito()
		{
			carrito.Clear();
		}

		public static double ObtenerTotal()
		{
			return carrito.Sum(c => c.Subtotal);
		}

		public static int ObtenerCantidadTotalArticulos()
		{
			return carrito.Sum(c => c.Cantidad);
		}
	}
}