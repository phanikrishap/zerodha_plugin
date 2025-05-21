using QAAdapterAddOn.ViewModels;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System;

namespace QANinjaAdapter
{
    public partial class LoadWindow : Window
    {
       
        public LoadWindow(LoadViewModel loadViewModel)
        {
            this.DataContext = loadViewModel;
            this.InitializeComponent();
        }

        //[DebuggerNonUserCode]
        //[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        //internal Delegate _CreateDelegate(Type delegateType, string handler)
        //{
        //    return Delegate.CreateDelegate(delegateType, this, handler);
        //}
    }
}