# Guia de defensa del sistema Vinoteca

Este documento esta pensado para que el equipo pueda explicar el sistema con seguridad frente al maestro. La idea no es memorizar codigo linea por linea, sino entender por que se tomo cada decision, como fluye la informacion y que protecciones tiene la aplicacion.

## 1. Que es el sistema

Vinoteca es una aplicacion de escritorio para Windows construida con WinUI 3 y .NET 8. Su objetivo es administrar una vinoteca desde tres perspectivas principales:

- Administracion del negocio: usuarios, inventario y reportes administrativos.
- Supervision: analisis de ventas, rendimiento, inventario y exportacion de reportes.
- Operacion de venta: busqueda de productos, carrito, cobro, generacion de ticket y consulta de tickets emitidos.

El sistema no depende de una base de datos externa. Guarda la informacion en archivos JSON locales dentro de la carpeta del usuario:

```text
%LOCALAPPDATA%\Vinoteca\Data
```

Esto se hizo para que el proyecto pueda ejecutarse en otra computadora sin depender de una ruta fija de la maquina donde se desarrollo.

## 2. Stack y estructura general

El proyecto esta construido con:

- .NET 8
- WinUI 3
- Windows App SDK
- C#
- XAML
- Archivos JSON para persistencia local

Archivo principal del proyecto:

```text
Vinoteca.csproj
```

Puntos importantes del proyecto:

- `OutputType` es `WinExe`, por eso se ejecuta como aplicacion de escritorio.
- `TargetFramework` es `net8.0-windows10.0.19041.0`.
- `UseWinUI` esta activo.
- `WindowsPackageType` esta en `None`, por lo que la app se ejecuta como aplicacion WinUI no empaquetada.
- Se desactivo `PublishTrimmed` para evitar errores de publicacion con WinUI.
- Se usan plataformas `x86`, `x64` y `ARM64`.

Carpetas principales:

```text
Models      Entidades del sistema: Usuario, Producto, Venta, CarritoItem
Services    Logica compartida: datos, sesion, carrito, tickets, imagenes, cache
Helpers     Validaciones y restricciones reutilizables
Views       Pantallas principales de la aplicacion
Assets      Recursos visuales e imagenes de productos
```

## 3. Arranque de la aplicacion

El arranque ocurre en `MainWindow.xaml.cs`.

Cuando se abre la aplicacion:

1. Se inicializa la ventana principal.
2. Se activa el estilo visual con `MicaBackdrop`.
3. Se configura la ventana maximizada.
4. Se llama a `DataService.InicializarArchivos()`.
5. Se navega a `LoginView`.

La llamada a `DataService.InicializarArchivos()` es clave porque:

- Crea los archivos JSON si no existen.
- Crea el administrador principal si hace falta.
- Crea categorias base.
- Crea productos de ejemplo.
- Migra datos antiguos si se encontraban en otra ubicacion.

Esto permite que el sistema este listo para usarse desde el primer inicio.

## 4. Persistencia de datos

La persistencia esta centralizada en `Services/DataService.cs`.

Archivos usados:

```text
usuarios.json
productos.json
ventas.json
categorias.json
```

El sistema lee y escribe listas de objetos en JSON. Esto hace que:

- Los datos sobrevivan aunque se cierre la aplicacion.
- No se necesite instalar SQL Server, MySQL u otro motor.
- El proyecto sea mas facil de ejecutar en computadoras de escuela.

DataService tambien normaliza informacion antes de guardarla:

- Corrige IDs faltantes.
- Normaliza correos.
- Normaliza roles.
- Garantiza que exista un administrador activo.
- Evita que ventas antiguas queden incompletas.

## 5. Modelos principales

### Usuario

Archivo:

```text
Models/Usuario.cs
```

Campos principales:

- `Id`: identificador unico.
- `Nombre`: nombre visible.
- `Correo`: correo usado para iniciar sesion.
- `Contrasena`: contrasena guardada localmente.
- `Rol`: Administrador, Supervisor o Empleado.
- `Activo`: indica si la cuenta puede entrar.

