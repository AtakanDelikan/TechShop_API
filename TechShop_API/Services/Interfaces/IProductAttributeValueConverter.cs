using TechShop_API.Models;

namespace TechShop_API.Services.Interfaces
{
    /// <summary>
    /// Responsible for parsing string input and applying it to ProductAttribute typed fields,
    /// and for extracting a display string from a ProductAttribute according to CategoryAttribute.DataType.
    /// </summary>
    public interface IProductAttributeValueConverter
    {
        /// <summary>Apply string value to productAttribute typed fields. Throws ArgumentException on invalid conversion.</summary>
        void ApplyValue(ProductAttribute productAttribute, CategoryAttribute categoryAttribute, string value);

        /// <summary>Read the stored value from productAttribute as display string according to categoryAttribute.DataType.</summary>
        string? ReadValue(ProductAttribute productAttribute, CategoryAttribute categoryAttribute);
    }
}
