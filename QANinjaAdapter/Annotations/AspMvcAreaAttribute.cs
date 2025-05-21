// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.AspMvcAreaAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AspMvcAreaAttribute : Attribute
{
  public AspMvcAreaAttribute()
  {
  }

  public AspMvcAreaAttribute([NotNull] string anonymousProperty)
  {
    this.AnonymousProperty = anonymousProperty;
  }

  [CanBeNull]
  public string AnonymousProperty { get; }
}
