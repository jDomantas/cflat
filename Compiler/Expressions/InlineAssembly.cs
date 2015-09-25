using System.Collections.Generic;

namespace Compiler.Expressions
{
    class InlineAssembly : Sentence
    {
        private string[] Lines { get; }

        public InlineAssembly(string[] lines)
        {
            Lines = lines;
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            writer.WriteLine("; === inline assembly block ===");

            foreach (var s in Lines)
                writer.WriteLine(s);

            writer.WriteLine("; === end of assembly block ===");
        }

        public static InlineAssembly TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();
            if (!stream.ExpectAndConsumeString("__asm", state))
                return null;

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume('{', state))
                return null;

            List<string> lines = new List<string>();

            while (stream.Peek() != SymbolStream.EOF)
            {
                stream.SkipWhitespace();

                if (stream.Peek() == '}')
                {
                    stream.Consume();
                    return new InlineAssembly(lines.ToArray());
                }

                lines.Add(stream.ConsumeToEndOfLine().Trim());
            }

            if (!stream.ExpectAndConsume('}', state))
                return null;

            state.Restore();
            return null;
        }
    }
}
