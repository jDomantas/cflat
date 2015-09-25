namespace Compiler.Expressions
{
    class Increment : Value
    {
        public Value BaseValue { get; }
        public bool IsIncrementing { get; }

        public Increment(Value baseValue, bool increment) : base(baseValue.Type, true)
        {
            BaseValue = baseValue;
            IsIncrementing = increment;

            DoesUseRegisters = baseValue.DoesUseRegisters;
        }

        public override string ToString()
        {
            if (IsIncrementing)
                return $"++{BaseValue}";
            else
                return $"--{BaseValue}";
        }

        public new static Increment TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (stream.Peek() == '+')
            {
                stream.Consume();
                if (!stream.ExpectAndConsume('+', state))
                    return null;

                Value inner = Value.TryRead(stream);
                if (inner == null)
                {
                    state.Restore("invalid value");
                    return null;
                }

                if (!inner.Writable)
                {
                    state.Restore("can't increment non-writable value");
                    return null;
                }

                return new Increment(inner, true);
            }
            else if (stream.Peek() == '-')
            {
                stream.Consume();
                if (!stream.ExpectAndConsume('-', state))
                    return null;

                Value inner = Value.TryRead(stream);
                if (inner == null)
                {
                    state.Restore("invalid value");
                    return null;
                }

                if (!inner.Writable)
                {
                    state.Restore("can't decrement non-writable value");
                    return null;
                }

                return new Increment(inner, false);
            }
            else
            {
                return null;
            }

        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            MathCalculation inner = BaseValue.CompileAndGetStorage(writer, definitions);
            if (BaseValue.Type.PointerDepth == 0 || BaseValue.Type.Dereferenced().GetSize() == 1)
            {
                if (IsIncrementing)
                    writer.WriteLine($"inc {inner.Value}");
                else
                    writer.WriteLine($"dec {inner.Value}");
            }
            else
            {
                if (IsIncrementing)
                    writer.WriteLine($"add {inner.Value}, {BaseValue.Type.Dereferenced().GetSize()}");
                else
                    writer.WriteLine($"sub {inner.Value}, {BaseValue.Type.Dereferenced().GetSize()}");
            }

            return inner;
        }
    }
}
