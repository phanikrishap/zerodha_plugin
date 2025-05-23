using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RazorPageBaseTypeAttribute : Attribute
{
  public RazorPageBaseTypeAttribute([NotNull] string baseType) => this.BaseType = baseType;

  public RazorPageBaseTypeAttribute([NotNull] string baseType, string pageName)
  {
    this.BaseType = baseType;
    this.PageName = pageName;
  }

  [NotNull]
  public string BaseType { get; }

  [CanBeNull]
  public string PageName { get; }
}

