using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Vinoteca.Helpers
{
	public class ImageSourceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var path = value as string;

			if (string.IsNullOrWhiteSpace(path))
				return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;

			try
			{
				return new BitmapImage(new Uri(path));
			}
			catch
			{
				return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
