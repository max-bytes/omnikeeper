using FluentValidation.Results;
using System;

namespace Omnikeeper.GridView.Helper
{
    public static class ValidationHelper
    {
        public static string CreateErrorMessage(ValidationResult validationResult)
        {
            var error = "";
            foreach (var e in validationResult.Errors)
            {
                error += $"{e}. ";
            }

            return error;
        }

        public static Exception CreateException(ValidationResult validationResult)
        {
            return new Exception(CreateErrorMessage(validationResult));
        }
    }
}
