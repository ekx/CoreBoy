using System;

namespace CoreBoy.Core.Utils;

public class MissingOpcodeException : Exception
{
    public MissingOpcodeException(string message, Exception inner) : base(message, inner) { }
}