using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Method)]
public sealed class NotifyPropertyChangedInvocatorAttribute : Attribute
{
  public NotifyPropertyChangedInvocatorAttribute()
  {
  }

  public NotifyPropertyChangedInvocatorAttribute([NotNull] string parameterName)
  {
    this.ParameterName = parameterName;
  }

  [CanBeNull]
  public string ParameterName { get; }
}

