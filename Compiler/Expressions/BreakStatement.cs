namespace Compiler.Expressions
{
    class BreakStatement : Sentence
    {
        public BreakStatement()
        {

        }

        public static BreakStatement TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();
            if (!stream.ExpectAndConsumeString("break", state))
                return null;
            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume(';', state))
                return null;

            if (stream.CurrentDefinitions.CurrentBreakStatement == null)
            {
                state.Restore("can't break while not in loop");
                return null;
            }

            return new BreakStatement();
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            writer.WriteLine("; break");
            writer.WriteLine($"jmp {definitions.CurrentBreakStatement}");
        }
    }
}
