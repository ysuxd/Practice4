using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace Practice
{
    public class PriceMultiplierConverter : IValueConverter
    {
        public static PriceMultiplierConverter Instance { get; } = new PriceMultiplierConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataRowView row)
            {
                try
                {
                    decimal price = row["price"] != DBNull.Value ?
                        System.Convert.ToDecimal(row["price"]) : 0;
                    int quantity = row["quantity"] != DBNull.Value ?
                        System.Convert.ToInt32(row["quantity"]) : 0;
                    return (price * quantity).ToString("0.00") + " ₽";
                }
                catch
                {
                    return "0.00 ₽";
                }
            }
            return "0.00 ₽";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}