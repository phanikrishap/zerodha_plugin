using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class CollectionAccessAttribute : Attribute
{
  public CollectionAccessAttribute(CollectionAccessType collectionAccessType)
  {
    this.CollectionAccessType = collectionAccessType;
  }

  public CollectionAccessType CollectionAccessType { get; }
}

