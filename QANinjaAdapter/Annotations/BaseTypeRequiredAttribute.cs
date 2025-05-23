using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[BaseTypeRequired(typeof (Attribute))]
public sealed class BaseTypeRequiredAttribute : Attribute
{
  public BaseTypeRequiredAttribute([NotNull] Type baseType) => this.BaseType = baseType;

  [NotNull]
  public Type BaseType { get; }
}

