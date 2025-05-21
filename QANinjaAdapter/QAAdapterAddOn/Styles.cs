using QANinjaAdapter;
using System;
using System.Windows;
using System.Windows.Media;

#nullable disable
namespace QAAdapterAddOn
{
    public partial class Styles : ResourceDictionary
    {
        public Styles()
        {
            this.InitializeComponent();
            if (!DataContext.Instance.IsDarkSkin())
                return;
            this[(object)"gridTextColor"] = (object)new SolidColorBrush(Colors.WhiteSmoke);
            this[(object)"textColor"] = (object)new SolidColorBrush(Colors.LightGray);
            this[(object)"highlightColor"] = (object)new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF494949"));
        }

        // Remove InitializeComponent and IComponentConnector.Connect methods
        // They're generated automatically in the .g.cs file
    }
}