Tambien conserva la propiedad `EsAdmin` por compatibilidad, pero el sistema moderno trabaja con `Rol`.

### Producto

Archivo:

```text
Models/Producto.cs
```

Campos principales:

- `Id`: tambien funciona como codigo corto.
- `Nombre`
- `Marca`
- `Categoria`
- `PrecioCompra`
- `PrecioVenta`
- `Stock`
- `Volumen`
- `PorcentajeAlcohol`
- `ImagenPath`
- `Activo`

El campo `Activo` permite ocultar o deshabilitar productos sin tener que borrarlos.

### Venta

Archivo:

```text
Models/Venta.cs
```

Campos principales:

- `Id`
- `Fecha`
- `UsuarioId`
- `EmpleadoId`
- `NombreEmpleado`
- `CorreoEmpleado`
- `MetodoPago`
- `MontoRecibido`
- `Cambio`
- `ReferenciaPago`
- `Productos`
- `Total`

Una venta guarda una copia de los productos vendidos, cantidades y subtotales. Esto es importante porque aunque despues cambie el precio o nombre de un producto, el ticket historico conserva lo vendido en ese momento.

### CarritoItem

Archivo:

```text
Models/CarritoItem.cs
```

Campos:

- `Producto`
- `Cantidad`
- `Subtotal`

El subtotal se calcula automaticamente:

```csharp
Producto.PrecioVenta * Cantidad
```

Eso evita guardar un valor duplicado que podria quedar desactualizado.

## 6. Sistema de roles

Los roles estan centralizados en `Services/RolesSistema.cs`.

Roles actuales:

- `Administrador`
- `Supervisor`
- `Empleado`

Tambien existe `Cliente`, pero esta mapeado internamente a `Empleado` para mantener compatibilidad con datos anteriores.

La normalizacion de roles evita errores como:

- Roles escritos con espacios.
- Roles vacios.
- Datos antiguos con `Cliente`.
- Roles desconocidos en archivos JSON.

Si el rol no es valido, se convierte en `Empleado` como valor seguro.

## 7. Sesion y permisos

La sesion esta en `Services/SessionService.cs`.

Este servicio guarda:

```text
UsuarioActivo
```

Con base en ese usuario calcula permisos:

| Permiso | Quien lo tiene | Para que sirve |
| --- | --- | --- |
| `EsAdministradorActivo` | Administrador activo | Acceso total administrativo |
| `EsSupervisorActivo` | Supervisor activo | Analisis y reportes de supervision |
| `EsEmpleadoActivo` | Empleado activo | Venta, carrito y tickets propios |
| `PuedeGestionarUsuarios` | Administrador | CRUD de usuarios |
| `PuedeGestionarInventario` | Administrador | CRUD de productos y categorias |
| `PuedeVerReportes` | Administrador | Historial administrativo de ventas |
| `PuedeVerAnalisisSupervisor` | Supervisor | Dashboard analitico |
| `PuedeComprar` | Empleado | Tienda, carrito y cobro |
| `PuedeProcesarVentas` | Empleado | Guardar ventas |

La ventaja de este diseño es que las pantallas no tienen que repetir condiciones complejas. Solo preguntan al servicio:

```csharp
SessionService.PuedeComprar
SessionService.PuedeGestionarUsuarios
SessionService.PuedeVerReportes
```

Eso hace que las reglas sean mas faciles de mantener.

## 8. Flujo de inicio de sesion

Archivo:

```text
LoginView.xaml.cs
```

El login valida en este orden:

1. Correo obligatorio.
2. Correo sin espacios al inicio o final.
3. Formato valido de correo.
4. Contrasena obligatoria.
5. Contrasena sin espacios al inicio o final.
6. Correo sin espacios internos.
7. Contrasena sin espacios internos.
8. Contrasena con minimo 6 caracteres.
9. Contrasena fuerte.
10. Usuario existente y contrasena correcta.
11. Cuenta activa.

