using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LocalSpaceManager.UI.Converters;

public class ViewVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string currentView && parameter is string targetView)
        {
            // For Cleanup view, we reuse the Files grid
            if (currentView == "Cleanup" && targetView == "Files") return Visibility.Visible;
            
            return currentView == targetView ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
