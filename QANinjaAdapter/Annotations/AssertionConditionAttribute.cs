using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AssertionConditionAttribute : Attribute
{
  public AssertionConditionAttribute(AssertionConditionType conditionType)
  {
    this.ConditionType = conditionType;
  }

  public AssertionConditionType ConditionType { get; }
}