La razon de este orden es pedagogica y practica: se muestra primero el error que el usuario puede entender y corregir mas rapido. Por ejemplo, si el correo esta mal escrito, no tiene sentido revisar primero la contrasena.

La contrasena fuerte exige:

- Una minuscula.
- Una mayuscula.
- Un numero.
- Un caracter especial.
- Sin espacios.

Tambien existe proteccion contra intentos fallidos:

- Maximo 3 intentos.
- Despues se bloquea por 5 minutos.

Esto no es seguridad bancaria, pero si demuestra control basico contra fuerza bruta en una aplicacion local.

## 9. Registro de usuarios desde pantalla publica

Archivo:

```text
RegisterView.xaml.cs
```

El registro permite crear una cuenta de empleado. Valida:

- Nombre obligatorio.
- Nombre sin espacios al inicio o final.
- Nombre minimo de 3 caracteres.
- Nombre sin numeros.
- Nombre solo con letras y espacios.
- Correo obligatorio.
- El usuario escribe solo la parte antes del `@`.
- Dominio seleccionado desde una lista permitida.
- Correo sin espacios.
- Nombre de correo maximo de 40 caracteres.
- Formato general de correo.
- Contrasena obligatoria.
- Confirmacion obligatoria.
- Contrasenas sin espacios.
- Minimo 6 caracteres.
- Contrasena fuerte.
- Contrasenas iguales.
- Correo no duplicado.

Dominios permitidos:

- gmail.com
- outlook.com
- yahoo.com
- hotmail.com
- live.com
- icloud.com

La pantalla usa `FormCacheService` para conservar temporalmente lo capturado. Si el usuario intenta salir con datos escritos, se le pregunta antes de perderlos.

## 10. Validaciones compartidas

Archivo:

```text
Helpers/FormValidationHelper.cs
```

Este helper concentra validaciones comunes:

- Formato de correo.
- Texto solo con letras y espacios.
- Correo no vacio.
- Longitud maxima de correo.
- Correo sin espacios.

La ventaja de tener un helper es que si se quiere cambiar una regla comun, no hay que buscarla en todas las pantallas.

## 11. Bloqueo de teclado e inserciones

Archivo:

```text
Helpers/InputRestrictionsHelper.cs
```

Esta es una de las partes mas importantes para defender el sistema.

El sistema no solo valida al presionar Guardar. Tambien bloquea y limpia entradas mientras el usuario escribe.

Tiene varios modos:

| Metodo | Uso |
| --- | --- |
| `AplicarSinEspaciosNiEnter` | Login, contrasenas, campos que no aceptan espacios |
| `AplicarSoloLetrasConEspacios` | Nombres, marcas, categorias |
| `AplicarSinEspacios` | Campos donde el espacio nunca es valido |
| `AplicarSoloNumeros` | Stock, cantidades, IDs |
| `AplicarSoloDecimal` | Precios, totales, montos |
| `AplicarTextoLibreSinEnter` | Busquedas y referencias |

### Como bloquea desde teclado

El helper escucha el evento `KeyDown`. Si detecta una tecla no permitida, marca:

```csharp
e.Handled = true;
```

Eso significa que la tecla se consume y no llega al control.

Ejemplos:

- Si el campo no permite espacios y el usuario presiona `Space`, se bloquea.
- Si el campo no permite saltos de linea y el usuario presiona `Enter`, se bloquea.
- Si un campo solo permite numeros, cualquier letra queda fuera despues de limpiar.

### Como limpia inserciones pegadas

Bloquear `KeyDown` no es suficiente, porque el usuario podria pegar texto con `Ctrl + V`. Por eso tambien se usan eventos como:

```text
TextChanged
PasswordChanged
```

Si el texto pegado trae caracteres invalidos, el helper reconstruye el texto dejando solo lo permitido.

Ejemplo:

```text
Entrada pegada: abc 12 ##
Campo numerico: 12
```

