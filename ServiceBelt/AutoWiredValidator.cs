using System;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;

// BUG #96: Move this to ServiceBeltStack

namespace ServiceBelt
{
    public abstract class AutowiredValidator<T> : AbstractValidator<T>
    {
        private object thisLock = new object();
        private bool constructed = false;

        public AutowiredValidator()
        {
        }

        protected abstract void AddRules();

        public override ValidationResult Validate(ValidationContext<T> context)
        {
            lock (thisLock)
            {
                if (!constructed)
                {
                    AddRules();
                    constructed = true;
                }
            }

            return base.Validate(context);
        }
    }
}

