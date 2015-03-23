using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Resources;
using ServiceStack.FluentValidation.Results;
using ServiceStack.FluentValidation.Validators;

namespace ServiceBelt
{
    public static class DataModelValidatorExtensions
    {
        public static IRuleBuilderOptions<T, TProp> IsUrl<T, TProp>(this IRuleBuilder<T, TProp> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new IsUrlValidator());
        }

        public static IRuleBuilderOptions<T, TProperty> IsInEnum<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new IsInEnumValidator<TProperty>());
        }

        public static IRuleBuilderOptions<T, TProp> IsReference<T, TProp>(this IRuleBuilder<T, TProp> ruleBuilder, IMongoManager mongo)
        {
            return ruleBuilder.SetValidator(new IsReferenceValidator(mongo));
        }
    }

    public class IsInEnumValidator<T> : PropertyValidator
    {
        // See http://fluentvalidation.codeplex.com/discussions/401309
        public IsInEnumValidator() : base("Property '{PropertyName}' it not a valid enum value.", "InvalidEnum")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            Type t = typeof(T).IsGenericType ? Nullable.GetUnderlyingType(typeof(T)) : typeof(T);

            // If T is not an enum to begin with, then must fail without checking anything
            if (!t.IsEnum)
                return false;

            // Valid if T is nullable and value is null
            if (typeof(T).IsGenericType && context.PropertyValue == null)
                return true;

            // Valid if it is defined in the enum
            return Enum.IsDefined(t, context.PropertyValue);
        }
    }

    public class IsUrlValidator : PropertyValidator
    {
        public IsUrlValidator() : base("URI '{Uri}' is not well formed", "InvalidUrl")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var uri = context.PropertyValue as string;

            if (uri == null)
            {
                context.MessageFormatter.AppendArgument("Uri", "");
                return false;
            }

            context.MessageFormatter.AppendArgument("Uri", uri.Replace("{", "{{").Replace("}", "}}"));

            return Uri.IsWellFormedUriString(uri, UriKind.Absolute);
        }
    }

    public class IsReferenceValidator : PropertyValidator
    {
        private IMongoManager mongo;

        public IsReferenceValidator(IMongoManager mongo) : base("Object id '{ObjectId}' not found in collection '{CollectionName}'", "InvalidReference")
        {
            this.mongo = mongo;
        }

        public override IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context)
        {
            context.MessageFormatter.AppendPropertyName(context.PropertyDescription);

            IList<ObjectId> ids;
            var failures = new List<ValidationFailure>();

            if (context.PropertyValue is ObjectId)
            {
                ids = new ObjectId[] { (ObjectId)context.PropertyValue };
            }
            else
            {
                ids = context.PropertyValue as IList<ObjectId>;

                if (ids == null)
                {
                    this.ErrorMessageSource = new StaticStringSource("{PropertyName} is not an ObjectId or List<ObjectId>");
                    failures.Add(CreateValidationError(context));
                    return failures;
                }
            }

            var collectionType = mongo.GetReferencedCollectionType((PropertyInfo)context.Rule.Member);

            if (collectionType == null)
            {
                throw new ArgumentException("Unable to determine referenced collection type of property '{0}'", context.PropertyName);
            }

            context.MessageFormatter.AppendArgument("CollectionName", collectionType.Name);

            foreach (var id in ids)
            {
                context.MessageFormatter.AppendArgument("ObjectId", id);

                if (!mongo.ItemExistsInCollection(collectionType, id))
                {
                    var failure = CreateValidationError(context);

                    failure.CustomState = collectionType;
                    failures.Add(failure);
                }
            }

            return failures;
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            return true;
        }
    }
}

