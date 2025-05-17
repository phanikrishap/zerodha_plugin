// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Controls.LoadingPanel
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using QANinjaAdapter.Annotations;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

#nullable disable
namespace QANinjaAdapter.Controls
{
    public partial class LoadingPanel : UserControl
    {
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
            nameof(IsLoading), typeof(bool), typeof(LoadingPanel),
            new UIPropertyMetadata(false, PropertyChangedCallback));

        public static readonly DependencyProperty MessageProperty = DependencyProperty.RegisterAttached(
            nameof(Message), typeof(string), typeof(LoadingPanel),
            new UIPropertyMetadata("", null));

        public static readonly DependencyProperty SubMessageProperty = DependencyProperty.RegisterAttached(
            nameof(SubMessage), typeof(string), typeof(LoadingPanel),
            new UIPropertyMetadata("", null));

        private static void PropertyChangedCallback(
            DependencyObject o,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(o is LoadingPanel loadingPanel))
                return;

            if ((bool)e.NewValue)
                loadingPanel.Visibility = Visibility.Visible;
            else
                loadingPanel.Visibility = Visibility.Hidden;
        }

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public string SubMessage
        {
            get => (string)GetValue(SubMessageProperty);
            set => SetValue(SubMessageProperty, value);
        }

        public LoadingPanel()
        {
            InitializeComponent();
            Visibility = Visibility.Hidden;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //[DebuggerNonUserCode]
        //[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        //internal Delegate _CreateDelegate(Type delegateType, string handler)
        //{
        //    return Delegate.CreateDelegate(delegateType, this, handler);
        //}
    }
}
