// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.AspMvcViewLocationFormatAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class AspMvcViewLocationFormatAttribute : Attribute
{
  public AspMvcViewLocationFormatAttribute([NotNull] string format) => this.Format = format;

  [NotNull]
  public string Format { get; }
}
