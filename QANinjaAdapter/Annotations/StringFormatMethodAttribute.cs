using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Delegate)]
public sealed class StringFormatMethodAttribute : Attribute
{
  public StringFormatMethodAttribute([NotNull] string formatParameterName)
  {
    this.FormatParameterName = formatParameterName;
  }

  [NotNull]
  public string FormatParameterName { get; }
}

