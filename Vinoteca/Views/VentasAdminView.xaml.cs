using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class VentasAdminView : Page
	{
		public ObservableCollection<CarritoItem> ItemsCarrito { get; } = new();

		public VentasAdminView()
		{
			InitializeComponent();

			if (!SessionService.PuedeComprar)
			{
				txtTitulo.Text = "Cobro restringido";
				txtSubtitulo.Text = "Solo empleados pueden procesar ventas";
				txtEstado.Text = "Admin y supervisor no pueden realizar acciones de caja";
				txtEstado.Visibility = Visibility.Visible;
				lvCarrito.IsEnabled = false;
				return;
			}

			InputRestrictionsHelper.AplicarSoloDecimal(txtMontoRecibido);
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtReferenciaPago);
			cmbMetodoPago.SelectedIndex = 0;
			ActualizarInterfaz();
			CarritoService.CarritoActualizado += ActualizarInterfaz;
			Unloaded += VentasAdminView_Unloaded;
		}

		private void VentasAdminView_Unloaded(object sender, RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= ActualizarInterfaz;
			Unloaded -= VentasAdminView_Unloaded;
		}

		private void ActualizarInterfaz()
		{
			var carrito = CarritoService.ObtenerCarrito();
			ItemsCarrito.Clear();
			foreach (var item in carrito)
			{
				ItemsCarrito.Add(item);
			}

			lvCarrito.ItemsSource = ItemsCarrito;
			txtTotalCarrito.Text = CarritoService.ObtenerTotal().ToString("C");
			txtEstado.Visibility = Visibility.Collapsed;
			txtTitulo.Text = "Cobro de venta";
			txtSubtitulo.Text = $"{CarritoService.ObtenerCantidadTotalArticulos()} articulo(s) listos para cobrar";
			txtVacio.Visibility = carrito.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			ActualizarPago();
		}

		private async void btnVaciar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo empleados pueden vaciar una venta");
				return;
			}

			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				return;
			}

			bool confirmarVaciado = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Vaciar venta",
				"Deseas quitar todos los productos de esta venta?",
				"Vaciar");
			if (!confirmarVaciado)
			{
				return;
			}

			CarritoService.LimpiarCarrito();
		}

		private async void btnFinalizarVenta_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo empleados pueden cobrar ventas");
				return;
			}

			var items = CarritoService.ObtenerCarrito();
			if (items.Count == 0)
			{
				MostrarEstado("No hay productos para procesar");
				return;
			}

			double total = CarritoService.ObtenerTotal();
			string metodoPago = ObtenerMetodoPago();
			bool pagoEfectivo = metodoPago == "Efectivo";
			double montoRecibido = pagoEfectivo ? ObtenerMontoRecibido() : total;
			string referenciaPago = txtReferenciaPago.Text?.Trim() ?? string.Empty;

			if (pagoEfectivo && montoRecibido < total)
			{
				MostrarEstado("El efectivo recibido no cubre el total de la venta");
				return;
			}

			if (!pagoEfectivo && string.IsNullOrWhiteSpace(referenciaPago))
			{
				MostrarEstado("Ingresa la referencia o folio del pago");
				return;
			}

			foreach (var item in items)
			{
				var productoActual = DataService.ObtenerProductos().FirstOrDefault(p => p.Id == item.Producto.Id && p.Activo);
				if (productoActual == null)
				{
					MostrarEstado($"El producto {item.Producto.Nombre} ya no esta disponible");
					return;
				}

				if (item.Cantidad > productoActual.Stock)
				{
					MostrarEstado($"Stock insuficiente para {item.Producto.Nombre}");
					return;
				}
			}

			bool confirmarVenta = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Confirmar venta",
				"Deseas registrar la venta con los productos actuales?",
				"Confirmar");
			if (!confirmarVenta)
			{
				return;
			}

			double cambio = pagoEfectivo ? Math.Round(montoRecibido - total, 2) : 0;
			string nombreEmpleado = SessionService.UsuarioActivo?.Nombre ?? "Empleado";
			string correoEmpleado = SessionService.UsuarioActivo?.Correo ?? string.Empty;

			var nuevaVenta = new Venta
			{
				UsuarioId = SessionService.UsuarioActivo?.Id ?? string.Empty,
				EmpleadoId = SessionService.UsuarioActivo?.Id ?? string.Empty,
				NombreEmpleado = nombreEmpleado,
				CorreoEmpleado = correoEmpleado,
				NombreCliente = nombreEmpleado,
				CorreoCliente = correoEmpleado,
				RolUsuario = SessionService.RolActivo,
				MetodoPago = metodoPago,
				MontoRecibido = Math.Round(montoRecibido, 2),
				Cambio = cambio,
				ReferenciaPago = referenciaPago,
				Productos = new List<CarritoItem>(items),
				Total = total
			};

			DataService.GuardarVenta(nuevaVenta);

			foreach (var item in items)
			{
				var productoActual = DataService.ObtenerProductos().First(p => p.Id == item.Producto.Id);
				productoActual.Stock -= item.Cantidad;
				if (productoActual.Stock < 0)
				{
					productoActual.Stock = 0;
				}

				DataService.GuardarProducto(productoActual);
			}

			CarritoService.LimpiarCarrito();

			await TicketPreviewService.MostrarAsync(nuevaVenta, XamlRoot);
			Frame.Navigate(typeof(MisTicketsView));
		}

		private void cmbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ActualizarPago();
		}

		private void txtPago_TextChanged(object sender, TextChangedEventArgs e)
		{
			ActualizarPago();
		}

		private void ActualizarPago()
		{
			if (txtCambio == null || txtMontoRecibido == null || cmbMetodoPago == null)
			{
				return;
			}

			double total = CarritoService.ObtenerTotal();
			string metodoPago = ObtenerMetodoPago();
			bool pagoEfectivo = metodoPago == "Efectivo";
			double recibido = pagoEfectivo ? ObtenerMontoRecibido() : total;
			double cambio = pagoEfectivo ? Math.Max(0, recibido - total) : 0;

			txtMontoRecibido.IsEnabled = pagoEfectivo;
			txtReferenciaPago.IsEnabled = !pagoEfectivo;
			txtPagoAyuda.Text = pagoEfectivo
				? "Ingresa el efectivo recibido para calcular el cambio"
				: "Captura la referencia del pago aprobado";
			txtCambio.Text = $"Cambio: {cambio.ToString("C")}";
		}

		private string ObtenerMetodoPago()
		{
			return (cmbMetodoPago.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
		}

		private double ObtenerMontoRecibido()
		{
			string texto = (txtMontoRecibido.Text ?? string.Empty).Replace(",", ".");
			return double.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double valor)
				? valor
				: 0;
		}

		private void MostrarEstado(string mensaje)
		{
			txtEstado.Text = mensaje;
			txtEstado.Visibility = Visibility.Visible;
		}
	}
}
