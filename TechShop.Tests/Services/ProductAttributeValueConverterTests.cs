using System;
using TechShop_API.Models;
using TechShop_API.Utility;
using Xunit;

namespace TechShop_API.Services
{
    public class ProductAttributeValueConverterTests
    {
        private readonly ProductAttributeValueConverter _converter = new();

        [Theory]
        [InlineData("test")]
        [InlineData("")]
        [InlineData(null)]
        public void ApplyValue_StringValue_SetsCorrectly(string value)
        {
            var pa = new ProductAttribute();
            var catAttr = new CategoryAttribute { DataType = SD.DataTypeEnum.String };

            _converter.ApplyValue(pa, catAttr, value);

            Assert.Equal(value, pa.String);
            Assert.Null(pa.Integer);
            Assert.Null(pa.Decimal);
            Assert.Null(pa.Boolean);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("0")]
        public void ApplyValue_IntegerValue_SetsCorrectly(string value)
        {
            var pa = new ProductAttribute();
            var catAttr = new CategoryAttribute { DataType = SD.DataTypeEnum.Integer };

            _converter.ApplyValue(pa, catAttr, value);

            Assert.Equal(int.Parse(value), pa.Integer);
            Assert.Null(pa.String);
            Assert.Null(pa.Decimal);
            Assert.Null(pa.Boolean);
        }

        [Theory]
        [InlineData("12.34")]
        [InlineData("0.0")]
        public void ApplyValue_DecimalValue_SetsCorrectly(string value)
        {
            var pa = new ProductAttribute();
            var catAttr = new CategoryAttribute { DataType = SD.DataTypeEnum.Decimal };

            _converter.ApplyValue(pa, catAttr, value);

            Assert.Equal(double.Parse(value, System.Globalization.CultureInfo.InvariantCulture), pa.Decimal);
            Assert.Null(pa.String);
            Assert.Null(pa.Integer);
            Assert.Null(pa.Boolean);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("invalid", null)]
        public void ApplyValue_BooleanValue_SetsCorrectly(string value, bool? expected)
        {
            var pa = new ProductAttribute();
            var catAttr = new CategoryAttribute { DataType = SD.DataTypeEnum.Boolean };

            _converter.ApplyValue(pa, catAttr, value);

            Assert.Equal(expected, pa.Boolean);
            Assert.Null(pa.String);
            Assert.Null(pa.Integer);
            Assert.Null(pa.Decimal);
        }

        [Fact]
        public void ReadValue_ReturnsCorrectStringRepresentation()
        {
            var pa = new ProductAttribute
            {
                String = "s",
                Integer = 5,
                Decimal = 1.23,
                Boolean = true
            };

            var strVal = _converter.ReadValue(pa, new CategoryAttribute { DataType = SD.DataTypeEnum.String });
            var intVal = _converter.ReadValue(pa, new CategoryAttribute { DataType = SD.DataTypeEnum.Integer });
            var decVal = _converter.ReadValue(pa, new CategoryAttribute { DataType = SD.DataTypeEnum.Decimal });
            var boolVal = _converter.ReadValue(pa, new CategoryAttribute { DataType = SD.DataTypeEnum.Boolean });

            Assert.Equal("s", strVal);
            Assert.Equal("5", intVal);
            Assert.Equal("1.23", decVal.Substring(0, 4)); // substring to handle precision
            Assert.Equal("true", boolVal);
        }
    }
}
