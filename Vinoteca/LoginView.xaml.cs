using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class LoginView : Page
	{
		public LoginView()
		{
			this.InitializeComponent();
		}

		private void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			// Ocultar error previo
			txtError.Visibility = Visibility.Collapsed;

			string correo = txtCorreo.Text.Trim();
			string password = txtPassword.Password;

			if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
			{
				MostrarError("Por favor llena ambos campos.");
				return;
			}

			// Validar contra el JSON
			var usuarios = DataService.ObtenerUsuarios();
			var usuarioValido = usuarios.FirstOrDefault(u => u.Correo == correo && u.Contrasena == password && u.Activo);

			if (usuarioValido != null)
			{
				// Guardar en sesión
				SessionService.IniciarSesion(usuarioValido);

				// Redirección por rol
				if (usuarioValido.EsAdmin)
				{
					this.Frame.Navigate(typeof(AdminMenuView));
				}
				else
				{
					this.Frame.Navigate(typeof(UsuarioMenuView));
				}
			}
			else
			{
				MostrarError("Credenciales incorrectas o usuario inactivo.");
			}
		}

		private void MostrarError(string mensaje)
		{
			txtError.Text = mensaje;
			txtError.Visibility = Visibility.Visible;
		}
	}
}