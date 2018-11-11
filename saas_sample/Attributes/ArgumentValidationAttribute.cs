using System;

namespace saas_sample.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class ArgumentValidationAttribute : Attribute
    {
        public abstract void Validate(object value, string argumentName);
    }
}