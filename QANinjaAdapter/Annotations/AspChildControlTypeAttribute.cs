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

