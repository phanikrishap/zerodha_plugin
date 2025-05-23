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

