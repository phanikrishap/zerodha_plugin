using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MustUseReturnValueAttribute : Attribute
{
  public MustUseReturnValueAttribute()
  {
  }

  public MustUseReturnValueAttribute([NotNull] string justification)
  {
    this.Justification = justification;
  }

  [CanBeNull]
  public string Justification { get; }
}