Esto demuestra una defensa en dos capas:

1. Prevencion al escribir.
2. Limpieza despues de pegar o modificar.

## 12. Por que se valida dos veces

El sistema valida en dos niveles:

1. Restriccion de entrada: evita que el usuario escriba cosas invalidas.
2. Validacion final: revisa todo antes de guardar, entrar o vender.

Esto es correcto porque la restriccion de teclado mejora la experiencia, pero no debe ser la unica defensa. La validacion final es la que realmente protege la informacion.

Respuesta recomendada si el maestro pregunta:

> Bloqueamos teclas para guiar al usuario, pero aun asi validamos al guardar porque los datos pueden llegar por pegado, por cambios del control o por archivos existentes. La validacion final es la capa obligatoria.

## 13. Usuarios y seguridad administrativa

Archivo:

```text
Views/UsuariosView.xaml.cs
```

Solo el administrador puede gestionar usuarios.

La pantalla incluye:

- Crear usuario.
- Editar usuario.
- Eliminar usuario.
- Activar o desactivar usuario.
- Buscar.
- Filtrar por rol.
- Filtrar por estado.
- Ordenar.

Validaciones de usuario:

- Nombre entre 3 y 50 caracteres.
- Nombre sin espacios dobles.
- Nombre sin numeros.
- Nombre solo con letras y espacios.
- Correo dividido en nombre local y dominio.
- Nombre de correo maximo 40 caracteres.
- Correo sin espacios.
- Dominio permitido.
- Correo no duplicado.
- Contrasena entre 8 y 20 caracteres.
- Contrasena fuerte.
- Confirmacion igual.
- Rol obligatorio.

Protecciones especiales:

- El administrador principal no se puede eliminar.
- El administrador principal no se puede desactivar.
- El administrador principal no puede perder su rol.
- Un usuario no puede eliminar su propia cuenta.
- Un usuario no puede desactivar su propia cuenta.
- Siempre debe existir al menos un administrador activo.

Estas reglas evitan que el sistema quede sin acceso administrativo.

## 14. Inventario

Archivo:

```text
Views/InventarioView.xaml.cs
```

Solo administracion puede gestionar inventario.

Funciones:

- Crear producto.
- Editar producto.
- Eliminar o desactivar producto.
- Crear categorias.
- Eliminar categorias si no estan en uso.
- Buscar productos.
- Filtrar por categoria.
- Filtrar por precio minimo y maximo.
- Ordenar productos.
- Agregar imagen al producto.

Validaciones de producto:

- Nombre entre 3 y 60 caracteres.
- Nombre solo con letras y espacios.
- Marca entre 2 y 40 caracteres.
- Categoria obligatoria.
- Precio mayor que 0.
- Precio menor o igual a 100000.
- Maximo 2 decimales.
- Stock entre 0 y 5000.
- Ruta de imagen maximo 300 caracteres.
- No repetir producto con misma combinacion de nombre y marca.

Validaciones de categoria:

- Nombre entre 3 y 30 caracteres.
- Solo letras y espacios.
- No duplicada.
- No se elimina si esta siendo usada por productos.

## 15. Manejo de imagenes

Archivo:

```text
Services/ImageAssetService.cs
```

Cuando se selecciona una imagen, no se guarda la ruta original de la computadora. El sistema copia la imagen a:

```text
Assets/productos
```

Esto se hizo porque una ruta como:

```text
C:\Users\Alumno\Pictures\vino.png
```

solo existe en una computadora. En cambio, si copiamos la imagen dentro del proyecto, el producto puede seguir mostrando su imagen en otra maquina.

El servicio tambien verifica que la imagen pertenezca a la carpeta del proyecto. Esto evita que se guarden rutas externas o inseguras.

Formatos aceptados:

- jpg
- jpeg
- png
- webp

## 16. Tienda y carrito

Archivos:

```text
Views/TiendaView.xaml.cs
Views/CarritoView.xaml.cs
Services/CarritoService.cs
```

