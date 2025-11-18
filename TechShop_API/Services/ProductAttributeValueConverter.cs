using System.Globalization;
using TechShop_API.Models;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Services
{
    public class ProductAttributeValueConverter : IProductAttributeValueConverter
    {
        public void ApplyValue(ProductAttribute productAttribute, CategoryAttribute categoryAttribute, string value)
        {
            if (categoryAttribute == null) throw new ArgumentNullException(nameof(categoryAttribute));

            switch (categoryAttribute.DataType)
            {
                case SD.DataTypeEnum.String:
                    productAttribute.String = value;
                    break;

                case SD.DataTypeEnum.Integer:
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                        throw new ArgumentException("Value cannot be converted to integer");
                    productAttribute.Integer = i;
                    productAttribute.String = null;
                    productAttribute.Decimal = null;
                    productAttribute.Boolean = null;
                    break;

                case SD.DataTypeEnum.Decimal:
                    if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                        throw new ArgumentException("Value cannot be converted to decimal");
                    productAttribute.Decimal = d;
                    productAttribute.String = null;
                    productAttribute.Integer = null;
                    productAttribute.Boolean = null;
                    break;

                case SD.DataTypeEnum.Boolean:
                    if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                        productAttribute.Boolean = true;
                    else if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
                        productAttribute.Boolean = false;
                    else
                        productAttribute.Boolean = null; // allow null for invalid input or explicit null
                    productAttribute.String = null;
                    productAttribute.Integer = null;
                    productAttribute.Decimal = null;
                    break;

                default:
                    // fallback to storing raw string
                    productAttribute.String = value;
                    break;
            }
        }

        public string? ReadValue(ProductAttribute productAttribute, CategoryAttribute categoryAttribute)
        {
            if (categoryAttribute == null) throw new ArgumentNullException(nameof(categoryAttribute));

            return categoryAttribute.DataType switch
            {
                SD.DataTypeEnum.String => productAttribute.String,
                SD.DataTypeEnum.Integer => productAttribute.Integer?.ToString(CultureInfo.InvariantCulture),
                SD.DataTypeEnum.Decimal => productAttribute.Decimal?.ToString("G", CultureInfo.InvariantCulture),
                SD.DataTypeEnum.Boolean => productAttribute.Boolean?.ToString().ToLower(),
                _ => productAttribute.String
            };
        }
    }
}
