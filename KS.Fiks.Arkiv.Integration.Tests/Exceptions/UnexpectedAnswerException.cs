using System;

namespace KS.Fiks.Arkiv.Integration.Tests.Exceptions;

public class UnexpectedAnswerException : Exception
{
    public UnexpectedAnswerException()
    {
    }

    public UnexpectedAnswerException(string message) : base(message)
    {
    }

    public UnexpectedAnswerException(string message, Exception inner) : base(message, inner)
    {
    }
}