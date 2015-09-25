using Compiler.Expressions.MathOperations;

namespace Compiler.Expressions
{
    class IfClause : Sentence
    {
        public MathOperation Condition { get; }
        public Block Body { get; }
        public Block ElseBody { get; }

        public IfClause(MathOperation condition, Block body, Block elseBody)
        {
            Condition = condition;
            Body = body;
            ElseBody = elseBody;
        }
        
        public override string ToString()
        {
            if (ElseBody == null)
                return $"if ({Condition}) {{ ... }}";
            else
                return $"if ({Condition}) {{ ... }} else {{ ... }}";
        }

        public static IfClause TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsumeString("if", state))
                return null;

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume('(', state))
                return null;

            MathOperation condition = MathOperation.TryRead(stream);
            if (condition == null)
            {
                state.Restore("invalid condition");
                return null;
            }

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume(')', state))
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

            stream.SkipWhitespace();
            Block body = Block.TryRead(stream);

            if (body == null)
            {
                state.Restore("invalid if body");
                return null;
            }

            var state2 = stream.SaveState();

            stream.SkipWhitespace();
            
            if (!stream.ExpectAndConsumeString("else", state2))
                return new IfClause(condition, body, null);

            stream.SkipWhitespace();
            Block elseBody = Block.TryRead(stream);
            if (elseBody == null)
            {
                state2.Restore("invalid else body");
                return null;
            }

            return new IfClause(condition, body, elseBody);
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            writer.WriteLine($"; {ToString()}");
            Condition.Compile(writer, definitions);

            int label = definitions.GetNextLabel();
            writer.WriteLine($"{SelectInstruction(Condition.Operation)} label_{label}_true_branch");
            if (ElseBody == null)
                writer.WriteLine($"jmp label_{label}_endif");
            else
                writer.WriteLine($"jmp label_{label}_false_branch");
            writer.WriteLine($"label_{label}_true_branch:");

            Body.Compile(writer, definitions);

            if (ElseBody != null)
            {
                writer.WriteLine($"jmp label_{label}_endif");
                writer.WriteLine($"label_{label}_false_branch:");
                ElseBody.Compile(writer, definitions);
            }

            writer.WriteLine($"label_{label}_endif:");
        }

        public static string SelectInstruction(MathOperation.Op compare)
        {
            switch (compare)
            {
                case MathOperation.Op.Equal: return "je";
                case MathOperation.Op.NotEqual: return "jne";
                case MathOperation.Op.Greater: return "ja";
                case MathOperation.Op.GreaterEqual: return "jae";
                case MathOperation.Op.Less: return "jb";
                case MathOperation.Op.LessEqual: return "jbe";
                default: throw new CompileException($"invalid compare operation: {compare}");
            }
        }
    }
}
