// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.RazorInjectionAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RazorInjectionAttribute : Attribute
{
  public RazorInjectionAttribute([NotNull] string type, [NotNull] string fieldName)
  {
    this.Type = type;
    this.FieldName = fieldName;
  }

  [NotNull]
  public string Type { get; }

  [NotNull]
  public string FieldName { get; }
}
