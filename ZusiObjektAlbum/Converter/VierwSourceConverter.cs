using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using ZusiKlassenLib.Fahrplan;
using ZusiObjektAlbum.MVVM;

namespace ZusiObjektAlbum.Converter
{

  public class ViewSourceConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      CollectionViewSource cvs = new CollectionViewSource() { Source = value };
      cvs.Filter += Cvs_Filter;
      return cvs.View;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    private void Cvs_Filter(object sender, FilterEventArgs e)
    {

      var item = e.Item;
      e.Accepted = true;

    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

  }
}
