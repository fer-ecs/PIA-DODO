using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Vinoteca.Views
{
	public sealed partial class InventarioView : Page, ICambiosPendientes
	{
		public ObservableCollection<ProductoItemViewModel> ProductosMostrados { get; } = new();
		private List<Producto> todosLosProductos = new();
		private Producto? productoSeleccionado;
		private bool ignorarCambioSeleccion;

		public InventarioView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			InputRestrictionsHelper.AplicarSoloLetrasConEspacios(txtNombre, txtMarca, txtNuevaCategoria);
			InputRestrictionsHelper.AplicarSoloDecimal(txtPrecioVenta, txtPrecioMin, txtPrecioMax);
			InputRestrictionsHelper.AplicarSoloNumeros(txtStock);
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtImagen, txtBuscar);
			lvProductos.ItemsSource = ProductosMostrados;
			ConfigurarFiltros();

			if (!SessionService.PuedeVerInformacionOperativa)
			{
				BloquearAcceso();
				return;
			}

			if (!SessionService.PuedeGestionarInventario)
			{
				ConfigurarModoSoloLectura();
			}

			CargarCategorias();
			CargarDatos();
		}

		public bool TieneCambiosPendientes => SessionService.PuedeGestionarInventario && FormularioTieneCambios();

		public string ObtenerMensajeCambiosPendientes()
		{
			return productoSeleccionado == null
				? "Hay un producto nuevo sin guardar"
				: "Hay cambios sin guardar en el producto seleccionado";
		}

		private void BloquearAcceso()
		{
			txtNombre.IsEnabled = false;
			txtMarca.IsEnabled = false;
			cmbCategoria.IsEnabled = false;
			txtNuevaCategoria.IsEnabled = false;
			btnAgregarCategoria.IsEnabled = false;
			btnEliminarCategoria.IsEnabled = false;
			txtPrecioVenta.IsEnabled = false;
			txtStock.IsEnabled = false;
			txtImagen.IsEnabled = false;
			btnSeleccionarImagen.IsEnabled = false;
			btnGuardar.IsEnabled = false;
			btnEliminar.IsEnabled = false;
			btnLimpiar.IsEnabled = false;
			lvProductos.IsEnabled = false;
			txtBuscar.IsEnabled = false;
			MostrarMensaje("Solo un administrador puede gestionar inventario", false);
		}

		private void ConfigurarModoSoloLectura()
		{
			txtNombre.IsEnabled = false;
			txtMarca.IsEnabled = false;
			cmbCategoria.IsEnabled = false;
			txtNuevaCategoria.IsEnabled = false;
			btnAgregarCategoria.IsEnabled = false;
			btnEliminarCategoria.IsEnabled = false;
			txtPrecioVenta.IsEnabled = false;
			txtStock.IsEnabled = false;
			txtImagen.IsEnabled = false;
			btnSeleccionarImagen.IsEnabled = false;
			btnGuardar.IsEnabled = false;
			btnEliminar.IsEnabled = false;
			btnLimpiar.IsEnabled = false;
			MostrarMensaje("Modo de solo lectura para supervision del inventario", true);
		}

		private void CargarDatos()
		{
			todosLosProductos = DataService.ObtenerProductos().ToList();
			AplicarFiltro();
		}

		private void CargarCategorias(string? categoriaSeleccionada = null)
		{
			cmbCategoria.Items.Clear();
			cmbFiltroCategoria.Items.Clear();
			cmbFiltroCategoria.Items.Add("Todas");
			foreach (var categoria in DataService.ObtenerCategorias())
			{
				cmbCategoria.Items.Add(categoria);
				cmbFiltroCategoria.Items.Add(categoria);
			}

			if (cmbFiltroCategoria.SelectedIndex < 0)
			{
				cmbFiltroCategoria.SelectedIndex = 0;
			}

			if (!string.IsNullOrWhiteSpace(categoriaSeleccionada))
			{
				cmbCategoria.SelectedItem = cmbCategoria.Items
					.Cast<string>()
					.FirstOrDefault(c => c.Equals(categoriaSeleccionada, StringComparison.OrdinalIgnoreCase));
			}
		}

		private void AplicarFiltro()
		{
			string busqueda = txtBuscar.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			string categoria = ObtenerCategoriaFiltro();
			double precioMin = ObtenerDecimalFormulario(txtPrecioMin.Text);
			double precioMax = ObtenerDecimalFormulario(txtPrecioMax.Text);
			var filtrados = todosLosProductos.Where(p =>
				(string.IsNullOrEmpty(busqueda) ||
				(p.Id?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Nombre?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Marca?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Categoria?.ToLowerInvariant().Contains(busqueda) ?? false)) &&
				(string.IsNullOrWhiteSpace(categoria) || string.Equals(p.Categoria, categoria, StringComparison.OrdinalIgnoreCase)) &&
				(precioMin < 0 || p.PrecioVenta >= precioMin) &&
				(precioMax < 0 || p.PrecioVenta <= precioMax));

			filtrados = ObtenerOrdenProductos() switch
			{
				"Nombre Z-A" => filtrados.OrderByDescending(p => p.Nombre),
				"Precio menor" => filtrados.OrderBy(p => p.PrecioVenta),
				"Precio mayor" => filtrados.OrderByDescending(p => p.PrecioVenta),
				"Stock menor" => filtrados.OrderBy(p => p.Stock),
				"Stock mayor" => filtrados.OrderByDescending(p => p.Stock),
				"ID" => filtrados.OrderBy(p => p.Id),
				_ => filtrados.OrderBy(p => p.Nombre)
			};

			ProductosMostrados.Clear();
			foreach (var producto in filtrados.Select(p => new ProductoItemViewModel(p)))
			{
				ProductosMostrados.Add(producto);
			}

			txtResumenProductos.Text = $"{ProductosMostrados.Count} de {todosLosProductos.Count} productos";
		}

		private void ConfigurarFiltros()
		{
			cmbOrdenProductos.SelectedIndex = 0;
		}

		private string ObtenerCategoriaFiltro()
		{
			string categoria = cmbFiltroCategoria.SelectedItem?.ToString() ?? string.Empty;
			return categoria == "Todas" ? string.Empty : categoria;
		}

		private string ObtenerOrdenProductos()
		{
			return (cmbOrdenProductos.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Nombre A-Z";
		}

		private void btnGuardar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarInventario)
			{
				MostrarMensaje("Solo el administrador puede guardar productos", false);
				return;
			}

			if (!ValidarFormulario(out string nombre, out string marca, out string categoria, out string imagen, out double precio, out int stock))
			{
				return;
			}

			bool esNuevoProducto = productoSeleccionado == null;
			var producto = productoSeleccionado ?? new Producto();
			producto.Nombre = nombre;
			producto.Marca = marca;
			producto.Categoria = categoria;
			producto.PrecioVenta = precio;
			producto.Stock = stock;
			producto.ImagenPath = imagen;
			producto.Activo = stock > 0;

			DataService.GuardarProducto(producto);

			LimpiarFormularioInterno();
			CargarDatos();
			MostrarMensaje(esNuevoProducto ? "Producto creado correctamente" : "Producto actualizado correctamente", true);
		}

		private bool ValidarFormulario(out string nombre, out string marca, out string categoria, out string imagen, out double precio, out int stock)
		{
			nombre = txtNombre.Text?.Trim() ?? string.Empty;
			marca = txtMarca.Text?.Trim() ?? string.Empty;
			categoria = ObtenerCategoriaActual();
			imagen = txtImagen.Text?.Trim() ?? string.Empty;
			string precioTexto = txtPrecioVenta.Text?.Trim() ?? string.Empty;
			string stockTexto = txtStock.Text?.Trim() ?? string.Empty;
			precio = 0;
			stock = 0;

			if (string.IsNullOrWhiteSpace(nombre))
			{
				MostrarMensaje("El nombre del producto es obligatorio", false);
				return false;
			}

			if (nombre.Length < 3 || nombre.Length > 60)
			{
				MostrarMensaje("El nombre debe tener entre 3 y 60 caracteres", false);
				return false;
			}

			if (!FormValidationHelper.EsTextoConLetrasYEspacios(nombre))
			{
				MostrarMensaje("El nombre solo debe contener letras y espacios entre palabras", false);
				return false;
			}

			if (string.IsNullOrWhiteSpace(marca))
			{
				MostrarMensaje("La marca es obligatoria", false);
				return false;
			}

			if (marca.Length < 2 || marca.Length > 40)
			{
				MostrarMensaje("La marca debe tener entre 2 y 40 caracteres", false);
				return false;
			}

			if (!FormValidationHelper.EsTextoConLetrasYEspacios(marca))
			{
				MostrarMensaje("La marca solo debe contener letras y espacios entre palabras", false);
				return false;
			}

			if (string.IsNullOrWhiteSpace(categoria))
			{
				MostrarMensaje("Selecciona una categoria", false);
				return false;
			}

			if (!double.TryParse(NormalizarDecimal(precioTexto), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double precioValor) ||
				precioValor <= 0 ||
				precioValor > 100000)
			{
				MostrarMensaje("El precio debe ser mayor a 0 y maximo 100000", false);
				return false;
			}

			if (ContarDecimales(precioTexto) > 2)
			{
				MostrarMensaje("El precio solo puede tener hasta 2 decimales", false);
				return false;
			}

			if (!int.TryParse(stockTexto, out int stockValor) || stockValor < 0 || stockValor > 5000)
			{
				MostrarMensaje("El stock solo debe contener numeros entre 0 y 5000", false);
				return false;
			}

			precio = Math.Round(precioValor, 2);
			stock = stockValor;

			if (!string.IsNullOrWhiteSpace(imagen))
			{
				if (imagen.Length > 300)
				{
					MostrarMensaje("La ruta o URL de la imagen no debe exceder 300 caracteres", false);
					return false;
				}

				bool esUrlValida = Uri.TryCreate(imagen, UriKind.Absolute, out Uri? uriResultado)
					&& (uriResultado.Scheme == Uri.UriSchemeHttp || uriResultado.Scheme == Uri.UriSchemeHttps);
				bool esRutaLocalValida = Regex.IsMatch(imagen, @"^[A-Za-z]:\\.+");

				if (!esUrlValida && !esRutaLocalValida)
				{
					MostrarMensaje("La imagen debe ser una URL valida o una ruta local valida", false);
					return false;
				}
			}

			string nombreValidado = nombre;
			string marcaValidada = marca;
			bool duplicado = DataService.ObtenerProductos().Any(p =>
				p.Id != productoSeleccionado?.Id &&
				p.Nombre != null &&
				p.Marca != null &&
				p.Nombre.Equals(nombreValidado, StringComparison.OrdinalIgnoreCase) &&
				p.Marca.Equals(marcaValidada, StringComparison.OrdinalIgnoreCase));

			if (duplicado)
			{
				MostrarMensaje("Ya existe un producto con el mismo nombre y marca", false);
				return false;
			}

			return true;
		}

		private async void lvProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ignorarCambioSeleccion)
			{
				return;
			}

			if (lvProductos.SelectedItem is not ProductoItemViewModel item)
			{
				return;
			}

			if (productoSeleccionado?.Id == item.Producto.Id)
			{
				return;
			}

			if (TieneCambiosPendientes)
			{
				bool puedeCambiar = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
					XamlRoot,
					this,
					"cambiar de producto",
					false);
				if (!puedeCambiar)
				{
					RestaurarSeleccionAnterior();
					return;
				}
			}

			CargarProductoEnFormulario(item.Producto);
			OcultarMensaje();
		}

		private async void btnEliminar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarInventario)
			{
				MostrarMensaje("Solo el administrador puede eliminar productos", false);
				return;
			}

			if (productoSeleccionado == null)
			{
				MostrarMensaje("Selecciona un producto para eliminar", false);
				return;
			}

			bool confirmarEliminacion = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Eliminar producto",
				"Deseas eliminar el producto seleccionado?",
				"Eliminar");
			if (!confirmarEliminacion)
			{
				return;
			}

			DataService.EliminarProducto(productoSeleccionado.Id);
			LimpiarFormularioInterno();
			CargarDatos();
			MostrarMensaje("Producto eliminado correctamente", true);
		}

		private async void btnLimpiar_Click(object sender, RoutedEventArgs e)
		{
			if (TieneCambiosPendientes)
			{
				bool puedeLimpiar = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
					XamlRoot,
					this,
					"limpiar el formulario",
					false);
				if (!puedeLimpiar)
				{
					return;
				}
			}

			LimpiarFormularioInterno();
			OcultarMensaje();
		}

		private void LimpiarFormularioInterno()
		{
			productoSeleccionado = null;
			txtNombre.Text = string.Empty;
			txtMarca.Text = string.Empty;
			cmbCategoria.SelectedIndex = -1;
			txtNuevaCategoria.Text = string.Empty;
			txtPrecioVenta.Text = "0";
			txtStock.Text = "0";
			txtImagen.Text = string.Empty;

			ignorarCambioSeleccion = true;
			lvProductos.SelectedItem = null;
			ignorarCambioSeleccion = false;
		}

		private void CargarProductoEnFormulario(Producto producto)
		{
			productoSeleccionado = producto;
			txtNombre.Text = producto.Nombre ?? string.Empty;
			txtMarca.Text = producto.Marca ?? string.Empty;
			txtPrecioVenta.Text = producto.PrecioVenta.ToString("0.##", CultureInfo.InvariantCulture);
			txtStock.Text = producto.Stock.ToString();
			txtImagen.Text = producto.ImagenPath ?? string.Empty;
			CargarCategorias(producto.Categoria);
		}

		private void RestaurarSeleccionAnterior()
		{
			ignorarCambioSeleccion = true;
			lvProductos.SelectedItem = productoSeleccionado == null
				? null
				: ProductosMostrados.FirstOrDefault(p => p.Producto.Id == productoSeleccionado.Id);
			ignorarCambioSeleccion = false;
		}

		private bool FormularioTieneCambios()
		{
			if (productoSeleccionado == null)
			{
				return !FormularioVacio();
			}

			return !FormularioCoincideConProducto(productoSeleccionado);
		}

		private bool FormularioVacio()
		{
			return string.IsNullOrWhiteSpace(txtNombre.Text) &&
				string.IsNullOrWhiteSpace(txtMarca.Text) &&
				string.IsNullOrWhiteSpace(ObtenerCategoriaActual()) &&
				ObtenerDecimalFormulario(txtPrecioVenta.Text) == 0 &&
				ObtenerNumeroFormulario(txtStock.Text) == 0 &&
				string.IsNullOrWhiteSpace(txtImagen.Text);
		}

		private bool FormularioCoincideConProducto(Producto producto)
		{
			return string.Equals((txtNombre.Text ?? string.Empty).Trim(), producto.Nombre ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals((txtMarca.Text ?? string.Empty).Trim(), producto.Marca ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals(ObtenerCategoriaActual(), producto.Categoria ?? string.Empty, StringComparison.Ordinal) &&
				Math.Abs(ObtenerDecimalFormulario(txtPrecioVenta.Text) - producto.PrecioVenta) < 0.01 &&
				ObtenerNumeroFormulario(txtStock.Text) == producto.Stock &&
				string.Equals((txtImagen.Text ?? string.Empty).Trim(), producto.ImagenPath ?? string.Empty, StringComparison.Ordinal);
		}

		private string ObtenerCategoriaActual()
		{
			return cmbCategoria.SelectedItem?.ToString() ?? string.Empty;
		}

		private static int ObtenerNumeroFormulario(string? texto)
		{
			return int.TryParse(texto?.Trim(), out int valor) ? valor : -1;
		}

		private static double ObtenerDecimalFormulario(string? texto)
		{
			return double.TryParse(NormalizarDecimal(texto), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double valor) ? valor : -1;
		}

		private static string NormalizarDecimal(string? texto)
		{
			return (texto ?? string.Empty).Trim().Replace(',', '.');
		}

		private static int ContarDecimales(string texto)
		{
			string normalizado = NormalizarDecimal(texto);
			int indiceDecimal = normalizado.IndexOf('.');
			return indiceDecimal < 0 ? 0 : normalizado.Length - indiceDecimal - 1;
		}

		private void btnAgregarCategoria_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarInventario)
			{
				MostrarMensaje("Solo el administrador puede crear categorias", false);
				return;
			}

			string categoria = txtNuevaCategoria.Text?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(categoria))
			{
				MostrarMensaje("Escribe el nombre de la categoria", false);
				return;
			}

			if (categoria.Length < 3 || categoria.Length > 30)
			{
				MostrarMensaje("La categoria debe tener entre 3 y 30 caracteres", false);
				return;
			}

			if (!FormValidationHelper.EsTextoConLetrasYEspacios(categoria))
			{
				MostrarMensaje("La categoria solo debe contener letras y espacios entre palabras", false);
				return;
			}

			if (!DataService.GuardarCategoria(categoria))
			{
				MostrarMensaje("Ya existe una categoria con ese nombre", false);
				return;
			}

			txtNuevaCategoria.Text = string.Empty;
			CargarCategorias(categoria);
			MostrarMensaje("Categoria creada correctamente", true);
		}

		private void btnEliminarCategoria_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarInventario)
			{
				MostrarMensaje("Solo el administrador puede quitar categorias", false);
				return;
			}

			string categoria = ObtenerCategoriaActual();
			if (string.IsNullOrWhiteSpace(categoria))
			{
				MostrarMensaje("Selecciona una categoria para quitar", false);
				return;
			}

			bool tieneProductos = DataService.ObtenerProductos().Any(p =>
				string.Equals(p.Categoria, categoria, StringComparison.OrdinalIgnoreCase));
			if (tieneProductos)
			{
				MostrarMensaje("No puedes quitar una categoria usada por productos", false);
				return;
			}

			if (!DataService.EliminarCategoria(categoria))
			{
				MostrarMensaje("No se pudo quitar la categoria seleccionada", false);
				return;
			}

			CargarCategorias();
			MostrarMensaje("Categoria eliminada correctamente", true);
		}

		private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
		{
			AplicarFiltro();
		}

		private void FiltroProductos_Changed(object sender, object e)
		{
			AplicarFiltro();
		}

		private async void btnSeleccionarImagen_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeGestionarInventario)
			{
				MostrarMensaje("Solo el administrador puede seleccionar imagenes", false);
				return;
			}

			if (App.VentanaPrincipal == null)
			{
				MostrarMensaje("No se pudo abrir el explorador de archivos", false);
				return;
			}

			var picker = new FileOpenPicker
			{
				SuggestedStartLocation = PickerLocationId.PicturesLibrary
			};
			picker.FileTypeFilter.Add(".jpg");
			picker.FileTypeFilter.Add(".jpeg");
			picker.FileTypeFilter.Add(".png");
			picker.FileTypeFilter.Add(".webp");

			InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.VentanaPrincipal));
			var archivo = await picker.PickSingleFileAsync();
			if (archivo == null)
			{
				return;
			}

			txtImagen.Text = archivo.Path;
			MostrarMensaje("Imagen seleccionada correctamente", true);
		}

		private void MostrarMensaje(string mensaje, bool esExito)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = (SolidColorBrush)Application.Current.Resources[esExito ? "WineSuccessBrush" : "WineDangerBrush"];
			txtMensaje.Visibility = Visibility.Visible;
		}

		private void OcultarMensaje()
		{
			txtMensaje.Visibility = Visibility.Collapsed;
		}
	}

	public class ProductoItemViewModel
	{
		public Producto Producto { get; }
		public string Nombre => Producto.Nombre ?? string.Empty;
		public string Marca => Producto.Marca ?? string.Empty;
		public string Categoria => Producto.Categoria ?? string.Empty;
		public string IdTexto => $"ID: {(Producto.Id?.Length > 8 ? Producto.Id[..8] : Producto.Id)}";
		public string PrecioTexto => Producto.PrecioVenta.ToString("C");
		public string StockTexto => Producto.Stock.ToString();
		public string EstadoTexto => Producto.Activo ? "Activo" : "Inactivo";

		public ProductoItemViewModel(Producto producto)
		{
			Producto = producto;
		}
	}
}
