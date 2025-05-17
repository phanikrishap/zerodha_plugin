// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.AspChildControlTypeAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AspChildControlTypeAttribute : Attribute
{
  public AspChildControlTypeAttribute([NotNull] string tagName, [NotNull] Type controlType)
  {
    this.TagName = tagName;
    this.ControlType = controlType;
  }

  [NotNull]
  public string TagName { get; }

  [NotNull]
  public Type ControlType { get; }
}
