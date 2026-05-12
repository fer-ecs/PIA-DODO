using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar el analisis del supervisor y dejar esa responsabilidad en un solo archivo - AnalisisSupervisorView
	public sealed partial class AnalisisSupervisorView : Page
	{
		private readonly ObservableCollection<AnalisisFila> productosTop = new();
		private readonly ObservableCollection<AnalisisFila> categoriasResumen = new();
		private readonly ObservableCollection<AnalisisFila> empleadosResumen = new();
		private readonly ObservableCollection<AnalisisFila> pagosResumen = new();
		private readonly ObservableCollection<AnalisisFila> inventarioAlertas = new();

		private List<Venta> ventasBase = new();
		private List<VentaLinea> lineasFiltradas = new();
		private List<Producto> productosBase = new();
		private bool cargandoFiltros;

		// esta seccion sirve para agrupar el analisis del supervisor y dejar esa responsabilidad en un solo archivo - AnalisisSupervisorView
		public AnalisisSupervisorView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtBuscarAnalisis);

			lvProductosTop.ItemsSource = productosTop;
			lvCategorias.ItemsSource = categoriasResumen;
			lvEmpleados.ItemsSource = empleadosResumen;
			lvPagos.ItemsSource = pagosResumen;
			lvInventario.ItemsSource = inventarioAlertas;

			if (!SessionService.PuedeVerAnalisisSupervisor)
			{
				MostrarEstado("Solo supervision puede consultar este modulo", false);
				return;
			}

			CargarDatos();
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarDatos
		private void CargarDatos()
		{
			ventasBase = DataService.ObtenerVentas();
			productosBase = DataService.ObtenerProductos();
			CargarCombos();
			AplicarAnalisis();
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarCombos
		private void CargarCombos()
		{
			cargandoFiltros = true;

			cmbPeriodo.SelectedIndex = 0;
			cmbOrdenProductos.SelectedIndex = 0;

			cmbCategoria.Items.Clear();
			cmbCategoria.Items.Add("Todas");
			var categorias = productosBase
				.Select(p => NormalizarTexto(p.Categoria, "Sin categoria"))
				.Concat(ventasBase.SelectMany(v => v.Productos.Select(p => NormalizarTexto(p.Producto.Categoria, "Sin categoria"))))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x)
				.ToList();

			foreach (string categoria in categorias)
			{
				cmbCategoria.Items.Add(categoria);
			}

			cmbEmpleado.Items.Clear();
			cmbEmpleado.Items.Add("Todos");
			var empleados = ventasBase
				.Select(v => NormalizarTexto(v.NombreEmpleado, "Empleado sin nombre"))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x)
				.ToList();

			foreach (string empleado in empleados)
			{
				cmbEmpleado.Items.Add(empleado);
			}

			cmbCategoria.SelectedIndex = 0;
			cmbEmpleado.SelectedIndex = 0;
			cargandoFiltros = false;
		}

		// esta seccion sirve para ordenar y ajustar datos de el analisis del supervisor para trabajar con valores limpios - AplicarAnalisis
		private void AplicarAnalisis()
		{
			string busqueda = (txtBuscarAnalisis.Text ?? string.Empty).Trim().ToLowerInvariant();
			string periodo = ObtenerTextoCombo(cmbPeriodo);
			string categoria = ObtenerTextoCombo(cmbCategoria);
			string empleado = ObtenerTextoCombo(cmbEmpleado);
			string ordenProductos = ObtenerTextoCombo(cmbOrdenProductos);

			var ventasPeriodo = FiltrarPorPeriodo(ventasBase, periodo)
				.Where(v => empleado == "Todos" || NormalizarTexto(v.NombreEmpleado, "Empleado sin nombre").Equals(empleado, StringComparison.OrdinalIgnoreCase))
				.ToList();

			var lineas = ventasPeriodo
				.SelectMany(v => v.Productos.Select(p => new VentaLinea(v, p)))
				.Where(l => categoria == "Todas" || NormalizarTexto(l.Item.Producto.Categoria, "Sin categoria").Equals(categoria, StringComparison.OrdinalIgnoreCase))
				.Where(l => CoincideBusqueda(l, busqueda))
				.ToList();

			lineasFiltradas = lineas;
			var ventasAnalizadas = lineas
				.GroupBy(l => l.Venta.Id)
				.Select(g => g.First().Venta)
				.ToList();

			double ingresos = lineas.Sum(l => l.Item.Subtotal);
			int unidades = lineas.Sum(l => l.Item.Cantidad);
			int totalVentas = ventasAnalizadas.Count;
			double ticketPromedio = totalVentas == 0 ? 0 : ingresos / totalVentas;
			double margen = lineas.Sum(l => Math.Max(0, l.Item.Producto.PrecioVenta - l.Item.Producto.PrecioCompra) * l.Item.Cantidad);

			txtTotalVentas.Text = totalVentas.ToString(CultureInfo.CurrentCulture);
			txtIngresos.Text = ingresos.ToString("C", CultureInfo.CurrentCulture);
			txtProductosVendidos.Text = unidades.ToString(CultureInfo.CurrentCulture);
			txtTicketPromedio.Text = ticketPromedio.ToString("C", CultureInfo.CurrentCulture);
			txtMargenEstimado.Text = margen.ToString("C", CultureInfo.CurrentCulture);

			CargarProductosTop(lineas, ordenProductos);
			CargarCategorias(lineas);
			CargarEmpleados(lineas);
			CargarPagos(lineas);
			CargarInventario();
			CargarLecturasRapidas(lineas, ventasAnalizadas);
			CargarPredicciones(lineas, ventasAnalizadas, periodo);

			txtResumenFiltro.Text = $"{totalVentas} venta(s) analizadas | {unidades} unidad(es)";
			MostrarEstado("Analisis actualizado", true);
		}

		// esta seccion sirve para ordenar y ajustar datos de el analisis del supervisor para trabajar con valores limpios - FiltrarPorPeriodo
		private static IEnumerable<Venta> FiltrarPorPeriodo(IEnumerable<Venta> ventas, string periodo)
		{
			DateTime hoy = DateTime.Now.Date;
			DateTime? desde = periodo switch
			{
				"Ultimos 7 dias" => hoy.AddDays(-6),
				"Ultimos 30 dias" => hoy.AddDays(-29),
				"Ultimos 90 dias" => hoy.AddDays(-89),
				_ => null
			};

			return desde == null ? ventas : ventas.Where(v => v.Fecha.Date >= desde.Value);
		}

		// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - CoincideBusqueda
		private static bool CoincideBusqueda(VentaLinea linea, string busqueda)
		{
			if (string.IsNullOrWhiteSpace(busqueda))
			{
				return true;
			}

			var producto = linea.Item.Producto;
			string texto = string.Join(" ", new[]
			{
				producto.Id,
				producto.Nombre,
				producto.Marca,
				producto.Categoria,
				linea.Venta.Id,
				linea.Venta.NombreEmpleado,
				linea.Venta.MetodoPago
			}).ToLowerInvariant();

			return texto.Contains(busqueda, StringComparison.OrdinalIgnoreCase);
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarProductosTop
		private void CargarProductosTop(List<VentaLinea> lineas, string orden)
		{
			productosTop.Clear();
			var resumen = lineas
				.GroupBy(l => l.Item.Producto.Id)
				.Select(g =>
				{
					var item = g.First().Item.Producto;
					int unidades = g.Sum(x => x.Item.Cantidad);
					double ingresos = g.Sum(x => x.Item.Subtotal);

					return new ProductoResumen
					{
						Nombre = NormalizarTexto(item.Nombre, "Producto sin nombre"),
						Detalle = $"{NormalizarTexto(item.Marca, "Sin marca")} | {NormalizarTexto(item.Categoria, "Sin categoria")} | Stock {item.Stock}",
						Unidades = unidades,
						Ingresos = ingresos,
						Stock = item.Stock
					};
				});

			resumen = orden switch
			{
				"Mas ingresos" => resumen.OrderByDescending(p => p.Ingresos).ThenBy(p => p.Nombre),
				"Menor stock" => resumen.OrderBy(p => p.Stock).ThenByDescending(p => p.Unidades),
				"Nombre A-Z" => resumen.OrderBy(p => p.Nombre),
				_ => resumen.OrderByDescending(p => p.Unidades).ThenByDescending(p => p.Ingresos)
			};

			var productos = resumen.Take(8).ToList();
			double maximo = productos.Count == 0 ? 1 : Math.Max(1, productos.Max(p => orden == "Mas ingresos" ? p.Ingresos : p.Unidades));

			foreach (var producto in productos)
			{
				double basePorcentaje = orden == "Mas ingresos" ? producto.Ingresos : producto.Unidades;
				productosTop.Add(new AnalisisFila
				{
					Titulo = producto.Nombre,
					Detalle = $"{producto.Detalle} | {producto.Unidades} unidad(es)",
					Importe = producto.Ingresos.ToString("C", CultureInfo.CurrentCulture),
					Porcentaje = Math.Round(basePorcentaje / maximo * 100, 1)
				});
			}

			if (productosTop.Count == 0)
			{
				productosTop.Add(FilaVacia("Sin productos en el filtro"));
			}
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarCategorias
		private void CargarCategorias(List<VentaLinea> lineas)
		{
			categoriasResumen.Clear();
			double maximo = Math.Max(1, lineas.GroupBy(l => NormalizarTexto(l.Item.Producto.Categoria, "Sin categoria")).Select(g => g.Sum(x => x.Item.Subtotal)).DefaultIfEmpty(0).Max());

			foreach (var categoria in lineas
				.GroupBy(l => NormalizarTexto(l.Item.Producto.Categoria, "Sin categoria"))
				.Select(g => new
				{
					Nombre = g.Key,
					Unidades = g.Sum(x => x.Item.Cantidad),
					Ingresos = g.Sum(x => x.Item.Subtotal)
				})
				.OrderByDescending(x => x.Ingresos))
			{
				categoriasResumen.Add(new AnalisisFila
				{
					Titulo = categoria.Nombre,
					Detalle = $"{categoria.Unidades} unidad(es) vendida(s)",
					Importe = categoria.Ingresos.ToString("C", CultureInfo.CurrentCulture),
					Porcentaje = Math.Round(categoria.Ingresos / maximo * 100, 1)
				});
			}

			if (categoriasResumen.Count == 0)
			{
				categoriasResumen.Add(FilaVacia("Sin categorias en el filtro"));
			}
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarEmpleados
		private void CargarEmpleados(List<VentaLinea> lineas)
		{
			empleadosResumen.Clear();
			foreach (var empleado in lineas
				.GroupBy(l => NormalizarTexto(l.Venta.NombreEmpleado, "Empleado sin nombre"))
				.Select(g => new
				{
					Nombre = g.Key,
					Ventas = g.GroupBy(x => x.Venta.Id).Count(),
					Unidades = g.Sum(x => x.Item.Cantidad),
					Ingresos = g.Sum(x => x.Item.Subtotal)
				})
				.OrderByDescending(x => x.Ingresos)
				.ThenByDescending(x => x.Ventas)
				.Take(6))
			{
				empleadosResumen.Add(new AnalisisFila
				{
					Titulo = empleado.Nombre,
					Detalle = $"{empleado.Ventas} venta(s) | {empleado.Unidades} unidad(es)",
					Importe = empleado.Ingresos.ToString("C", CultureInfo.CurrentCulture),
					Porcentaje = 0
				});
			}

			if (empleadosResumen.Count == 0)
			{
				empleadosResumen.Add(FilaVacia("Sin ventas por empleado"));
			}
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarPagos
		private void CargarPagos(List<VentaLinea> lineas)
		{
			pagosResumen.Clear();
			double total = Math.Max(1, lineas.Sum(l => l.Item.Subtotal));

			foreach (var pago in lineas
				.GroupBy(l => NormalizarTexto(l.Venta.MetodoPago, "Sin metodo"))
				.Select(g => new
				{
					Nombre = g.Key,
					Ventas = g.GroupBy(x => x.Venta.Id).Count(),
					Ingresos = g.Sum(x => x.Item.Subtotal)
				})
				.OrderByDescending(x => x.Ingresos))
			{
				pagosResumen.Add(new AnalisisFila
				{
					Titulo = pago.Nombre,
					Detalle = $"{pago.Ventas} venta(s)",
					Importe = pago.Ingresos.ToString("C", CultureInfo.CurrentCulture),
					Porcentaje = Math.Round(pago.Ingresos / total * 100, 1)
				});
			}

			if (pagosResumen.Count == 0)
			{
				pagosResumen.Add(FilaVacia("Sin metodos de pago"));
			}
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarInventario
		private void CargarInventario()
		{
			inventarioAlertas.Clear();
			var activos = productosBase.Where(p => p.Activo).ToList();
			double valorInventario = activos.Sum(p => p.Stock * p.PrecioVenta);
			var alertas = activos
				.Where(p => p.Stock < 5)
				.OrderBy(p => p.Stock)
				.ThenBy(p => p.Nombre)
				.Take(8)
				.ToList();

			txtProductosActivos.Text = $"Productos activos: {activos.Count}";
			txtValorInventario.Text = $"Valor en stock: {valorInventario.ToString("C", CultureInfo.CurrentCulture)}";
			txtStockCritico.Text = $"Stock critico: {alertas.Count}";

			foreach (var producto in alertas)
			{
				string estado = producto.Stock <= 0 ? "No hay stock disponible" : "Stock bajo";
				inventarioAlertas.Add(new AnalisisFila
				{
					Titulo = NormalizarTexto(producto.Nombre, "Producto sin nombre"),
					Detalle = $"{NormalizarTexto(producto.Categoria, "Sin categoria")} | {NormalizarTexto(producto.Marca, "Sin marca")}",
					Importe = $"{estado}: {producto.Stock}",
					Porcentaje = 0
				});
			}

			if (inventarioAlertas.Count == 0)
			{
				inventarioAlertas.Add(new AnalisisFila
				{
					Titulo = "Inventario estable",
					Detalle = "No hay productos por debajo de 5 unidades",
					Importe = "Sin alertas",
					Porcentaje = 0
				});
			}
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarLecturasRapidas
		private void CargarLecturasRapidas(List<VentaLinea> lineas, List<Venta> ventasAnalizadas)
		{
			var productoTop = lineas
				.GroupBy(l => NormalizarTexto(l.Item.Producto.Nombre, "Producto sin nombre"))
				.Select(g => new { Nombre = g.Key, Unidades = g.Sum(x => x.Item.Cantidad), Ingresos = g.Sum(x => x.Item.Subtotal) })
				.OrderByDescending(x => x.Unidades)
				.ThenByDescending(x => x.Ingresos)
				.FirstOrDefault();

			var empleadoTop = lineas
				.GroupBy(l => NormalizarTexto(l.Venta.NombreEmpleado, "Empleado sin nombre"))
				.Select(g => new { Nombre = g.Key, Ventas = g.GroupBy(x => x.Venta.Id).Count(), Total = g.Sum(x => x.Item.Subtotal) })
				.OrderByDescending(x => x.Total)
				.ThenByDescending(x => x.Ventas)
				.FirstOrDefault();

			var diaTop = ventasAnalizadas
				.GroupBy(v => v.Fecha.Date)
				.Select(g => new { Fecha = g.Key, Total = g.Sum(v => v.Total), Ventas = g.Count() })
				.OrderByDescending(x => x.Total)
				.FirstOrDefault();

			txtProductoTop.Text = productoTop == null
				? "Producto lider: sin datos"
				: $"Producto lider: {productoTop.Nombre} ({productoTop.Unidades} unidad(es), {productoTop.Ingresos.ToString("C", CultureInfo.CurrentCulture)})";

			txtEmpleadoTop.Text = empleadoTop == null
				? "Empleado lider: sin datos"
				: $"Empleado lider: {empleadoTop.Nombre} ({empleadoTop.Ventas} venta(s), {empleadoTop.Total.ToString("C", CultureInfo.CurrentCulture)})";

			txtDiaTop.Text = diaTop == null
				? "Mejor dia: sin datos"
				: $"Mejor dia: {diaTop.Fecha:dd/MM/yyyy} ({diaTop.Ventas} venta(s), {diaTop.Total.ToString("C", CultureInfo.CurrentCulture)})";
		}

		// esta seccion sirve para cargar informacion de el analisis del supervisor y preparar lo que se muestra en pantalla - CargarPredicciones
		private void CargarPredicciones(List<VentaLinea> lineas, List<Venta> ventasAnalizadas, string periodo)
		{
			int dias = CalcularDiasAnalizados(ventasAnalizadas, periodo);
			double ingresos = lineas.Sum(l => l.Item.Subtotal);
			int unidades = lineas.Sum(l => l.Item.Cantidad);
			double ingresosSemana = ingresos / dias * 7;
			double unidadesSemana = unidades / (double)dias * 7;
			int sinStock = productosBase.Count(p => p.Activo && p.Stock <= 0);
			int bajoStock = productosBase.Count(p => p.Activo && p.Stock > 0 && p.Stock < 5);

			txtPrediccionIngresos.Text = $"Proyeccion semanal: {ingresosSemana.ToString("C", CultureInfo.CurrentCulture)}";
			txtPrediccionProductos.Text = $"Productos esperados: {Math.Round(unidadesSemana, 1)} unidad(es) en 7 dias";

			if (sinStock > 0)
			{
				txtRiesgoStock.Text = $"Riesgo de stock: {sinStock} sin stock y {bajoStock} por debajo de 5";
			}
			else if (bajoStock > 0)
			{
				txtRiesgoStock.Text = $"Riesgo de stock: {bajoStock} producto(s) por debajo de 5";
			}
			else
			{
				txtRiesgoStock.Text = "Riesgo de stock: sin alertas";
			}
		}

		// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - CalcularDiasAnalizados
		private static int CalcularDiasAnalizados(List<Venta> ventas, string periodo)
		{
			return periodo switch
			{
				"Ultimos 7 dias" => 7,
				"Ultimos 30 dias" => 30,
				"Ultimos 90 dias" => 90,
				_ when ventas.Count > 0 => Math.Max(1, (ventas.Max(v => v.Fecha.Date) - ventas.Min(v => v.Fecha.Date)).Days + 1),
				_ => 1
			};
		}

		// esta seccion sirve para responder a la accion del usuario en el analisis del supervisor y mover el flujo al siguiente paso - btnExportarExcel_Click
		private async void btnExportarExcel_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeVerAnalisisSupervisor)
			{
				MostrarEstado("No tienes permiso para exportar este reporte", false);
				return;
			}

			if (App.VentanaPrincipal == null)
			{
				MostrarEstado("No se pudo abrir el selector para guardar", false);
				return;
			}

			try
			{
				var picker = new FileSavePicker
				{
					SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
					DefaultFileExtension = ".xls",
					SuggestedFileName = $"analisis-supervisor-vinoteca-{DateTime.Now:yyyyMMdd-HHmmss}"
				};

				picker.FileTypeChoices.Add("Libro de Excel", new List<string> { ".xls" });
				picker.FileTypeChoices.Add("Documento HTML para Excel", new List<string> { ".html" });
				InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.VentanaPrincipal));

				var archivo = await picker.PickSaveFileAsync();
				if (archivo == null)
				{
					MostrarEstado("Exportacion cancelada", false);
					return;
				}

				await System.IO.File.WriteAllTextAsync(archivo.Path, ConstruirExcelHtml(), new UTF8Encoding(true));
				MostrarEstado($"Reporte Excel guardado en: {archivo.Path}", true);
			}
			catch (Exception ex)
			{
				MostrarEstado($"No se pudo exportar el reporte: {ex.Message}", false);
			}
		}

		// esta seccion sirve para armar datos o contenido de el analisis del supervisor y devolverlo ya preparado - ConstruirExcelHtml
		private string ConstruirExcelHtml()
		{
			var sb = new StringBuilder();

			sb.AppendLine("<html>");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"utf-8\">");
			sb.AppendLine("<style>");
			sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;color:#1f1f24;background:#ffffff}");
			sb.AppendLine("h1{background:#8b2036;color:#ffffff;padding:18px 22px;margin:0 0 14px 0;font-size:24px}");
			sb.AppendLine("h2{color:#8b2036;margin:24px 0 8px 0;font-size:17px}");
			sb.AppendLine("table{border-collapse:collapse;width:100%;margin-bottom:18px}");
			sb.AppendLine("th{background:#8b2036;color:#ffffff;text-align:left;font-weight:700;padding:9px;border:1px solid #6f1829}");
			sb.AppendLine("td{padding:8px;border:1px solid #e3d6d8;vertical-align:top}");
			sb.AppendLine(".meta td{background:#fbf6f3}");
			sb.AppendLine(".kpi td{background:#f4e8e9;font-size:16px;font-weight:700}");
			sb.AppendLine(".section{background:#fbf6f3;font-weight:700;color:#8b2036}");
			sb.AppendLine(".money{mso-number-format:'\\0022$\\0022#,##0.00';text-align:right}");
			sb.AppendLine(".number{text-align:right}");
			sb.AppendLine("</style>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<h1>Analisis supervisor Vinoteca</h1>");

			sb.AppendLine("<table class=\"meta\">");
			AgregarFila(sb, "Generado", DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture));
			AgregarFila(sb, "Periodo", ObtenerTextoCombo(cmbPeriodo));
			AgregarFila(sb, "Categoria", ObtenerTextoCombo(cmbCategoria));
			AgregarFila(sb, "Empleado", ObtenerTextoCombo(cmbEmpleado));
			AgregarFila(sb, "Busqueda", string.IsNullOrWhiteSpace(txtBuscarAnalisis.Text) ? "Sin busqueda" : txtBuscarAnalisis.Text.Trim());
			sb.AppendLine("</table>");

			sb.AppendLine("<h2>Resumen ejecutivo</h2>");
			sb.AppendLine("<table class=\"kpi\"><tr><th>Ventas</th><th>Ingresos</th><th>Unidades</th><th>Ticket promedio</th><th>Margen estimado</th></tr>");
			sb.Append("<tr>");
			AgregarCelda(sb, txtTotalVentas.Text);
			AgregarCelda(sb, txtIngresos.Text);
			AgregarCelda(sb, txtProductosVendidos.Text);
			AgregarCelda(sb, txtTicketPromedio.Text);
			AgregarCelda(sb, txtMargenEstimado.Text);
			sb.AppendLine("</tr></table>");

			sb.AppendLine("<h2>Predicciones e inventario</h2>");
			sb.AppendLine("<table>");
			AgregarFila(sb, "Proyeccion ingresos", txtPrediccionIngresos.Text);
			AgregarFila(sb, "Proyeccion productos", txtPrediccionProductos.Text);
			AgregarFila(sb, "Riesgo de stock", txtRiesgoStock.Text);
			AgregarFila(sb, "Productos activos", txtProductosActivos.Text);
			AgregarFila(sb, "Valor inventario", txtValorInventario.Text);
			AgregarFila(sb, "Stock critico", txtStockCritico.Text);
			sb.AppendLine("</table>");

			sb.AppendLine("<h2>Lectura rapida</h2>");
			sb.AppendLine("<table>");
			AgregarFila(sb, "Producto lider", txtProductoTop.Text);
			AgregarFila(sb, "Empleado lider", txtEmpleadoTop.Text);
			AgregarFila(sb, "Mejor dia", txtDiaTop.Text);
			sb.AppendLine("</table>");

			AgregarTablaAnalisis(sb, "Productos destacados", productosTop);
			AgregarTablaAnalisis(sb, "Categorias", categoriasResumen);
			AgregarTablaAnalisis(sb, "Empleados", empleadosResumen);
			AgregarTablaAnalisis(sb, "Metodos de pago", pagosResumen);
			AgregarTablaAnalisis(sb, "Alertas de stock", inventarioAlertas);

			sb.AppendLine("<h2>Detalle de ventas</h2>");
			sb.AppendLine("<table><tr><th>Ticket</th><th>Fecha</th><th>Empleado</th><th>Producto</th><th>Cantidad</th><th>Subtotal</th></tr>");
			foreach (var linea in lineasFiltradas.OrderBy(l => l.Venta.Fecha).ThenBy(l => l.Venta.Id))
			{
				sb.Append("<tr>");
				AgregarCelda(sb, linea.Venta.Id);
				AgregarCelda(sb, linea.Venta.Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture));
				AgregarCelda(sb, NormalizarTexto(linea.Venta.NombreEmpleado, "Empleado sin nombre"));
				AgregarCelda(sb, NormalizarTexto(linea.Item.Producto.Nombre, "Producto sin nombre"));
				AgregarCelda(sb, linea.Item.Cantidad.ToString(CultureInfo.CurrentCulture), "number");
				AgregarCelda(sb, linea.Item.Subtotal.ToString("C", CultureInfo.CurrentCulture), "money");
				sb.AppendLine("</tr>");
			}

			if (lineasFiltradas.Count == 0)
			{
				sb.AppendLine("<tr><td colspan=\"6\">Sin ventas para los filtros seleccionados</td></tr>");
			}

			sb.AppendLine("</table>");
			sb.AppendLine("</body></html>");
			return sb.ToString();
		}

		// esta seccion sirve para agregar informacion a el analisis del supervisor y recalcular lo necesario - AgregarTablaAnalisis
		private static void AgregarTablaAnalisis(StringBuilder sb, string titulo, IEnumerable<AnalisisFila> filas)
		{
			sb.AppendLine($"<h2>{Celda(titulo)}</h2>");
			sb.AppendLine("<table><tr><th>Nombre</th><th>Detalle</th><th>Importe</th><th>Porcentaje</th></tr>");
			foreach (var fila in filas)
			{
				sb.Append("<tr>");
				AgregarCelda(sb, fila.Titulo);
				AgregarCelda(sb, fila.Detalle);
				AgregarCelda(sb, fila.Importe);
				AgregarCelda(sb, fila.Porcentaje <= 0 ? string.Empty : $"{fila.Porcentaje:0.##}%", "number");
				sb.AppendLine("</tr>");
			}

			sb.AppendLine("</table>");
		}

		// esta seccion sirve para agregar informacion a el analisis del supervisor y recalcular lo necesario - AgregarFila
		private static void AgregarFila(StringBuilder sb, string etiqueta, string valor)
		{
			sb.Append("<tr>");
			AgregarCelda(sb, etiqueta, "section");
			AgregarCelda(sb, valor);
			sb.AppendLine("</tr>");
		}

		// esta seccion sirve para agregar informacion a el analisis del supervisor y recalcular lo necesario - AgregarCelda
		private static void AgregarCelda(StringBuilder sb, string valor, string clase = "")
		{
			string atributoClase = string.IsNullOrWhiteSpace(clase) ? string.Empty : $" class=\"{clase}\"";
			sb.Append($"<td{atributoClase}>{Celda(valor)}</td>");
		}

		// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - Celda
		private static string Celda(string valor)
		{
			return WebUtility.HtmlEncode(valor ?? string.Empty);
		}

		// esta seccion sirve para responder a la accion del usuario en el analisis del supervisor y mover el flujo al siguiente paso - btnLimpiarFiltros_Click
		private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
		{
			cargandoFiltros = true;
			txtBuscarAnalisis.Text = string.Empty;
			cmbPeriodo.SelectedIndex = 0;
			cmbCategoria.SelectedIndex = 0;
			cmbEmpleado.SelectedIndex = 0;
			cmbOrdenProductos.SelectedIndex = 0;
			cargandoFiltros = false;
			AplicarAnalisis();
		}

		// esta seccion sirve para responder a la accion del usuario en el analisis del supervisor y mover el flujo al siguiente paso - FiltroAnalisis_Changed
		private void FiltroAnalisis_Changed(object sender, object e)
		{
			if (!cargandoFiltros && IsLoaded)
			{
				AplicarAnalisis();
			}
		}

		// esta seccion sirve para mostrar mensajes o ventanas de el analisis del supervisor para que el usuario entienda el estado - MostrarEstado
		private void MostrarEstado(string mensaje, bool correcto)
		{
			txtEstado.Text = mensaje;
			txtEstado.Foreground = (SolidColorBrush)Application.Current.Resources[correcto ? "WineSuccessBrush" : "WineDangerBrush"];
			txtEstado.Visibility = Visibility.Visible;
		}

		// esta seccion sirve para leer informacion de el analisis del supervisor y regresarla lista para usarse - ObtenerTextoCombo
		private static string ObtenerTextoCombo(ComboBox combo)
		{
			if (combo.SelectedItem is ComboBoxItem item)
			{
				return item.Content?.ToString() ?? string.Empty;
			}

			return combo.SelectedItem?.ToString() ?? string.Empty;
		}

		// esta seccion sirve para ordenar y ajustar datos de el analisis del supervisor para trabajar con valores limpios - NormalizarTexto
		private static string NormalizarTexto(string? valor, string respaldo)
		{
			return string.IsNullOrWhiteSpace(valor) ? respaldo : valor.Trim();
		}

		// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - FilaVacia
		private static AnalisisFila FilaVacia(string texto)
		{
			return new AnalisisFila
			{
				Titulo = texto,
				Detalle = "Ajusta los filtros o registra nuevas ventas",
				Importe = string.Empty,
				Porcentaje = 0
			};
		}

		// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - VentaLinea
		private sealed class VentaLinea
		{
			// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - VentaLinea
			public VentaLinea(Venta venta, CarritoItem item)
			{
				Venta = venta;
				Item = item;
			}

			public Venta Venta { get; }
			public CarritoItem Item { get; }
		}

		// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - ProductoResumen
		private sealed class ProductoResumen
		{
			public string Nombre { get; set; } = string.Empty;
			public string Detalle { get; set; } = string.Empty;
			public int Unidades { get; set; }
			public double Ingresos { get; set; }
			public int Stock { get; set; }
		}

		// esta seccion sirve para manejar el analisis del supervisor y concentrar aqui esta parte del flujo - AnalisisFila
		private sealed class AnalisisFila
		{
			public string Titulo { get; set; } = string.Empty;
			public string Detalle { get; set; } = string.Empty;
			public string Importe { get; set; } = string.Empty;
			public double Porcentaje { get; set; }
		}
	}
}
