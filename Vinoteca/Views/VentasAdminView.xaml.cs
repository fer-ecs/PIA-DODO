using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar el cobro de ventas y dejar esa responsabilidad en un solo archivo - VentasAdminView
	public sealed partial class VentasAdminView : Page, IVentaTemporal
	{
		public ObservableCollection<CarritoItem> ItemsCarrito { get; } = new();
		private const double LimiteCambioEfectivo = 10000;
		private const string MensajeEfectivoExcedeLimite = "El efectivo recibido supera el limite permitido";
		private const string MensajeFolioInvalido = "El folio solo puede contener letras y numeros";
		private bool actualizandoPago;
		private bool pagoVerificado;
		private bool cargandoBorrador;

		// esta seccion sirve para agrupar el cobro de ventas y dejar esa responsabilidad en un solo archivo - VentasAdminView
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
			InputRestrictionsHelper.AplicarSoloLetrasNumeros(txtReferenciaPago);
			cmbMetodoPago.SelectedIndex = 0;
			ActualizarInterfaz();
			CarritoService.CarritoActualizado += ActualizarInterfaz;
			Loaded += VentasAdminView_Loaded;
			Unloaded += VentasAdminView_Unloaded;
		}

		public bool TieneVentaTemporal => CarritoService.ObtenerCarrito().Count > 0;

		// esta seccion sirve para armar datos o contenido de el cobro de ventas y devolverlo ya preparado - CrearVentaBorrador
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

		// esta seccion sirve para responder a la accion del usuario en el cobro de ventas y mover el flujo al siguiente paso - VentasAdminView_Loaded
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
				cargandoBorrador = true;
				CarritoService.ReemplazarCarrito(borrador.Productos);
				SeleccionarMetodoPago(borrador.MetodoPago);
				txtMontoRecibido.Text = borrador.MontoRecibido > 0
					? borrador.MontoRecibido.ToString("0.00", CultureInfo.InvariantCulture)
					: string.Empty;
				txtReferenciaPago.Text = borrador.ReferenciaPago ?? string.Empty;
				cargandoBorrador = false;
				pagoVerificado = false;
				MostrarEstado("Venta recuperada, verifica el pago antes de cobrar");
				ActualizarPago();
				GuardarBorradorVentaActual();
			}
			else if (resultado == ContentDialogResult.Secondary)
			{
				DataService.EliminarVentaBorrador(usuarioId);
				MostrarEstado("Borrador descartado");
			}
		}

		// esta seccion sirve para responder a la accion del usuario en el cobro de ventas y mover el flujo al siguiente paso - VentasAdminView_Unloaded
		private void VentasAdminView_Unloaded(object sender, RoutedEventArgs e)
		{
			GuardarBorradorVentaActual();
			CarritoService.CarritoActualizado -= ActualizarInterfaz;
			Loaded -= VentasAdminView_Loaded;
			Unloaded -= VentasAdminView_Unloaded;
		}

		// esta seccion sirve para actualizar el cobro de ventas despues de un cambio y sincronizar la pantalla - ActualizarInterfaz
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
			GuardarBorradorVentaActual();
		}

		// esta seccion sirve para responder a la accion del usuario en el cobro de ventas y mover el flujo al siguiente paso - btnVaciar_Click
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
			GuardarBorradorVentaActual();
		}

		// esta seccion sirve para responder a la accion del usuario en el cobro de ventas y mover el flujo al siguiente paso - btnFinalizarVenta_Click
		private async void btnFinalizarVenta_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				await MostrarAlertaPagoAsync("DODOAVISO", "Solo empleados pueden cobrar ventas");
				return;
			}

			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				await MostrarAlertaPagoAsync("DODOAVISO", "No hay productos para procesar");
				return;
			}

			bool pagoEstabaVerificado = pagoVerificado;
			double totalAntesDeSincronizar = CarritoService.ObtenerTotal();
			if (!CarritoService.SincronizarConInventario(out string mensajeStock))
			{
				pagoVerificado = false;
				await MostrarAlertaPagoAsync("DODOAVISO", mensajeStock);
				ActualizarPago();
				return;
			}

			double totalActual = CarritoService.ObtenerTotal();
			if (Math.Abs(totalActual - totalAntesDeSincronizar) > 0.01)
			{
				pagoVerificado = false;
				await MostrarAlertaPagoAsync("DODOAVISO", "El precio o inventario cambio, verifica el pago de nuevo");
				ActualizarPago();
				return;
			}

			if (!pagoEstabaVerificado)
			{
				await MostrarAlertaPagoAsync("DODOAVISO", "Verifica el pago antes de cobrar");
				return;
			}

			var items = CarritoService.ObtenerCarrito();
			double total = totalActual;
			string metodoPago = ObtenerMetodoPago();
			bool pagoEfectivo = metodoPago == "Efectivo";
			double montoRecibido = pagoEfectivo ? ObtenerMontoRecibido() : total;
			string referenciaPago = txtReferenciaPago.Text?.Trim() ?? string.Empty;

			if (montoRecibido <= 0)
			{
				await MostrarAlertaPagoAsync("DODOAVISO", "Ingresa un monto mayor a 0");
				return;
			}

			if (pagoEfectivo && montoRecibido < total)
			{
				await MostrarAlertaPagoAsync("DODOAVISO", "El efectivo recibido no cubre el total de la venta");
				return;
			}

			if (pagoEfectivo && EfectivoSuperaLimite(montoRecibido, total))
			{
				await MostrarAlertaPagoAsync("DODOAVISO", MensajeEfectivoExcedeLimite);
				return;
			}

			if (!pagoEfectivo && string.IsNullOrWhiteSpace(referenciaPago))
			{
				await MostrarAlertaPagoAsync("DODOAVISO", metodoPago == "Tarjeta"
					? "Ingresa el folio de autorizacion de la terminal"
					: "Ingresa la referencia bancaria de la transferencia");
				return;
			}

			if (!pagoEfectivo && !EsFolioValido(referenciaPago))
			{
				await MostrarAlertaPagoAsync("DODOAVISO", MensajeFolioInvalido);
				return;
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

			List<string> alertasStock;
			try
			{
				alertasStock = DataService.RegistrarVentaConInventario(nuevaVenta);
			}
			catch (InvalidOperationException ex)
			{
				pagoVerificado = false;
				await MostrarAlertaPagoAsync("DODOAVISO", ex.Message);
				ActualizarPago();
				return;
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

		// esta seccion sirve para responder a la accion del usuario en el cobro de ventas y mover el flujo al siguiente paso - cmbMetodoPago_SelectionChanged
		private void cmbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ResetearVerificacion();
			ActualizarPago();
			GuardarBorradorVentaActual();
		}

		// esta seccion sirve para responder a la accion del usuario en el cobro de ventas y mover el flujo al siguiente paso - txtPago_TextChanged
		private void txtPago_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (actualizandoPago)
			{
				return;
			}

			ResetearVerificacion();
			ActualizarPago();
			GuardarBorradorVentaActual();
		}

		// esta seccion sirve para responder a la accion del usuario en el cobro de ventas y mover el flujo al siguiente paso - btnVerificarPago_Click
		private async void btnVerificarPago_Click(object sender, RoutedEventArgs e)
		{
			if (ValidarPago(out string mensaje))
			{
				pagoVerificado = true;
				MostrarEstado("DODOAVISO: pago verificado, ya puedes confirmar la venta");
				ActualizarPago();
				return;
			}

			pagoVerificado = false;
			MostrarEstado(mensaje);
			ActualizarPago();
			await MostrarAlertaPagoAsync("Revisa el pago", mensaje);
		}

		// esta seccion sirve para actualizar el cobro de ventas despues de un cambio y sincronizar la pantalla - ActualizarPago
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

		// esta seccion sirve para guardar informacion de el cobro de ventas y mantener los datos persistidos - GuardarBorradorVentaActual
		private void GuardarBorradorVentaActual()
		{
			if (cargandoBorrador || !SessionService.PuedeComprar)
			{
				return;
			}

			string usuarioId = SessionService.UsuarioActivo?.Id ?? string.Empty;
			if (string.IsNullOrWhiteSpace(usuarioId))
			{
				return;
			}

			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				DataService.EliminarVentaBorrador(usuarioId);
				return;
			}

			DataService.GuardarVentaBorrador(CrearVentaBorrador());
		}

		// esta seccion sirve para revisar reglas de el cobro de ventas y evitar que pase un dato incorrecto - ValidarPago
		private bool ValidarPago(out string mensaje)
		{
			mensaje = string.Empty;
			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				mensaje = "No hay productos para procesar";
				return false;
			}

			if (!CarritoService.ValidarDisponibilidad(out mensaje))
			{
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

			if (pagoEfectivo && EfectivoSuperaLimite(montoRecibido, total))
			{
				mensaje = MensajeEfectivoExcedeLimite;
				return false;
			}

			if (!pagoEfectivo && string.IsNullOrWhiteSpace(referenciaPago))
			{
				mensaje = metodoPago == "Tarjeta"
					? "Ingresa el folio de autorizacion de la terminal"
					: "Ingresa la referencia bancaria de la transferencia";
				return false;
			}

			if (!pagoEfectivo && !EsFolioValido(referenciaPago))
			{
				mensaje = MensajeFolioInvalido;
				return false;
			}

			return true;
		}

		// esta seccion sirve para revisar reglas de el cobro de ventas y evitar que pase un dato incorrecto - EsFolioValido
		private static bool EsFolioValido(string folio)
		{
			return Regex.IsMatch(folio, "^[A-Za-z0-9]+$");
		}

		// esta seccion sirve para manejar el cobro de ventas y concentrar aqui esta parte del flujo - EfectivoSuperaLimite
		private static bool EfectivoSuperaLimite(double montoRecibido, double total)
		{
			return montoRecibido > total + LimiteCambioEfectivo;
		}

		// esta seccion sirve para manejar el cobro de ventas y concentrar aqui esta parte del flujo - ResetearVerificacion
		private void ResetearVerificacion()
		{
			pagoVerificado = false;
		}

		// esta seccion sirve para leer informacion de el cobro de ventas y regresarla lista para usarse - ObtenerMetodoPago
		private string ObtenerMetodoPago()
		{
			return (cmbMetodoPago.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
		}

		// esta seccion sirve para leer informacion de el cobro de ventas y regresarla lista para usarse - ObtenerMontoRecibido
		private double ObtenerMontoRecibido()
		{
			string texto = (txtMontoRecibido.Text ?? string.Empty).Replace(",", ".");
			return double.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double valor)
				? valor
				: 0;
		}

		// esta seccion sirve para mostrar mensajes o ventanas de el cobro de ventas para que el usuario entienda el estado - MostrarEstado
		private void MostrarEstado(string mensaje)
		{
			txtEstado.Text = mensaje;
			txtEstado.Visibility = Visibility.Visible;
			if (bdAlertaPago != null && txtAlertaPago != null)
			{
				txtAlertaPago.Text = mensaje;
				bdAlertaPago.Visibility = Visibility.Visible;
			}
		}

		// esta seccion muestra una alerta clara cuando falta un paso del cobro
		private async Task MostrarAlertaPagoAsync(string titulo, string mensaje)
		{
			MostrarEstado(mensaje);
			var dialog = new ContentDialog
			{
				Title = titulo,
				Content = mensaje,
				CloseButtonText = "Entendido",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = XamlRoot
			};
			await dialog.ShowAsync();
		}

		// esta seccion sirve para manejar el cobro de ventas y concentrar aqui esta parte del flujo - SeleccionarMetodoPago
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

