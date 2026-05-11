using System;
using System.Collections.Generic;
using System.Linq;
using Vinoteca.Models;

namespace Vinoteca.Services
{
	public static class CarritoService
	{
		private static readonly List<CarritoItem> carrito = new();

		public static event Action? CarritoActualizado;

		public static bool AgregarAlCarrito(Producto producto, out string mensaje)
		{
			mensaje = string.Empty;

			if (!SessionService.PuedeComprar)
			{
				mensaje = "Solo empleados pueden agregar productos a la venta";
				return false;
			}

			if (producto == null || string.IsNullOrWhiteSpace(producto.Id))
			{
				mensaje = "Producto invalido";
				return false;
			}

			var productoActual = DataService.ObtenerProductos()
				.FirstOrDefault(p => p.Id == producto.Id && p.Activo);

			if (productoActual == null)
			{
				mensaje = "El producto ya no esta disponible";
				return false;
			}

			if (productoActual.Stock <= 0)
			{
				mensaje = "El producto no tiene stock disponible";
				return false;
			}

			var itemExistente = carrito.FirstOrDefault(c => c.Producto.Id == productoActual.Id);
			int cantidadNueva = (itemExistente?.Cantidad ?? 0) + 1;
			if (cantidadNueva > productoActual.Stock)
			{
				mensaje = "No hay suficiente stock para agregar mas unidades";
				return false;
			}

			if (itemExistente != null)
			{
				itemExistente.Cantidad = cantidadNueva;
				itemExistente.Producto = productoActual;
			}
			else
			{
				carrito.Add(new CarritoItem { Producto = productoActual, Cantidad = 1 });
			}

			NotificarCambio();
			return true;
		}

		public static bool CambiarCantidad(string productoId, int nuevaCantidad, out string mensaje)
		{
			mensaje = string.Empty;
			if (!SessionService.PuedeComprar)
			{
				mensaje = "Solo empleados pueden modificar la venta";
				return false;
			}

			var item = carrito.FirstOrDefault(c => c.Producto.Id == productoId);
			if (item == null)
			{
				mensaje = "No se encontro el producto en la venta";
				return false;
			}

			if (nuevaCantidad <= 0)
			{
				carrito.Remove(item);
				NotificarCambio();
				return true;
			}

			var productoActual = DataService.ObtenerProductos()
				.FirstOrDefault(p => p.Id == productoId && p.Activo);

			if (productoActual == null)
			{
				mensaje = "El producto ya no esta disponible";
				return false;
			}

			if (nuevaCantidad > productoActual.Stock)
			{
				mensaje = "La cantidad solicitada excede el stock disponible";
				return false;
			}

			item.Producto = productoActual;
			item.Cantidad = nuevaCantidad;
			NotificarCambio();
			return true;
		}

		public static bool QuitarDelCarrito(string productoId)
		{
			if (!SessionService.PuedeComprar)
			{
				return false;
			}

			var item = carrito.FirstOrDefault(c => c.Producto.Id == productoId);
			if (item == null)
			{
				return false;
			}

			carrito.Remove(item);
			NotificarCambio();
			return true;
		}

		public static List<CarritoItem> ObtenerCarrito()
		{
			return carrito
				.Select(c => new CarritoItem
				{
					Producto = c.Producto,
					Cantidad = c.Cantidad
				})
				.ToList();
		}

		public static void LimpiarCarrito()
		{
			carrito.Clear();
			NotificarCambio();
		}

		public static double ObtenerTotal()
		{
			return carrito.Sum(c => c.Subtotal);
		}

		public static int ObtenerCantidadTotalArticulos()
		{
			return carrito.Sum(c => c.Cantidad);
		}

		private static void NotificarCambio()
		{
			CarritoActualizado?.Invoke();
		}
	}
}
