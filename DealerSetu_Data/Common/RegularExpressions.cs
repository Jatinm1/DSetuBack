using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DealerSetu_Data.Common
{
    public class RegularExpressions
    {
        [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        public class CustomRegularExpressionAttribute : RegularExpressionAttribute
        {
            public CustomRegularExpressionAttribute() : base("^[a-zA-Z0-9 &_\\-\\/\\\\().]*$")
            {
                ErrorMessage = "Can contain letters, numbers, spaces, underscore, ampersand, hyphen, slash, period, and parentheses only.";
            }
            //public CustomRegularExpressionAttribute() : base(@"^[a-zA-Z0-9]*$")
            //{
            //    ErrorMessage = "Can contain letters and numbers.";
            //}
        }
        [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        public class CustomDateTimeFormatAttribute : RegularExpressionAttribute
        {
            public CustomDateTimeFormatAttribute() : base(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$")
            {
                ErrorMessage = "Can contain letters and numbers only.";
            }
        }

        public class AlphaNumericAttribute : ValidationAttribute
        {
            // The IsValid method is overridden to provide custom validation logic
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                // If the value is null, it is considered valid
                if (value == null)
                {
                    return ValidationResult.Success;
                }

                // If the value is a string, apply the regular expression validation
                if (value is string str)
                {
                    // Check if the string matches the specified pattern
                    if (Regex.IsMatch(str, @"^[a-zA-Z0-9\s\.\-_]*$"))
                    {
                        return ValidationResult.Success;
                    }
                    else
                    {
                        // Return an error message if the string does not match the pattern
                        return new ValidationResult("The field must contain only letters, numbers, spaces, hyphens, periods, and underscores.");
                    }
                }

                // If the value is an array of strings, validate each element of the array
                if (value is string[] array)
                {
                    foreach (var item in array)
                    {
                        // Check each string in the array against the pattern
                        if (item != null && !Regex.IsMatch(item, @"^[a-zA-Z0-9\s\.\-_]*$"))
                        {
                            // Return an error message if any string does not match the pattern
                            return new ValidationResult("The field must contain only letters, numbers, spaces, hyphens, periods, and underscores.");
                        }
                    }
                    return ValidationResult.Success;
                }

                // If the value is an integer, it is considered valid
                if (value is int)
                {
                    return ValidationResult.Success;
                }

                // If the value is not a string, an array of strings, or an integer, return an error message
                return new ValidationResult("Invalid field type.");
            }
        }



        public class CustomRegexArrayAttribute : ValidationAttribute
        {
            private readonly string _regexPattern;

            public CustomRegexArrayAttribute(string regexPattern)
            {
                _regexPattern = regexPattern;
            }

            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                if (value == null)
                {
                    return ValidationResult.Success;
                }

                if (value is string[] stringArray)
                {
                    var invalidItems = stringArray.Where(item => !IsValidItem(item)).ToList();

                    if (invalidItems.Any())
                    {
                        var invalidItemsString = string.Join(", ", invalidItems);
                        return new ValidationResult($"The following items in the array are not valid according to the regex pattern '{_regexPattern}': {invalidItemsString}");
                    }

                    return ValidationResult.Success;
                }

                return new ValidationResult("The value provided is not a string array.");
            }

            private bool IsValidItem(string item)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    return System.Text.RegularExpressions.Regex.IsMatch(item, _regexPattern);
                }
                return true;
            }
        }
    }
}
