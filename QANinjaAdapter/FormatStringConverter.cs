// Adapted for Zerodha
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

#nullable disable
namespace QANinjaAdapter
{
    public class FormatStringConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override TypeConverter.StandardValuesCollection GetStandardValues(
          ITypeDescriptorContext context)
        {
            return new TypeConverter.StandardValuesCollection((ICollection)new List<string>()
            {
                "kite.zerodha.com",
                "zerodha.com"
            });
        }
    }
}