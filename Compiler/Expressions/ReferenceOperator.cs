using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Expressions
{
    class ReferenceOperator : Value
    {
        public Variable ReferencedVariable { get; }
        public AddressDereference Address { get; }

        public ReferenceOperator(Variable variable) 
            : base(new DataType(variable.Type.Name, variable.Type.PointerDepth + 1), false)
        {
            Address = null;
            ReferencedVariable = variable;
            DoesUseRegisters = true;
        }

        public ReferenceOperator(AddressDereference address) 
            : base(new DataType(address.Type.Name, address.Type.PointerDepth + 1), false)
        {
            Address = address;
            ReferencedVariable = null;
            DoesUseRegisters = Address.DoesUseRegisters;
        }

        public override string ToString()
        {
            if (ReferencedVariable != null)
                return $"&{ReferencedVariable.ToString()}";
            return $"&{Address.ToString()}";
        }

        public new static ReferenceOperator TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsume('&', state))
                return null;

            stream.SkipWhitespace();

            AddressDereference address = AddressDereference.TryRead(stream);
            if (address != null && !address.Writable)
            {
                state.Restore("cannot dereference expression");
                return null;
            }

            if (address != null) return new ReferenceOperator(address);

            Variable variable = Variable.TryRead(stream);
            if (variable == null || !variable.Writable)
            {
                state.Restore("cannot dereference expression");
                return null;
            }
            
            return new ReferenceOperator(variable);
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            if (Address != null)
                return Address.CompileAndGetStorage(writer, definitions);
            else
            {
                int address;
                definitions.FindVariable(ReferencedVariable.Name, out address);
                
                writer.WriteLine("mov dx, bp");
                if (address > 0)
                    writer.WriteLine($"add dx, {address}");
                else
                    writer.WriteLine($"sub dx, {-address}");

                return new MathCalculation(MathCalculation.Type.Register, "dx");
            }
        }
    }
}
