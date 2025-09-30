using System;
using System.Linq;
using MongoDB.Bson;

namespace Maynard.Minq.Minq.Extensions;

public static class StringExtension
{
    public static bool CanBeMongoId(this string _string) => ObjectId.TryParse(_string, out ObjectId _);

    public static void MustBeMongoId(this string _string)
    {
        if (!_string.CanBeMongoId())
            throw new Exception(_string);
    }
}