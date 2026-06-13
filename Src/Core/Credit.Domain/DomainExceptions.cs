using System;

namespace Credit.Domain;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

