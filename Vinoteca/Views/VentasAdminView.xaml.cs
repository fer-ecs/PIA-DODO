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
	public sealed partial class VentasAdminView : Page, IVentaTemporal
	{
		public ObservableCollection<CarritoItem> ItemsCarrito { get; } = new();
		private bool actualizandoPago;
		private bool pagoVerificado;

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
			Loaded += VentasAdminView_Loaded;
			Unloaded += VentasAdminView_Unloaded;
		}

		public bool TieneVentaTemporal => CarritoService.ObtenerCarrito().Count > 0;

		public VentaBorrador CrearVentaBorrador()
		{
			return new VentaBorrador
			{
				UsuarioId = SessionService.UsuarioActivo?.Id ?? string.Empty,
				MetodoPago = ObtenerMetodoPago(),
				MontoRecibido = ObtenerMontoRecibido(),
				ReferenciaPago = txtReferenciaPago.Text?.Trim() ?? string.Empty,
				Productos = CarritoService.ObtenerCarrito(),
				FechaActualizacion = DateTime.Now
			};
		}

		private async void VentasAdminView_Loaded(object sender, RoutedEventArgs e)
		{
			string usuarioId = SessionService.UsuarioActivo?.Id ?? string.Empty;
			if (string.IsNullOrWhiteSpace(usuarioId) || CarritoService.ObtenerCarrito().Count > 0)
			{
				return;
			}

			var borrador = DataService.ObtenerVentaBorrador(usuarioId);
			if (borrador == null || borrador.Productos.Count == 0)
			{
				return;
			}

			var dialog = new ContentDialog
			{
				Title = "Venta en borrador",
				Content = $"Hay una venta guardada temporalmente del {borrador.FechaActualizacion:g}. Deseas retomarla?",
				PrimaryButtonText = "Retomar",
				SecondaryButtonText = "Descartar",
				CloseButtonText = "Despues",
				XamlRoot = XamlRoot,
				DefaultButton = ContentDialogButton.Primary
			};

			var resultado = await dialog.ShowAsync();
			if (resultado == ContentDialogResult.Primary)
			{
				CarritoService.ReemplazarCarrito(borrador.Productos);
				SeleccionarMetodoPago(borrador.MetodoPago);
				txtMontoRecibido.Text = borrador.MontoRecibido > 0
					? borrador.MontoRecibido.ToString("0.00", CultureInfo.InvariantCulture)
					: string.Empty;
				txtReferenciaPago.Text = borrador.ReferenciaPago ?? string.Empty;
				pagoVerificado = false;
				MostrarEstado("Venta recuperada, verifica el pago antes de cobrar");
				ActualizarPago();
			}
			else if (resultado == ContentDialogResult.Secondary)
			{
				DataService.EliminarVentaBorrador(usuarioId);
				MostrarEstado("Borrador descartado");
			}
		}

		private void VentasAdminView_Unloaded(object sender, RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= ActualizarInterfaz;
			Loaded -= VentasAdminView_Loaded;
			Unloaded -= VentasAdminView_Unloaded;
		}

		private void ActualizarInterfaz()
		{
			pagoVerificado = false;
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
				$"Deseas quitar los {CarritoService.ObtenerCantidadTotalArticulos()} articulo(s) de esta venta?",
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

			if (!pagoVerificado)
			{
				MostrarEstado("Verifica el pago antes de cobrar");
				return;
			}

			double total = CarritoService.ObtenerTotal();
			string metodoPago = ObtenerMetodoPago();
			bool pagoEfectivo = metodoPago == "Efectivo";
			double montoRecibido = pagoEfectivo ? ObtenerMontoRecibido() : total;
			string referenciaPago = txtReferenciaPago.Text?.Trim() ?? string.Empty;

			if (montoRecibido <= 0)
			{
				MostrarEstado("Ingresa un monto mayor a 0");
				return;
			}

			if (pagoEfectivo && montoRecibido < total)
			{
				MostrarEstado("El efectivo recibido no cubre el total de la venta");
				return;
			}

			if (!pagoEfectivo && string.IsNullOrWhiteSpace(referenciaPago))
			{
				MostrarEstado(metodoPago == "Tarjeta"
					? "Ingresa el folio de autorizacion de la terminal"
					: "Ingresa la referencia bancaria de la transferencia");
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
				$"Deseas registrar esta venta por {total.ToString("C")} con pago {metodoPago}?",
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

			var alertasStock = new List<string>();
			foreach (var item in items)
			{
				var productoActual = DataService.ObtenerProductos().First(p => p.Id == item.Producto.Id);
				productoActual.Stock -= item.Cantidad;
				if (productoActual.Stock < 0)
				{
					productoActual.Stock = 0;
				}

				DataService.GuardarProducto(productoActual);

				if (productoActual.Stock <= 0)
				{
					alertasStock.Add($"{productoActual.Nombre}: sin stock disponible");
				}
				else if (productoActual.Stock < 5)
				{
					alertasStock.Add($"{productoActual.Nombre}: stock bajo ({productoActual.Stock})");
				}
			}

			CarritoService.LimpiarCarrito();
			DataService.EliminarVentaBorrador(SessionService.UsuarioActivo?.Id ?? string.Empty);

			if (alertasStock.Count > 0)
			{
				MostrarEstado(string.Join(" | ", alertasStock));
			}

			await TicketPreviewService.MostrarAsync(nuevaVenta, XamlRoot);
			Frame.Navigate(typeof(MisTicketsView));
		}

		private void cmbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ResetearVerificacion();
			ActualizarPago();
		}

		private void txtPago_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (actualizandoPago)
			{
				return;
			}

			ResetearVerificacion();
			ActualizarPago();
		}

		private void btnVerificarPago_Click(object sender, RoutedEventArgs e)
		{
			if (ValidarPago(out string mensaje))
			{
				pagoVerificado = true;
				MostrarEstado("Pago verificado, ya puedes confirmar la venta");
				ActualizarPago();
				return;
			}

			pagoVerificado = false;
			MostrarEstado(mensaje);
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
			bool hayProductos = CarritoService.ObtenerCarrito().Count > 0;

			actualizandoPago = true;
			txtMontoRecibido.IsEnabled = pagoEfectivo;
			txtReferenciaPago.IsEnabled = !pagoEfectivo;
			lblMontoPago.Text = pagoEfectivo ? "Monto recibido" : "Monto cobrado";

			if (!pagoEfectivo)
			{
				string totalTexto = total.ToString("0.00", CultureInfo.InvariantCulture);
				if (txtMontoRecibido.Text != totalTexto)
				{
					txtMontoRecibido.Text = totalTexto;
				}
			}

			if (pagoEfectivo)
			{
				txtReferenciaPago.PlaceholderText = "Opcional";
				txtPagoAyuda.Text = "Efectivo";
				txtPagoDetalle.Text = "Captura el dinero recibido para calcular el cambio y registrar el ticket";
				txtCambio.Visibility = Visibility.Visible;
				txtCambio.Text = $"Cambio: {cambio.ToString("C")}";
			}
			else if (metodoPago == "Tarjeta")
			{
				txtReferenciaPago.PlaceholderText = "Folio de autorizacion";
				txtPagoAyuda.Text = "Tarjeta";
				txtPagoDetalle.Text = "Primero cobra en la terminal externa y despues captura el folio para emitir el ticket";
				txtCambio.Visibility = Visibility.Collapsed;
			}
			else
			{
				txtReferenciaPago.PlaceholderText = "Referencia bancaria";
				txtPagoAyuda.Text = "Transferencia";
				txtPagoDetalle.Text = "Confirma el deposito en la cuenta del negocio y captura la referencia antes de emitir el ticket";
				txtCambio.Visibility = Visibility.Collapsed;
			}
			btnVaciar.IsEnabled = hayProductos && SessionService.PuedeComprar;
			btnVerificarPago.IsEnabled = hayProductos && SessionService.PuedeComprar && cmbMetodoPago.SelectedIndex >= 0;
			btnFinalizarVenta.IsEnabled = hayProductos && SessionService.PuedeComprar && pagoVerificado;
			actualizandoPago = false;
		}

		private bool ValidarPago(out string mensaje)
		{
			mensaje = string.Empty;
			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				mensaje = "No hay productos para procesar";
				return false;
			}

			if (cmbMetodoPago.SelectedIndex < 0)
			{
				mensaje = "Selecciona un metodo de pago";
				return false;
			}

			double total = CarritoService.ObtenerTotal();
			if (total <= 0)
			{
				mensaje = "La venta debe tener un total mayor a 0";
				return false;
			}

			string metodoPago = ObtenerMetodoPago();
			bool pagoEfectivo = metodoPago == "Efectivo";
			double montoRecibido = pagoEfectivo ? ObtenerMontoRecibido() : total;
			string referenciaPago = txtReferenciaPago.Text?.Trim() ?? string.Empty;

			if (montoRecibido <= 0)
			{
				mensaje = "Ingresa un monto mayor a 0";
				return false;
			}

			if (pagoEfectivo && montoRecibido < total)
			{
				mensaje = "El efectivo recibido no cubre el total de la venta";
				return false;
			}

			if (!pagoEfectivo && string.IsNullOrWhiteSpace(referenciaPago))
			{
				mensaje = metodoPago == "Tarjeta"
					? "Ingresa el folio de autorizacion de la terminal"
					: "Ingresa la referencia bancaria de la transferencia";
				return false;
			}

			return true;
		}

		private void ResetearVerificacion()
		{
			pagoVerificado = false;
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

		private void SeleccionarMetodoPago(string metodoPago)
		{
			foreach (ComboBoxItem item in cmbMetodoPago.Items)
			{
				if (item.Content?.ToString()?.Equals(metodoPago, StringComparison.OrdinalIgnoreCase) == true)
				{
					cmbMetodoPago.SelectedItem = item;
					return;
				}
			}

			cmbMetodoPago.SelectedIndex = 0;
		}
	}
}