La tienda solo esta disponible para empleados.

Funciones:

- Buscar productos.
- Filtrar por categoria.
- Ver productos con stock.
- Escanear o escribir codigo corto.
- Agregar al carrito.
- Ver resumen rapido del carrito.
- Pasar al cobro.

El carrito se guarda en memoria usando `CarritoService`. No se guarda en JSON porque representa una venta en proceso, no una venta terminada.

El carrito valida:

- Que el usuario tenga permiso de compra.
- Que el producto exista.
- Que el producto este activo.
- Que haya stock.
- Que la cantidad no supere el stock.

Tambien emite el evento:

```text
CarritoActualizado
```

Ese evento permite que la interfaz actualice contadores y totales automaticamente cuando se agregan, eliminan o cambian productos.

## 17. Cobro y generacion de venta

Archivo:

```text
Views/VentasAdminView.xaml.cs
```

Aunque el nombre del archivo dice `VentasAdminView`, el flujo de cobro esta pensado para empleados autorizados.

Metodos de pago:

- Efectivo.
- Tarjeta.
- Transferencia.

Reglas por metodo:

- En efectivo, el monto recibido debe cubrir el total.
- En efectivo, se calcula el cambio.
- En tarjeta, primero se cobra en terminal externa y se captura folio.
- En transferencia, se confirma deposito y se captura referencia.
- En tarjeta y transferencia, la referencia es obligatoria.

Antes de guardar la venta, se vuelve a revisar el stock contra los datos actuales. Esto es importante porque el carrito pudo haberse llenado antes, pero el inventario pudo cambiar despues.

Flujo de cobro:

1. Usuario confirma venta.
2. Se valida carrito.
3. Se valida metodo de pago.
4. Se revisa stock real actual.
5. Se crea objeto `Venta`.
6. Se guarda con `DataService.GuardarVenta`.
7. Se descuentan existencias.
8. Se guardan productos actualizados.
9. Se limpia carrito.
10. Se muestra vista previa del ticket.
11. Se navega a tickets.

Alertas de stock:

- Si queda en 0: sin stock disponible.
- Si queda debajo de 5: stock bajo.

## 18. Tickets

Archivos:

```text
Views/MisTicketsView.xaml.cs
Services/TicketPreviewService.cs
Services/TicketPdfService.cs
```

Cada venta genera un ticket.

La pantalla de tickets permite:

- Ver tickets emitidos por el empleado actual.
- Buscar por ID, metodo de pago, total, producto, marca o categoria.
- Ordenar por fecha, total o ID.
- Previsualizar.
- Descargar PDF.

El filtro usa:

```text
DataService.ObtenerVentasPorUsuario(SessionService.UsuarioActivo.Id)
```

Esto significa que el empleado ve sus propios tickets, no todos los del sistema.

## 19. Vista previa del ticket

Archivo:

```text
Services/TicketPreviewService.cs
```

La vista previa usa `ContentDialog` de WinUI.

Muestra:

- Marca Vinoteca.
- Logo.
- Numero de ticket.
- Fecha.
- Empleado.
- Correo.
- Productos.
- Cantidades.
- Subtotales.
- Subtotal sin IVA.
- IVA incluido 16%.
- Total pagado.
- Metodo.
- Recibido.
- Cambio.

La previsualizacion esta construida con controles WinUI, no como imagen estatica. Eso permite que se adapte al contenido.

## 20. Generacion de PDF

Archivo:

```text
Services/TicketPdfService.cs
```

El PDF se genera directamente desde C#, sin libreria externa.

El flujo es:

1. Se abre un `FileSavePicker`.
2. El usuario elige donde guardar.
3. Se construye el contenido del ticket.
4. Se escriben objetos PDF manualmente.
5. Se genera un archivo `%PDF-1.4`.

El servicio escribe:

