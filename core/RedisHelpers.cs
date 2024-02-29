using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpbench.Core;

internal static class RedisHelpers
{
    public static string GetJobKey(string id) => $"job:{id}";
    public static string JobKeyToId(string jobKey) => jobKey.Split(':', 2)[1];
}
