using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar el inventario y dejar esa responsabilidad en un solo archivo - InventarioView
	public sealed partial class InventarioView : Page, ICambiosPendientes, IDescartaCambiosPendientes
	{
		private const string CachePrefixProductoNuevo = "Inventario_Nuevo_";
		private const string CacheNombre = "Inventario_Nuevo_Nombre";
		private const string CacheMarca = "Inventario_Nuevo_Marca";
		private const string CacheCategoria = "Inventario_Nuevo_Categoria";
		private const string CacheNuevaCategoria = "Inventario_Nuevo_NuevaCategoria";
		private const string CachePrecio = "Inventario_Nuevo_Precio";
		private const string CacheStock = "Inventario_Nuevo_Stock";
		private const string CacheImagen = "Inventario_Nuevo_Imagen";

		public ObservableCollection<ProductoItemViewModel> ProductosMostrados { get; } = new();
		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - HashSet<string>
		private static readonly HashSet<string> ExtensionesImagenPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".jpg",
			".jpeg",
			".png",
			".webp"
		};

		private List<Producto> todosLosProductos = new();
		private Producto? productoSeleccionado;
		private bool ignorarCambioSeleccion;
		private bool ignorarCambiosFiltro;
		private bool cargandoCacheProducto;

		// esta seccion sirve para agrupar el inventario y dejar esa responsabilidad en un solo archivo - InventarioView
		public InventarioView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			InputRestrictionsHelper.AplicarSoloLetrasConEspacios(txtNombre, txtMarca, txtNuevaCategoria);
			InputRestrictionsHelper.AplicarSoloDecimal(txtPrecioVenta, txtPrecioMin, txtPrecioMax);
			InputRestrictionsHelper.AplicarSoloNumeros(txtStock);
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtBuscar);
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
			ConfigurarCacheFormulario();
			CargarFormularioNuevoDesdeCache();
			DataService.ProductosActualizados += ProductosActualizados;
			Unloaded += InventarioView_Unloaded;
		}

		public bool TieneCambiosPendientes => SessionService.PuedeGestionarInventario && FormularioTieneCambios();

		// esta seccion sirve para leer informacion de el inventario y regresarla lista para usarse - ObtenerMensajeCambiosPendientes
		public string ObtenerMensajeCambiosPendientes()
		{
			return productoSeleccionado == null
				? "Hay un producto nuevo sin guardar"
				: "Hay cambios sin guardar en el producto seleccionado";
		}

		public void DescartarCambiosPendientes()
		{
			LimpiarCacheFormularioNuevo();
		}

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - InventarioView_Unloaded
		private void InventarioView_Unloaded(object sender, RoutedEventArgs e)
		{
			DataService.ProductosActualizados -= ProductosActualizados;
			Unloaded -= InventarioView_Unloaded;
		}

		// esta seccion sirve para actualizar el inventario despues de un cambio y sincronizar la pantalla - ProductosActualizados
		private void ProductosActualizados()
		{
			DispatcherQueue.TryEnqueue(CargarDatos);
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - BloquearAcceso
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

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - ConfigurarModoSoloLectura
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

		// esta seccion sirve para cargar informacion de el inventario y preparar lo que se muestra en pantalla - CargarDatos
		private void CargarDatos()
		{
			todosLosProductos = DataService.ObtenerProductos().ToList();
			AplicarFiltro();
		}

		// esta seccion sirve para cargar informacion de el inventario y preparar lo que se muestra en pantalla - CargarCategorias
		private void CargarCategorias(string? categoriaSeleccionada = null, bool actualizarFiltro = true)
		{
			string categoriaFiltroActual = ObtenerCategoriaFiltro();

			cmbCategoria.Items.Clear();
			if (actualizarFiltro)
			{
				ignorarCambiosFiltro = true;
				cmbFiltroCategoria.Items.Clear();
				cmbFiltroCategoria.Items.Add("Todas");
			}

			foreach (var categoria in DataService.ObtenerCategorias())
			{
				cmbCategoria.Items.Add(categoria);
				if (actualizarFiltro)
				{
					cmbFiltroCategoria.Items.Add(categoria);
				}
			}

			if (actualizarFiltro)
			{
				cmbFiltroCategoria.SelectedItem = cmbFiltroCategoria.Items
					.Cast<string>()
					.FirstOrDefault(c => c.Equals(categoriaFiltroActual, StringComparison.OrdinalIgnoreCase)) ?? "Todas";
				ignorarCambiosFiltro = false;
			}

			if (!string.IsNullOrWhiteSpace(categoriaSeleccionada))
			{
				cmbCategoria.SelectedItem = cmbCategoria.Items
					.Cast<string>()
					.FirstOrDefault(c => c.Equals(categoriaSeleccionada, StringComparison.OrdinalIgnoreCase));
			}
		}

		// esta seccion sirve para ordenar y ajustar datos de el inventario para trabajar con valores limpios - AplicarFiltro
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
				"ID" => filtrados.OrderBy(p => ObtenerIdNumerico(p.Id)),
				_ => filtrados.OrderBy(p => p.Nombre)
			};

			ProductosMostrados.Clear();
			foreach (var producto in filtrados.Select(p => new ProductoItemViewModel(p)))
			{
				ProductosMostrados.Add(producto);
			}

			txtResumenProductos.Text = $"{ProductosMostrados.Count} de {todosLosProductos.Count} productos";
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - ConfigurarFiltros
		private void ConfigurarFiltros()
		{
			cmbOrdenProductos.SelectedIndex = 0;
		}

		// esta seccion sirve para leer informacion de el inventario y regresarla lista para usarse - ObtenerCategoriaFiltro
		private string ObtenerCategoriaFiltro()
		{
			string categoria = cmbFiltroCategoria.SelectedItem?.ToString() ?? string.Empty;
			return categoria == "Todas" ? string.Empty : categoria;
		}

		// esta seccion sirve para leer informacion de el inventario y regresarla lista para usarse - ObtenerOrdenProductos
		private string ObtenerOrdenProductos()
		{
			return (cmbOrdenProductos.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Nombre A-Z";
		}

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - btnGuardar_Click
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

		// esta seccion sirve para revisar reglas de el inventario y evitar que pase un dato incorrecto - ValidarFormulario
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
					MostrarMensaje("La ruta de la imagen no debe exceder 300 caracteres", false);
					return false;
				}

				if (!ImageAssetService.EsImagenDelProyectoValida(imagen))
				{
					MostrarMensaje("Selecciona una imagen desde el explorador de archivos", false);
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

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - lvProductos_SelectionChanged
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

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - btnEliminar_Click
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

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - btnLimpiar_Click
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

		// esta seccion sirve para quitar informacion de el inventario y dejar el estado consistente - LimpiarFormularioInterno
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
			LimpiarCacheFormularioNuevo();
		}

		// esta seccion sirve para cargar informacion de el inventario y preparar lo que se muestra en pantalla - CargarProductoEnFormulario
		private void CargarProductoEnFormulario(Producto producto)
		{
			LimpiarCacheFormularioNuevo();
			productoSeleccionado = producto;
			txtNombre.Text = producto.Nombre ?? string.Empty;
			txtMarca.Text = producto.Marca ?? string.Empty;
			txtPrecioVenta.Text = producto.PrecioVenta.ToString("0.##", CultureInfo.InvariantCulture);
			txtStock.Text = producto.Stock.ToString();
			txtImagen.Text = producto.ImagenPath ?? string.Empty;
			CargarCategorias(producto.Categoria, actualizarFiltro: false);
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - RestaurarSeleccionAnterior
		private void RestaurarSeleccionAnterior()
		{
			ignorarCambioSeleccion = true;
			lvProductos.SelectedItem = productoSeleccionado == null
				? null
				: ProductosMostrados.FirstOrDefault(p => p.Producto.Id == productoSeleccionado.Id);
			ignorarCambioSeleccion = false;
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - FormularioTieneCambios
		private bool FormularioTieneCambios()
		{
			if (productoSeleccionado == null)
			{
				return !FormularioVacio();
			}

			return !FormularioCoincideConProducto(productoSeleccionado);
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - FormularioVacio
		private bool FormularioVacio()
		{
			return string.IsNullOrWhiteSpace(txtNombre.Text) &&
				string.IsNullOrWhiteSpace(txtMarca.Text) &&
				string.IsNullOrWhiteSpace(ObtenerCategoriaActual()) &&
				ObtenerDecimalFormulario(txtPrecioVenta.Text) == 0 &&
				ObtenerNumeroFormulario(txtStock.Text) == 0 &&
				string.IsNullOrWhiteSpace(txtImagen.Text);
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - FormularioCoincideConProducto
		private bool FormularioCoincideConProducto(Producto producto)
		{
			return string.Equals((txtNombre.Text ?? string.Empty).Trim(), producto.Nombre ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals((txtMarca.Text ?? string.Empty).Trim(), producto.Marca ?? string.Empty, StringComparison.Ordinal) &&
				string.Equals(ObtenerCategoriaActual(), producto.Categoria ?? string.Empty, StringComparison.Ordinal) &&
				Math.Abs(ObtenerDecimalFormulario(txtPrecioVenta.Text) - producto.PrecioVenta) < 0.01 &&
				ObtenerNumeroFormulario(txtStock.Text) == producto.Stock &&
				string.Equals((txtImagen.Text ?? string.Empty).Trim(), producto.ImagenPath ?? string.Empty, StringComparison.Ordinal);
		}

		// esta seccion sirve para leer informacion de el inventario y regresarla lista para usarse - ObtenerCategoriaActual
		private string ObtenerCategoriaActual()
		{
			return cmbCategoria.SelectedItem?.ToString() ?? string.Empty;
		}

		// esta seccion sirve para leer informacion de el inventario y regresarla lista para usarse - ObtenerNumeroFormulario
		private static int ObtenerNumeroFormulario(string? texto)
		{
			return int.TryParse(texto?.Trim(), out int valor) ? valor : -1;
		}

		// esta seccion sirve para leer informacion de el inventario y regresarla lista para usarse - ObtenerDecimalFormulario
		private static double ObtenerDecimalFormulario(string? texto)
		{
			return double.TryParse(NormalizarDecimal(texto), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double valor) ? valor : -1;
		}

		// esta seccion sirve para ordenar y ajustar datos de el inventario para trabajar con valores limpios - NormalizarDecimal
		private static string NormalizarDecimal(string? texto)
		{
			return (texto ?? string.Empty).Trim().Replace(',', '.');
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - ContarDecimales
		private static int ContarDecimales(string texto)
		{
			string normalizado = NormalizarDecimal(texto);
			int indiceDecimal = normalizado.IndexOf('.');
			return indiceDecimal < 0 ? 0 : normalizado.Length - indiceDecimal - 1;
		}

		// esta seccion sirve para leer informacion de el inventario y regresarla lista para usarse - ObtenerIdNumerico
		private static int ObtenerIdNumerico(string? id)
		{
			return int.TryParse(id, out int valor) ? valor : int.MaxValue;
		}

		// esta seccion sirve para revisar reglas de el inventario y evitar que pase un dato incorrecto - EsArchivoImagenLocalValido
		private static bool EsArchivoImagenLocalValido(string ruta)
		{
			if (string.IsNullOrWhiteSpace(ruta) || !Path.IsPathRooted(ruta) || !File.Exists(ruta))
			{
				return false;
			}

			string extension = Path.GetExtension(ruta);
			return ExtensionesImagenPermitidas.Contains(extension);
		}

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - btnAgregarCategoria_Click
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

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - btnEliminarCategoria_Click
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

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - ConfigurarCacheFormulario
		private void ConfigurarCacheFormulario()
		{
			txtNombre.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtMarca.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
			cmbCategoria.SelectionChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtNuevaCategoria.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtPrecioVenta.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtStock.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
			txtImagen.TextChanged += (s, e) => GuardarFormularioNuevoEnCache();
		}

		// esta seccion sirve para cargar informacion de el inventario y preparar lo que se muestra en pantalla - CargarFormularioNuevoDesdeCache
		private void CargarFormularioNuevoDesdeCache()
		{
			if (!SessionService.PuedeGestionarInventario || productoSeleccionado != null)
			{
				return;
			}

			cargandoCacheProducto = true;
			txtNombre.Text = App.FormCacheService.GetValue(CacheNombre) ?? txtNombre.Text;
			txtMarca.Text = App.FormCacheService.GetValue(CacheMarca) ?? txtMarca.Text;
			SeleccionarCategoria(App.FormCacheService.GetValue(CacheCategoria));
			txtNuevaCategoria.Text = App.FormCacheService.GetValue(CacheNuevaCategoria) ?? txtNuevaCategoria.Text;
			txtPrecioVenta.Text = App.FormCacheService.GetValue(CachePrecio) ?? txtPrecioVenta.Text;
			txtStock.Text = App.FormCacheService.GetValue(CacheStock) ?? txtStock.Text;
			txtImagen.Text = App.FormCacheService.GetValue(CacheImagen) ?? txtImagen.Text;
			cargandoCacheProducto = false;
		}

		// esta seccion sirve para guardar informacion de el inventario y mantener los datos persistidos - GuardarFormularioNuevoEnCache
		private void GuardarFormularioNuevoEnCache()
		{
			if (cargandoCacheProducto || !SessionService.PuedeGestionarInventario || productoSeleccionado != null)
			{
				return;
			}

			if (FormularioVacio())
			{
				LimpiarCacheFormularioNuevo();
				return;
			}

			GuardarValorCache(CacheNombre, txtNombre.Text);
			GuardarValorCache(CacheMarca, txtMarca.Text);
			GuardarValorCache(CacheCategoria, ObtenerCategoriaActual());
			GuardarValorCache(CacheNuevaCategoria, txtNuevaCategoria.Text);
			GuardarValorCache(CachePrecio, txtPrecioVenta.Text);
			GuardarValorCache(CacheStock, txtStock.Text);
			GuardarValorCache(CacheImagen, txtImagen.Text);
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - SeleccionarCategoria
		private void SeleccionarCategoria(string? categoria)
		{
			if (string.IsNullOrWhiteSpace(categoria))
			{
				return;
			}

			cmbCategoria.SelectedItem = cmbCategoria.Items
				.Cast<string>()
				.FirstOrDefault(c => c.Equals(categoria, StringComparison.OrdinalIgnoreCase));
		}

		// esta seccion sirve para guardar informacion de el inventario y mantener los datos persistidos - GuardarValorCache
		private static void GuardarValorCache(string clave, string valor)
		{
			if (string.IsNullOrEmpty(valor))
			{
				App.FormCacheService.RemoveValue(clave);
				return;
			}

			App.FormCacheService.SetValue(clave, valor);
		}

		// esta seccion sirve para quitar informacion de el inventario y dejar el estado consistente - LimpiarCacheFormularioNuevo
		private static void LimpiarCacheFormularioNuevo()
		{
			App.FormCacheService.ClearPrefix(CachePrefixProductoNuevo);
		}

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - txtBuscar_TextChanged
		private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
		{
			AplicarFiltro();
		}

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - FiltroProductos_Changed
		private void FiltroProductos_Changed(object sender, object e)
		{
			if (ignorarCambiosFiltro)
			{
				return;
			}

			AplicarFiltro();
		}

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - btnLimpiarFiltrosProductos_Click
		private void btnLimpiarFiltrosProductos_Click(object sender, RoutedEventArgs e)
		{
			ignorarCambiosFiltro = true;
			txtBuscar.Text = string.Empty;
			txtPrecioMin.Text = string.Empty;
			txtPrecioMax.Text = string.Empty;
			cmbFiltroCategoria.SelectedItem = "Todas";
			cmbOrdenProductos.SelectedIndex = 0;
			ignorarCambiosFiltro = false;
			AplicarFiltro();
		}

		// esta seccion sirve para responder a la accion del usuario en el inventario y mover el flujo al siguiente paso - btnSeleccionarImagen_Click
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

			if (!EsArchivoImagenLocalValido(archivo.Path))
			{
				MostrarMensaje("Selecciona un archivo local JPG, JPEG, PNG o WEBP", false);
				return;
			}

			try
			{
				string rutaProyecto = ImageAssetService.CopiarImagenAProyecto(archivo.Path);
				if (string.IsNullOrWhiteSpace(rutaProyecto))
				{
					MostrarMensaje("No se pudo guardar la imagen en assets", false);
					return;
				}

				txtImagen.Text = rutaProyecto;
				MostrarMensaje("Imagen guardada en assets correctamente", true);
			}
			catch
			{
				MostrarMensaje("No se pudo guardar la imagen en assets", false);
			}
		}

		// esta seccion sirve para mostrar mensajes o ventanas de el inventario para que el usuario entienda el estado - MostrarMensaje
		private void MostrarMensaje(string mensaje, bool esExito)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = (SolidColorBrush)Application.Current.Resources[esExito ? "WineSuccessBrush" : "WineDangerBrush"];
			txtMensaje.Visibility = Visibility.Visible;
		}

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - OcultarMensaje
		private void OcultarMensaje()
		{
			txtMensaje.Visibility = Visibility.Collapsed;
		}
	}

	// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - ProductoItemViewModel
	public class ProductoItemViewModel
	{
		public Producto Producto { get; }
		public string Nombre => Producto.Nombre ?? string.Empty;
		public string Marca => Producto.Marca ?? string.Empty;
		public string Categoria => Producto.Categoria ?? string.Empty;
		public string IdTexto => $"ID: {(Producto.Id?.Length > 8 ? Producto.Id[..8] : Producto.Id)}";
		public string PrecioTexto => Producto.PrecioVenta.ToString("C");
		public string StockTexto => Producto.Stock.ToString();
		public string EstadoTexto => Producto.Stock <= 0
			? "Sin stock disponible"
			: Producto.Stock < 5
				? $"Stock bajo: {Producto.Stock}"
				: Producto.Activo ? "Activo" : "Inactivo";
		public SolidColorBrush EstadoBrush => (SolidColorBrush)Application.Current.Resources[
			Producto.Stock <= 0 || Producto.Stock < 5 ? "WineDangerBrush" : "WineMutedBrush"];

		// esta seccion sirve para manejar el inventario y concentrar aqui esta parte del flujo - ProductoItemViewModel
		public ProductoItemViewModel(Producto producto)
		{
			Producto = producto;
		}
	}
}
