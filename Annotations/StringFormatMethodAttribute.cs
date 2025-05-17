// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.StringFormatMethodAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Delegate)]
public sealed class StringFormatMethodAttribute : Attribute
{
  public StringFormatMethodAttribute([NotNull] string formatParameterName)
  {
    this.FormatParameterName = formatParameterName;
  }

  [NotNull]
  public string FormatParameterName { get; }
}
