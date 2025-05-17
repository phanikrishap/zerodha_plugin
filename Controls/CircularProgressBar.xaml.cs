// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Controls.CircularProgressBar
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

#nullable disable
namespace QANinjaAdapter.Controls
{
    public partial class CircularProgressBar : UserControl
{
    public static readonly DependencyProperty MinimumProperty =
             DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(CircularProgressBar),
                 new UIPropertyMetadata(1));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(CircularProgressBar),
            new UIPropertyMetadata(1));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(CircularProgressBar),
            new UIPropertyMetadata(100));

    private readonly DispatcherTimer _animationTimer;


        public CircularProgressBar()
        {
            this.InitializeComponent();
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(this.OnVisibleChanged);
            this._animationTimer = new DispatcherTimer(DispatcherPriority.ContextIdle, this.Dispatcher)
            {
                Interval = new TimeSpan(0, 0, 0, 0, 75)
            };
        }

        public int Minimum
  {
    get => (int) this.GetValue(CircularProgressBar.MinimumProperty);
    set => this.SetValue(CircularProgressBar.MinimumProperty, (object) value);
  }

  public int Maximum
  {
    get => (int) this.GetValue(CircularProgressBar.MaximumProperty);
    set => this.SetValue(CircularProgressBar.MaximumProperty, (object) value);
  }

  public int Value
  {
    get => (int) this.GetValue(CircularProgressBar.ValueProperty);
    set => this.SetValue(CircularProgressBar.ValueProperty, (object) value);
  }

  private static void SetPosition(
    DependencyObject ellipse,
    double offset,
    double posOffSet,
    double step)
  {
    ellipse.SetValue(Canvas.LeftProperty, (object) (50.0 + Math.Sin(offset + posOffSet * step) * 50.0));
    ellipse.SetValue(Canvas.TopProperty, (object) (50.0 + Math.Cos(offset + posOffSet * step) * 50.0));
  }

  private void Start()
  {
    this._animationTimer.Tick += new EventHandler(this.OnAnimationTick);
    this._animationTimer.Start();
  }

  private void Stop()
  {
    this._animationTimer.Stop();
    this._animationTimer.Tick -= new EventHandler(this.OnAnimationTick);
  }

    private void OnAnimationTick(object sender, EventArgs e)
    {
        _spinnerRotate.Angle = (_spinnerRotate.Angle + 36.0) % 360.0;
    }

    private void OnCanvasLoaded(object sender, RoutedEventArgs e)
    {
        CircularProgressBar.SetPosition((DependencyObject)_circle0, Math.PI, 0.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle1, Math.PI, 1.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle2, Math.PI, 2.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle3, Math.PI, 3.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle4, Math.PI, 4.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle5, Math.PI, 5.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle6, Math.PI, 6.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle7, Math.PI, 7.0, Math.PI / 5.0);
        CircularProgressBar.SetPosition((DependencyObject)this._circle8, Math.PI, 8.0, Math.PI / 5.0);
    }

    private void OnCanvasUnloaded(object sender, RoutedEventArgs e) => this.Stop();

  private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if ((bool) e.NewValue)
      this.Start();
    else
      this.Stop();
  }
}
}
