using System;
using System.Runtime.Serialization;

namespace Compiler
{
    class CompileException : Exception
    {
        public SymbolStream Stream;

        public CompileException()
        {

        }

        public CompileException(string message, string location) : base($"{message}, at {location}")
        {

        }

        public CompileException(string message) : base(message)
        {

        }

        public CompileException(string message, Exception inner) : base(message, inner)
        {

        }

        public CompileException(SymbolStream stream) : base()
        {
            Stream = stream;
        }

        protected CompileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