- Catalogo del PDF.
- Pagina.
- Fuentes Helvetica y Helvetica-Bold.
- Contenido de texto y bloques visuales.
- Tabla de referencias `xref`.
- Trailer final.

Tambien calcula:

```text
Subtotal = Total / 1.16
IVA = Total - Subtotal
Total = venta.Total
```

Esto se usa tanto en PDF como en vista previa para que ambos tengan los mismos totales.

Limitacion importante para explicar:

El PDF usa codificacion ASCII y limpia caracteres fuera del rango basico. Esto simplifica la generacion manual del PDF, pero puede quitar acentos en el archivo generado. Para un proyecto escolar es aceptable porque evita depender de paquetes externos.

## 21. Reportes administrativos

Archivo:

```text
Views/ReportesView.xaml.cs
```

Solo administracion puede ver reportes administrativos.

Permite:

- Consultar todas las ventas.
- Buscar por ID, empleado, correo, cliente, metodo de pago, producto o marca.
- Filtrar por total minimo y maximo.
- Ordenar por fecha, total, empleado o ID.
- Ver ganancias totales.
- Ver total de ventas registradas.
- Ver lineas vendidas.
- Previsualizar ticket.
- Exportar ticket a PDF.

El objetivo de este modulo es auditoria. El administrador no solo ve sus ventas, sino el historial completo del negocio.

## 22. Analisis de supervisor

Archivo:

```text
Views/AnalisisSupervisorView.xaml.cs
```

Solo supervision puede entrar.

Este modulo es una innovacion importante porque convierte ventas e inventario en informacion util para tomar decisiones.

Incluye:

- Filtro por periodo.
- Filtro por categoria.
- Filtro por empleado.
- Busqueda libre.
- Orden de productos.
- Total de ventas.
- Ingresos.
- Productos vendidos.
- Ticket promedio.
- Margen estimado.
- Productos top.
- Resumen por categoria.
- Resumen por empleado.
- Resumen por metodo de pago.
- Alertas de inventario.
- Lecturas rapidas.
- Predicciones semanales.
- Exportacion a Excel.

El margen estimado se calcula asi:

```text
(PrecioVenta - PrecioCompra) * Cantidad
```

Las predicciones toman lo vendido en el periodo y lo proyectan a 7 dias.

Esto no es inteligencia artificial, pero si es analitica basica. La respuesta correcta si preguntan es:

> El sistema hace una proyeccion simple basada en promedio diario de ventas. No predice con machine learning; estima con los datos historicos filtrados.

## 23. Exportacion a Excel

Archivo:

```text
Views/AnalisisSupervisorView.xaml.cs
```

La exportacion de supervisor usa `FileSavePicker` y permite guardar:

- `.xls`
- `.html`

Internamente construye una tabla HTML con estilos. Excel puede abrir ese archivo como libro.

Se exporta:

- Datos de generacion.
- Periodo.
- Categoria.
- Empleado.
- Busqueda.
- Resumen ejecutivo.
- Predicciones.
- Inventario.
- Producto lider.
- Empleado lider.
- Mejor dia.
- Productos destacados.
- Categorias.
- Empleados.
- Metodos de pago.
- Alertas de stock.
- Detalle de ventas.

Ventaja:

- No requiere instalar librerias de Excel.
- Es compatible con una defensa escolar.
- El archivo puede abrirse en Excel.

## 24. Alertas y mensajes

El sistema maneja mensajes de varias formas:

### Mensajes visibles

En pantallas como login, registro, usuarios, inventario, tickets y reportes se usan `TextBlock` visibles u ocultos para mostrar errores o exito.

Ejemplos:

- Correo obligatorio.
- Contrasena debil.
- Usuario creado correctamente.
- Producto guardado correctamente.
- Guardado cancelado.
- Ticket guardado en una ruta.

### Dialogos de confirmacion

Archivo:

```text
Services/CambiosPendientesService.cs
```

Usa `ContentDialog` para confirmar acciones importantes:

