using System;
using System.Text.Json.Serialization;
using Maynard.Minq.Models;

namespace Maynard.Minq.Exceptions;

public class UniqueConstraintException<T>(T failure, Exception inner) : MinqException("Unique constraint violated; operation cannot proceed", inner: inner)
    where T : MinqDocument
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T SuspectedFailure { get; set; } = failure;
}