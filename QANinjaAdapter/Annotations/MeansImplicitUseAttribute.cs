using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
public sealed class MeansImplicitUseAttribute : Attribute
{
  public MeansImplicitUseAttribute()
    : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default)
  {
  }

  public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags)
    : this(useKindFlags, ImplicitUseTargetFlags.Default)
  {
  }

  public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags)
    : this(ImplicitUseKindFlags.Default, targetFlags)
  {
  }

  public MeansImplicitUseAttribute(
    ImplicitUseKindFlags useKindFlags,
    ImplicitUseTargetFlags targetFlags)
  {
    this.UseKindFlags = useKindFlags;
    this.TargetFlags = targetFlags;
  }

  [UsedImplicitly]
  public ImplicitUseKindFlags UseKindFlags { get; }

  [UsedImplicitly]
  public ImplicitUseTargetFlags TargetFlags { get; }
}

