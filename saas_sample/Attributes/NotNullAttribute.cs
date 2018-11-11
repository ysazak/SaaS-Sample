using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace saas_sample.Attributes
{
    public class NotNullAttribute : ArgumentValidationAttribute
    {
        public override void Validate(object value, string argumentName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }
    }
}
