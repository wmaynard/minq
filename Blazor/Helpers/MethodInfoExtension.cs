using System.Collections.Generic;
using System.Reflection;

namespace Maynard.Minq.Blazor.Helpers;

internal static class MethodInfoExtension
{
    internal static bool SignatureMatches(this MethodInfo info, MethodInfo other)
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
    internal static string GenerateSignatureString(this MethodInfo method)
    {
        string returnType = method.ReturnType.GetFriendlyName();
        ParameterInfo[] parameters = method.GetParameters();
        List<string> paramStrings = [];

        foreach (ParameterInfo p in parameters)
        {
            string modifier = p.IsOut ? "out " : (p.ParameterType.IsByRef ? "ref " : "");
            string typeName = p.ParameterType.GetFriendlyName();
            paramStrings.Add($"{modifier}{typeName} {p.Name}");
        }

        return $"{returnType} {method.Name}({string.Join(", ", paramStrings)})";
    }
}