using System.Linq.Expressions;
using System.Reflection;

namespace Rumble.Platform.Common.MinqOld;

public class MemberInfoAccess
{
    public Expression Accessor { get; set; }
    public MemberInfo Member { get; set; }
}