using System;


namespace QABrokerAPI.Common.Extensions;

public static class DateTimeExtensions
{
  public static long ConvertToUnixTime(this DateTime datetime)
  {
    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    return (long) (datetime - dateTime).TotalMilliseconds;
  }
}

