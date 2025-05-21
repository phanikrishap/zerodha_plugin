// Decompiled with JetBrains decompiler
// Type: QANinjaAdapter.Annotations.UsedImplicitlyAttribute
// Assembly: QANinjaAdapter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3950ED3-7884-49E5-9F57-41CBA3235764
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\QANinjaAdapter.dll

using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.All)]
public sealed class UsedImplicitlyAttribute : Attribute
{
  public UsedImplicitlyAttribute()
    : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default)
  {
  }

  public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags)
    : this(useKindFlags, ImplicitUseTargetFlags.Default)
  {
  }

  public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags)
    : this(ImplicitUseKindFlags.Default, targetFlags)
  {
  }

  public UsedImplicitlyAttribute(
    ImplicitUseKindFlags useKindFlags,
    ImplicitUseTargetFlags targetFlags)
  {
    this.UseKindFlags = useKindFlags;
    this.TargetFlags = targetFlags;
  }

  public ImplicitUseKindFlags UseKindFlags { get; }

  public ImplicitUseTargetFlags TargetFlags { get; }
}
