// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.MacroAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class MacroAttribute : Attribute
{
  [CanBeNull]
  public string Expression { get; set; }

  public int Editable { get; set; }

  [CanBeNull]
  public string Target { get; set; }
}
