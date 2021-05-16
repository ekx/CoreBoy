using System;
namespace CoreBoy.Core
{
    public class MissingOpcodeException : Exception
    {
        public MissingOpcodeException() : base() { }
        public MissingOpcodeException(string message) : base(message) { }
        public MissingOpcodeException(string message, Exception inner) : base(message, inner) { }
    }
}

