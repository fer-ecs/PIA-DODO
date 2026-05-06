using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Vinoteca.Helpers
{
	public class ImageSourceConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			var path = value as string;

			if (string.IsNullOrWhiteSpace(path))
				return null;

			try
			{
				return new BitmapImage(new Uri(path));
			}
			catch
			{
				return null;
			}
		}

		public object? ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
