using System;

namespace Statsify.Core.Expressions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class FunctionAttribute : Attribute
    {
        public string Name { get; private set; }

        public FunctionAttribute(string name)
        {
            Name = name;
        }
    }
}