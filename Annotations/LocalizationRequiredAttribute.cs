// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.LocalizationRequiredAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.All)]
public sealed class LocalizationRequiredAttribute : Attribute
{
  public LocalizationRequiredAttribute()
    : this(true)
  {
  }

  public LocalizationRequiredAttribute(bool required) => this.Required = required;

  public bool Required { get; }
}
