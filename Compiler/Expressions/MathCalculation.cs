namespace Compiler.Expressions
{
    class MathCalculation
    {
        public enum Type { None, Imediate, StackValue, DataValue, Register }

        public Type StorageType { get; }
        public string Value { get; }

        public MathCalculation(Type storage, string value)
        {
            StorageType = storage;
            Value = value;
        }
        
        public MathCalculation() : this(Type.None, "")
        {

        }
    }
}
