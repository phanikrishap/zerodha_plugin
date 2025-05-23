using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PathReferenceAttribute : Attribute
{
  public PathReferenceAttribute()
  {
  }

  public PathReferenceAttribute([NotNull, PathReference] string basePath)
  {
    this.BasePath = basePath;
  }

  [CanBeNull]
  public string BasePath { get; }
}

