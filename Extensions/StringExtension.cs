using System;
using MongoDB.Bson;

namespace Maynard.Minq.Extensions;

public static class StringExtension
{
    public static bool CanBeMongoId(this string value) => ObjectId.TryParse(value, out ObjectId _);

    public static void MustBeMongoId(this string value)
    {
        if (!value.CanBeMongoId())
            throw new(value);
    }
}