using System.Reflection;

namespace Maynard.Minq.Blazor.Helpers;

public static class MethodInfoExtension
{
    public static bool SignatureMatches(this MethodInfo info, MethodInfo other)
    {
        if (info.ReturnType != other.ReturnType) 
            return false;

        ParameterInfo[] expectedParams = info.GetParameters();
        ParameterInfo[] actualParams = other.GetParameters();

        if (expectedParams.Length != actualParams.Length) 
            return false;

        for (int i = 0; i < expectedParams.Length; i++)
            if (expectedParams[i].ParameterType != actualParams[i].ParameterType) 
                return false;
            else if (expectedParams[i].IsOut != actualParams[i].IsOut) 
                return false;
        
        return true;
    }
}