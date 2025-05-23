using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[Flags]
public enum ImplicitUseTargetFlags
{
  Default = 1,
  Itself = Default, // 0x00000001
  Members = 2,
  WithMembers = Members | Itself, // 0x00000003
}

