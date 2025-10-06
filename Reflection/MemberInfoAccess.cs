using System.Linq.Expressions;
using System.Reflection;

namespace Maynard.Minq.Reflection;

public class MemberInfoAccess
{
    public Expression Accessor { get; set; }
    public MemberInfo Member { get; set; }
}