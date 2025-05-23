using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Extensions;

public static class EnumExtensions
{
  public static string GetEnumMemberValue<T>(T value) where T : struct, IConvertible
  {
    MemberInfo element = typeof (T).GetTypeInfo().DeclaredMembers.SingleOrDefault<MemberInfo>((Func<MemberInfo, bool>) (x => x.Name == value.ToString()));
    if ((object) element == null)
      return (string) null;
    return element.GetCustomAttribute<EnumMemberAttribute>(false)?.Value;
  }
}

