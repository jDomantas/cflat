namespace Compiler.Expressions
{
    class ReturnStatement : Sentence
    {
        public MathExpression Result { get; private set; }
        public DataType Type { get; private set; }

        public ReturnStatement(MathExpression result) : base()
        {
            Result = result;
            Type = null;
        }
        
        public override string ToString()
        {
            return $"return {Result}";
        }

        public static ReturnStatement TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsumeString("return", state))
                return null;

            stream.SkipWhitespace();
            if (stream.CurrentDefinitions.CurrentFunction.ReturnType == DataType.Void)
            {

                if (!stream.ExpectAndConsume(';', state))
                    return null;
                else
                    return new ReturnStatement(null);
            }
            else
            {
                MathExpression result = MathExpression.TryRead(stream);
                if (result == null)
                {
                    state.Restore("invalid return expression");
                    return null;
                }

                stream.SkipWhitespace();
                if (!stream.ExpectAndConsume(';', state))
                    return null;

                if (result.Type != stream.CurrentDefinitions.CurrentFunction.ReturnType && 
                    !result.Type.CanImplicitlyCastTo(stream.CurrentDefinitions.CurrentFunction.ReturnType))
                {
                    state.Restore($"can't implicitly cast {result.Type} to {stream.CurrentDefinitions.CurrentFunction.ReturnType}");
                    return null;
                }

                if (result.Type != stream.CurrentDefinitions.CurrentFunction.ReturnType)
                    result = new TypeCast(result, stream.CurrentDefinitions.CurrentFunction.ReturnType);
                
                return new ReturnStatement(result);
            }
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            writer.WriteLine($"; {ToString()}");
            MathCalculation result = Result.CompileAndGetStorage(writer, definitions);
            string dest = Result.Type.GetSize() == 1 ? "al" : "ax";

            if (result.StorageType == MathCalculation.Type.DataValue ||
                result.StorageType == MathCalculation.Type.StackValue ||
                result.StorageType == MathCalculation.Type.Imediate)
                writer.WriteLine($"mov {dest}, {result.Value}");
            else if (result.StorageType == MathCalculation.Type.Register && result.Value != dest)
            {
                writer.WriteLine($"mov {dest}, {result.Value}");
            }

            int count = definitions.CountFunctionExiting();

            if (count > 0)
            {
                writer.WriteLine("; pop inner variables");
                writer.WriteLine($"add sp, {count}");
            }
            else
            {
                writer.WriteLine("; no variables to pop");
            }

            writer.WriteLine("pop bp");
            if (definitions.CurrentFunction.Parameters.TotalSize > 0)
                writer.WriteLine($"ret {definitions.CurrentFunction.Parameters.TotalSize}");
            else
                writer.WriteLine("ret");
        }
    }
}