- Salir con cambios pendientes.
- Cambiar de pantalla con formulario lleno.
- Vaciar carrito.
- Eliminar productos.
- Eliminar usuarios.
- Finalizar venta.

### Alertas de stock

En cobro y analisis se alerta cuando:

- El producto se queda sin stock.
- El producto queda por debajo de 5 unidades.

## 25. Cambios pendientes

Archivo:

```text
Services/CambiosPendientesService.cs
```

El sistema define una interfaz:

```csharp
public interface ICambiosPendientes
{
    bool TieneCambiosPendientes { get; }
    string ObtenerMensajeCambiosPendientes();
}
```

Pantallas como registro, usuarios e inventario implementan esta interfaz.

Esto permite que el sistema pregunte antes de perder informacion:

- Si hay campos llenos en un formulario.
- Si se esta editando un producto.
- Si se esta editando un usuario.
- Si hay productos en el carrito.

La ventaja es que la logica de confirmacion esta centralizada y no duplicada en cada pantalla.

## 26. Diseno visual

Archivo:

```text
App.xaml
```

El diseño usa recursos globales:

- `WineBackgroundBrush`
- `WinePanelBrush`
- `WineAccentBrush`
- `WineHeroBrush`
- `WineDangerBrush`
- `WineSuccessBrush`
- Estilos para botones.
- Estilos para TextBox.
- Estilos para PasswordBox.
- Estilos para ComboBox.
- Estilos para tarjetas.

La ventaja de centralizar estilos es que si se cambia la identidad visual, se puede ajustar desde `App.xaml` sin reescribir todas las pantallas.

El sistema usa una identidad visual relacionada con vinoteca:

- Colores vino.
- Paneles claros.
- Bordes suaves.
- Estados de error y exito diferenciados.

## 27. Innovaciones principales del sistema

Estas son buenas respuestas si el maestro pregunta que tiene de especial el proyecto:

1. Roles reales con permisos separados.
2. Rutas protegidas segun rol.
3. Validacion en dos capas: entrada y guardado.
4. Bloqueo de teclado y limpieza de texto pegado.
5. Control de intentos fallidos en login.
6. Administrador principal protegido.
7. Prevencion de que el sistema se quede sin administradores.
8. Datos portables en `%LOCALAPPDATA%`.
9. Migracion desde datos antiguos.
10. Inventario con categorias, imagenes y stock.
11. Copia controlada de imagenes al proyecto.
12. Carrito con evento de actualizacion.
13. Verificacion de stock antes de vender.
14. Alertas de stock bajo y sin stock.
15. Tickets por empleado.
16. Vista previa de ticket.
17. Generacion de PDF sin libreria externa.
18. Reportes administrativos.
19. Analisis de supervision.
20. Exportacion a Excel/HTML.
21. Confirmaciones por cambios pendientes.
22. Cache temporal de formulario de registro.
23. Diseño visual centralizado.
24. Datos de ejemplo para demostracion.

## 28. Preguntas probables del maestro y respuestas

### Por que no usaron base de datos?

Porque el objetivo del proyecto era una aplicacion local, portable y facil de ejecutar en computadoras escolares. Usamos JSON para persistencia local. Aun asi, centralizamos el acceso en `DataService`, por lo que en el futuro se podria cambiar a una base de datos sin reescribir todas las pantallas.

### Donde se guardan los datos?

En `%LOCALAPPDATA%\Vinoteca\Data`, dentro de archivos JSON. Esa ruta pertenece al usuario de Windows y evita depender de una carpeta fija del proyecto.

### Como evitan que cualquiera entre a pantallas de administrador?

Con `SessionService`. Cada pantalla revisa permisos como `PuedeGestionarUsuarios`, `PuedeGestionarInventario` o `PuedeVerReportes`. Si el usuario no tiene permiso, la pantalla se bloquea o redirige.

### Como funciona el sistema de roles?

Los roles estan definidos en `RolesSistema`: Administrador, Supervisor y Empleado. El usuario activo se guarda en `SessionService`, y desde ahi se calculan permisos. No se decide por texto suelto en cada pantalla, sino desde un servicio central.

