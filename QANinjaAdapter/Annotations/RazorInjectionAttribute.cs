using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RazorInjectionAttribute : Attribute
{
  public RazorInjectionAttribute([NotNull] string type, [NotNull] string fieldName)
  {
    this.Type = type;
    this.FieldName = fieldName;
  }

  [NotNull]
  public string Type { get; }

  [NotNull]
  public string FieldName { get; }
}

