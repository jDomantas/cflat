using Compiler.Expressions.MathOperations;

namespace Compiler.Expressions
{
    class ForLoop : Sentence
    {
        public Sentence Initializer { get; }
        public MathOperation Condition { get; }
        public MathExpression Incrementer { get; }
        public Block Body { get; }

        public ForLoop(Sentence initializer, MathOperation condition, MathExpression incrementer, Block body)
        {
            Initializer = initializer;
            Condition = condition;
            Incrementer = incrementer;
            Body = body;
        }
        
        public override string ToString()
        {
            return $"for ({Initializer}; {Condition}; {Incrementer}) {{ ... }}";
        }

        public static ForLoop TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsumeString("for", state))
            {
                state.Restore("invalid for keyword");
                return null;
            }

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume('(', state))
                return null;

            stream.SkipWhitespace();

            Sentence init;
            if (stream.Peek() == ';')
            {
                stream.Consume();
                init = null;
            }
            else
            {
                init = VariableDefinition.TryRead(stream);
                if (init == null)
                {
                    init = MathExpression.TryRead(stream);
                    if (init == null)
                    {
                        state.Restore("invalid initializer");
                        return null;
                    }
                    stream.SkipWhitespace();
                    if (!stream.ExpectAndConsume(';', state))
                        return null;
                }
            }

            stream.SkipWhitespace();
            MathExpression condition = MathOperation.TryRead(stream);
            if (condition == null)
            {
                state.Restore("invalid condition");
                return null;
            }

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume(';', state))
                return null;

            if (condition.Type.PointerDepth > 0)
                condition = new CompareOperation(MathOperation.Op.NotEqual, condition, new LiteralValue(0, condition.Type));
            if (condition.Type == DataType.UInt)
                condition = new CompareOperation(MathOperation.Op.NotEqual, condition, new LiteralValue(0, DataType.UInt));
            if (condition.Type == DataType.UByte)
                condition = new CompareOperation(MathOperation.Op.NotEqual, condition, new LiteralValue(0, DataType.UByte));

            if (condition.Type != DataType.Flags)
            {
                state.Restore($"invalid condition type, expected: flags, got: {condition.Type}");
                return null;
            }

            MathOperation cond = condition as MathOperation;
            if (cond == null)
            {
                state.Restore("invalid condition");
                return null;
            }

            stream.SkipWhitespace();
            MathExpression incrementer = null;
            if (stream.Peek() != ')')
            {
                incrementer = MathExpression.TryRead(stream);
                if (incrementer == null)
                {
                    state.Restore("invalid incrementer");
                    return null;
                }
            }

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume(')', state))
                return null;

            string prevBreak = stream.CurrentDefinitions.CurrentBreakStatement;
            string prevCont = stream.CurrentDefinitions.CurrentContinueStatement;
            stream.CurrentDefinitions.CurrentBreakStatement = "";
            stream.CurrentDefinitions.CurrentContinueStatement = "";
            
            stream.SkipWhitespace();
            Block body = Block.TryRead(stream);

            stream.CurrentDefinitions.CurrentBreakStatement = prevBreak;
            stream.CurrentDefinitions.CurrentContinueStatement = prevCont;

            if (body == null)
            {
                state.Restore("invalid for body");
                return null;
            }

            state.RestoreDefinitions();

            return new ForLoop(init, cond, incrementer, body);
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            definitions.EnterBlock();

            int label = definitions.GetNextLabel();

            if (Initializer != null)
            {
                writer.WriteLine("; for initializer");
                Initializer.Compile(writer, definitions);
            }

            writer.WriteLine($"; for condition: {Condition}");
            writer.WriteLine($"label_{label}_for_condition:");
            Condition.Compile(writer, definitions);

            writer.WriteLine($"{IfClause.SelectInstruction(Condition.Operation)} label_{label}_for_body");
            writer.WriteLine($"jmp label_{label}_for_end");
            writer.WriteLine($"label_{label}_for_body:");

            string prevBreak = definitions.CurrentBreakStatement;
            string prevCont = definitions.CurrentContinueStatement;
            definitions.CurrentBreakStatement = $"label_{label}_for_end";
            definitions.CurrentContinueStatement = $"label_{label}_for_increment";

            Body.Compile(writer, definitions);

            definitions.CurrentBreakStatement = prevBreak;
            definitions.CurrentContinueStatement = prevCont;

            if (Incrementer != null)
            {
                writer.WriteLine("; for incrementer");
                writer.WriteLine($"label_{label}_for_increment:");
                Incrementer.Compile(writer, definitions);
            }

            writer.WriteLine($"jmp label_{label}_for_condition");
            writer.WriteLine($"label_{label}_for_end:");

            int bytes = definitions.ExitBlock();
            if (bytes > 0)
            {
                writer.WriteLine($"; exiting for, popping {bytes} bytes");
                writer.WriteLine($"add sp, {bytes}");
            }
        }
    }
}