### Por que bloquean letras o espacios desde teclado?

Para evitar errores antes de que ocurran y mejorar la captura. Por ejemplo, stock solo debe aceptar numeros, precio solo decimal y correo no debe tener espacios.

### Si alguien pega texto invalido, que pasa?

El sistema tambien escucha cambios de texto. Si se pega contenido invalido, se limpia dejando solo los caracteres permitidos. Por eso hay proteccion al escribir y al pegar.

### Por que validan otra vez al guardar si ya bloquearon el teclado?

Porque el bloqueo de teclado es solo una ayuda visual. La validacion final es la proteccion real. Los datos tambien pueden venir de pegado, archivos JSON o cambios internos.

### Como protegen al administrador principal?

En `UsuariosView` y `DataService`. El administrador principal no se puede eliminar, desactivar ni cambiar de rol. Ademas, siempre debe existir al menos un administrador activo.

### Como evitan vender mas stock del disponible?

Primero `CarritoService` valida al agregar productos. Despues, antes de finalizar venta, `VentasAdminView` vuelve a revisar el stock actual desde `DataService`. Esa segunda revision evita vender productos si el inventario cambio despues de llenar el carrito.

### Como se genera el ticket?

Cuando se cobra una venta, se crea un objeto `Venta`, se guarda, se descuenta stock, se limpia el carrito y se muestra una vista previa. Despues el ticket puede descargarse como PDF.

### El PDF usa una libreria?

No. `TicketPdfService` escribe manualmente la estructura basica del PDF. Usa objetos PDF, fuentes base, contenido, tabla `xref` y trailer.

### Por que el PDF puede perder acentos?

Porque se genera en ASCII para simplificar la escritura manual del archivo. Es una decision aceptable para evitar dependencias externas en el proyecto escolar.

### Como funciona la exportacion a Excel?

El supervisor genera un archivo `.xls` o `.html`. Internamente se construye HTML con tablas y estilos, y Excel puede abrirlo como libro.

### Que diferencia hay entre reportes y analisis?

Reportes administrativos muestran historial de ventas para auditoria y tickets PDF. Analisis de supervisor calcula indicadores: ingresos, ticket promedio, productos top, categorias, empleados, pagos, inventario y proyecciones.

### Que son cambios pendientes?

Son formularios o carritos con informacion no guardada. El sistema detecta eso con `ICambiosPendientes` y pregunta antes de salir o navegar.

### Que pasa si una cuenta esta desactivada?

Aunque el correo y contrasena sean correctos, el login rechaza el acceso y muestra que la cuenta fue desactivada.

### Que significa que el sistema es portable?

Que no depende de rutas de una computadora especifica. Guarda datos en LocalAppData, copia imagenes al proyecto y puede compilarse con comandos estandar de .NET.

## 29. Como explicar el flujo completo en una defensa

Una forma clara de explicarlo:

1. El usuario abre la app.
2. `MainWindow` inicializa archivos y manda a login.
3. El login valida correo, contrasena e intentos.
4. Si entra, `SessionService` guarda el usuario activo.
5. Segun su rol, se navega a admin, supervisor o empleado.
6. Admin gestiona usuarios, inventario y reportes.
7. Supervisor analiza ventas e inventario y exporta Excel.
8. Empleado vende productos desde tienda y carrito.
9. Al cobrar, se valida pago y stock.
10. Se guarda venta, se descuenta inventario y se genera ticket.
11. Los tickets se pueden previsualizar y exportar a PDF.

## 30. Frase final para defender el proyecto

El sistema no solo muestra pantallas. Tiene una estructura completa con roles, permisos, validaciones reutilizables, proteccion de datos, inventario, flujo de venta, tickets, PDF, reportes, analisis y exportaciones. Las reglas importantes estan centralizadas en servicios para que la aplicacion sea mas ordenada, mantenible y facil de explicar.

