using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class UsuariosView : Page
	{
		public ObservableCollection<UsuarioItemViewModel> Usuarios { get; } = new();

		public UsuariosView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			lvUsuarios.ItemsSource = Usuarios;
			CargarUsuarios();
		}

		private void CargarUsuarios()
		{
			Usuarios.Clear();

			foreach (var usuario in DataService.ObtenerUsuarios().OrderBy(u => u.Nombre))
			{
				Usuarios.Add(new UsuarioItemViewModel(usuario));
			}
		}

		private void btnCrearUsuario_Click(object sender, RoutedEventArgs e)
		{
			OcultarMensaje();

			string nombre = txtNombre.Text;
			string correo = txtCorreo.Text;
			string password = txtPassword.Password;
			string confirmarPassword = txtConfirmarPassword.Password;

			if (string.IsNullOrWhiteSpace(nombre))
			{
				MostrarError("El nombre es obligatorio");
				return;
			}

			if (nombre.Length < 3)
			{
				MostrarError("El nombre debe tener al menos 3 caracteres");
				return;
			}

			if (nombre.Length > 50)
			{
				MostrarError("El nombre no debe exceder 50 caracteres");
				return;
			}

			if (!nombre.Any(char.IsUpper))
			{
				MostrarError("El nombre debe incluir al menos una mayuscula");
				return;
			}

			if (!nombre.Any(char.IsLower))
			{
				MostrarError("El nombre debe incluir al menos una minuscula");
				return;
			}

			if (nombre.Any(char.IsDigit))
			{
				MostrarError("El nombre no debe contener numeros");
				return;
			}

			if (string.IsNullOrWhiteSpace(correo))
			{
				MostrarError("El correo es obligatorio");
				return;
			}

			if (correo.Length > 80)
			{
				MostrarError("El correo no debe exceder 80 caracteres");
				return;
			}

			if (!correo.Contains("@") || !correo.EndsWith(".com", StringComparison.OrdinalIgnoreCase))
			{
				MostrarError("El correo debe incluir @ y terminar en .com");
				return;
			}

			string patternEmail = @"^[^@\s]+@[^@\s]+\.com$";
			if (!Regex.IsMatch(correo, patternEmail, RegexOptions.IgnoreCase))
			{
				MostrarError("Ingresa un correo valido");
				return;
			}

			if (string.IsNullOrWhiteSpace(password))
			{
				MostrarError("La contrasena es obligatoria");
				return;
			}

			if (password.Length < 8)
			{
				MostrarError("La contrasena debe contener al menos 8 caracteres");
				return;
			}

			if (password.Length > 20)
			{
				MostrarError("La contrasena no debe exceder 20 caracteres");
				return;
			}

			if (!password.Any(char.IsUpper))
			{
				MostrarError("La contrasena debe incluir al menos una mayuscula");
				return;
			}

			if (!password.Any(char.IsLower))
			{
				MostrarError("La contrasena debe incluir al menos una minuscula");
				return;
			}

			if (!password.Any(char.IsDigit))
			{
				MostrarError("La contrasena debe incluir al menos un numero");
				return;
			}

			if (!password.Any(c => !char.IsLetterOrDigit(c)))
			{
				MostrarError("La contrasena debe incluir al menos un caracter especial");
				return;
			}

			if (string.IsNullOrWhiteSpace(confirmarPassword))
			{
				MostrarError("Debes confirmar la contrasena");
				return;
			}

			if (password != confirmarPassword)
			{
				MostrarError("Las contrasenas no coinciden");
				return;
			}

			if (DataService.ObtenerUsuarioPorCorreo(correo) != null)
			{
				MostrarError("Ya existe una cuenta registrada con ese correo");
				return;
			}

			var nuevoUsuario = new Usuario
			{
				Id = Guid.NewGuid().ToString(),
				Nombre = nombre,
				Correo = correo,
				Contrasena = password,
				EsAdmin = chkEsAdmin.IsChecked == true,
				Activo = true
			};

			if (!DataService.GuardarUsuario(nuevoUsuario))
			{
				MostrarError("No se pudo crear el usuario");
				return;
			}

			MostrarExito("Usuario creado correctamente");
			LimpiarFormulario();
			CargarUsuarios();
		}

		private void btnCambiarEstado_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not UsuarioItemViewModel item)
			{
				return;
			}

			if (item.Usuario.Correo != null &&
				item.Usuario.Correo.Equals("admin@vinoteca.com", StringComparison.OrdinalIgnoreCase))
			{
				MostrarError("El usuario admin no se puede desactivar");
				return;
			}

			item.Usuario.Activo = !item.Usuario.Activo;
			DataService.ActualizarUsuario(item.Usuario);
			CargarUsuarios();
		}

		private void LimpiarFormulario()
		{
			txtNombre.Text = string.Empty;
			txtCorreo.Text = string.Empty;
			txtPassword.Password = string.Empty;
			txtConfirmarPassword.Password = string.Empty;
			chkEsAdmin.IsChecked = false;
		}

		private void MostrarError(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
			txtMensaje.Visibility = Visibility.Visible;
		}

		private void MostrarExito(string mensaje)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
			txtMensaje.Visibility = Visibility.Visible;
		}

		private void OcultarMensaje()
		{
			txtMensaje.Visibility = Visibility.Collapsed;
		}
	}

	public class UsuarioItemViewModel
	{
		public Usuario Usuario { get; }
		public string Nombre => Usuario.Nombre ?? string.Empty;
		public string Correo => Usuario.Correo ?? string.Empty;
		public string RolTexto => Usuario.EsAdmin ? "Rol: Admin" : "Rol: Usuario";
		public string EstadoTexto => Usuario.Activo ? "Activo" : "Inactivo";
		public string AccionEstadoTexto => Usuario.Activo ? "Desactivar" : "Activar";

		public UsuarioItemViewModel(Usuario usuario)
		{
			Usuario = usuario;
		}
	}
}
