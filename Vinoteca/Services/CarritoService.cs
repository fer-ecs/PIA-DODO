using System;
using System.Collections.Generic;
using System.Linq;
using Vinoteca.Models;

namespace Vinoteca.Services
{
	// esta seccion sirve para agrupar el carrito de venta y dejar esa responsabilidad en un solo archivo - CarritoService
	public static class CarritoService
	{
		private static readonly List<CarritoItem> carrito = new();

		public static event Action? CarritoActualizado;

		// esta seccion sirve para agregar informacion a el carrito de venta y recalcular lo necesario - AgregarAlCarrito
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
				mensaje = "No hay stock disponible";
				return false;
			}

			var itemExistente = carrito.FirstOrDefault(c => c.Producto.Id == productoActual.Id);
			int cantidadNueva = (itemExistente?.Cantidad ?? 0) + 1;
			if (cantidadNueva > productoActual.Stock)
			{
				mensaje = "No hay suficiente stock disponible";
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

			int stockDisponible = productoActual.Stock - cantidadNueva;
			if (stockDisponible <= 0)
			{
				mensaje = $"{productoActual.Nombre} agregado. No hay stock disponible";
			}
			else if (stockDisponible < 5)
			{
				mensaje = $"{productoActual.Nombre} agregado. Stock bajo: quedan {stockDisponible}";
			}

			NotificarCambio();
			return true;
		}

		// esta seccion sirve para manejar el carrito de venta y concentrar aqui esta parte del flujo - CambiarCantidad
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

			if (productoActual.Stock <= 0)
			{
				mensaje = "No hay stock disponible";
				return false;
			}

			if (nuevaCantidad > productoActual.Stock)
			{
				mensaje = "No hay suficiente stock disponible";
				return false;
			}

			item.Producto = productoActual;
			item.Cantidad = nuevaCantidad;
			int stockDisponible = productoActual.Stock - nuevaCantidad;
			if (stockDisponible <= 0)
			{
				mensaje = $"{productoActual.Nombre} queda sin stock disponible";
			}
			else if (stockDisponible < 5)
			{
				mensaje = $"Stock bajo para {productoActual.Nombre}: quedan {stockDisponible}";
			}

			NotificarCambio();
			return true;
		}

		// esta seccion sirve para quitar informacion de el carrito de venta y dejar el estado consistente - QuitarDelCarrito
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

		// esta seccion sirve para leer informacion de el carrito de venta y regresarla lista para usarse - ObtenerCarrito
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

		// esta seccion sirve para quitar informacion de el carrito de venta y dejar el estado consistente - LimpiarCarrito
		public static void LimpiarCarrito()
		{
			carrito.Clear();
			NotificarCambio();
		}

		// esta seccion sirve para manejar el carrito de venta y concentrar aqui esta parte del flujo - ReemplazarCarrito
		public static void ReemplazarCarrito(IEnumerable<CarritoItem> items)
		{
			carrito.Clear();

			foreach (var item in items)
			{
				if (item?.Producto == null || item.Cantidad <= 0)
				{
					continue;
				}

				var productoActual = DataService.ObtenerProductos()
					.FirstOrDefault(p => p.Id == item.Producto.Id && p.Activo);
				if (productoActual == null)
				{
					continue;
				}

				if (productoActual.Stock <= 0)
				{
					continue;
				}

				int cantidadPermitida = Math.Min(item.Cantidad, productoActual.Stock);
				if (cantidadPermitida <= 0)
				{
					continue;
				}

				carrito.Add(new CarritoItem
				{
					Producto = productoActual,
					Cantidad = cantidadPermitida
				});
			}

			NotificarCambio();
		}

		// esta seccion sirve para actualizar el carrito de venta despues de un cambio y sincronizar la pantalla - SincronizarConInventario
		public static bool SincronizarConInventario(out string mensaje)
		{
			mensaje = string.Empty;
			bool carritoActualizado = false;
			bool ventaValida = true;
			var alertas = new List<string>();
			var productosActuales = DataService.ObtenerProductos();

			for (int i = carrito.Count - 1; i >= 0; i--)
			{
				var item = carrito[i];
				if (item?.Producto == null || string.IsNullOrWhiteSpace(item.Producto.Id) || item.Cantidad <= 0)
				{
					carrito.RemoveAt(i);
					carritoActualizado = true;
					ventaValida = false;
					alertas.Add("Se quitaron productos sin cantidad valida");
					continue;
				}

				var productoActual = productosActuales.FirstOrDefault(p => p.Id == item.Producto.Id && p.Activo);
				if (productoActual == null || productoActual.Stock <= 0)
				{
					carrito.RemoveAt(i);
					carritoActualizado = true;
					ventaValida = false;
					alertas.Add($"{item.Producto.Nombre} ya no esta disponible");
					continue;
				}

				if (item.Cantidad > productoActual.Stock)
				{
					item.Cantidad = productoActual.Stock;
					carritoActualizado = true;
					ventaValida = false;
					alertas.Add($"Se ajusto la cantidad de {productoActual.Nombre} al stock disponible");
				}

				if (!ReferenceEquals(item.Producto, productoActual))
				{
					item.Producto = productoActual;
				}
			}

			if (carritoActualizado)
			{
				NotificarCambio();
			}

			mensaje = string.Join(" | ", alertas.Distinct());
			return ventaValida;
		}

		// esta seccion sirve para revisar reglas de el carrito de venta y evitar que pase un dato incorrecto - ValidarDisponibilidad
		public static bool ValidarDisponibilidad(out string mensaje)
		{
			mensaje = string.Empty;
			if (!SincronizarConInventario(out mensaje))
			{
				return false;
			}

			var productosActuales = DataService.ObtenerProductos();

			foreach (var item in carrito)
			{
				if (item?.Producto == null || string.IsNullOrWhiteSpace(item.Producto.Id) || item.Cantidad <= 0)
				{
					mensaje = "Quita productos sin cantidad valida antes de cobrar";
					return false;
				}

				var productoActual = productosActuales
					.FirstOrDefault(p => p.Id == item.Producto.Id && p.Activo);

				if (productoActual == null)
				{
					mensaje = $"El producto {item.Producto.Nombre} ya no esta disponible";
					return false;
				}

				if (productoActual.Stock <= 0)
				{
					mensaje = $"No hay stock disponible para {productoActual.Nombre}";
					return false;
				}

				if (item.Cantidad > productoActual.Stock)
				{
					mensaje = $"Stock insuficiente para {productoActual.Nombre}";
					return false;
				}
			}

			return true;
		}

		// esta seccion sirve para leer informacion de el carrito de venta y regresarla lista para usarse - ObtenerTotal
		public static double ObtenerTotal()
		{
			return carrito.Sum(c => c.Subtotal);
		}

		// esta seccion sirve para leer informacion de el carrito de venta y regresarla lista para usarse - ObtenerCantidadTotalArticulos
		public static int ObtenerCantidadTotalArticulos()
		{
			return carrito.Sum(c => c.Cantidad);
		}

		// esta seccion sirve para manejar el carrito de venta y concentrar aqui esta parte del flujo - NotificarCambio
		private static void NotificarCambio()
		{
			CarritoActualizado?.Invoke();
		}
	}
}
