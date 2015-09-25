using System;

namespace Compiler.Expressions
{
    abstract class MathExpression : Sentence
    {
        public DataType Type { get; private set; }
        public bool Writable { get; }
        public bool IsConstant { get; protected set; }
        public int ConstantValue { get; protected set; }
        public bool DoesUseRegisters { get; protected set; }
        
        public MathExpression(DataType type, bool writable)
        {
            if (type == null)
                throw new Exception("must set type immediately");

            Type = type;
            Writable = writable;
            
            IsConstant = false;
            DoesUseRegisters = true;
        }
        
        public static MathExpression TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            MathOperation operation = MathOperation.TryRead(stream);
            if (operation != null)
                return operation;

            Value value = Value.TryRead(stream);
            if (value != null)
                return value;

            state.Restore("invalid mathematical expression");
            return null;
        }
        
        public sealed override void Compile(CodeWriter writer, Definitions definitions)
        {
            writer.WriteLine($"; {ToString()}");
            MathCalculation result = CompileAndGetStorage(writer, definitions);
        }

        public void CompileAndPlaceOnStack(CodeWriter writer, Definitions definitions)
        {
            MathCalculation result = CompileAndGetStorage(writer, definitions);
            if (result.StorageType == MathCalculation.Type.DataValue ||
                result.StorageType == MathCalculation.Type.Imediate ||
                result.StorageType == MathCalculation.Type.Register ||
                result.StorageType == MathCalculation.Type.StackValue)
            {
                if (Type.GetSize() == 1)
                {
                    if (result.StorageType == MathCalculation.Type.StackValue || result.StorageType == MathCalculation.Type.DataValue)
                    {
                        writer.WriteLine($"mov al, {result.Value}");
                        writer.WriteLine("push_byte al");
                    }
                    else
                        writer.WriteLine($"push_byte {result.Value}");
                }
                else
                    writer.WriteLine($"push {result.Value}");
            }
        }

        public abstract MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions);
    }
}
