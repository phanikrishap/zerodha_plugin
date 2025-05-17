// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.PublicAPIAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
public sealed class PublicAPIAttribute : Attribute
{
  public PublicAPIAttribute()
  {
  }

  public PublicAPIAttribute([NotNull] string comment) => this.Comment = comment;

  [CanBeNull]
  public string Comment { get; }
}
