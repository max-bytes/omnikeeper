using FluentValidation.Results;

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
    }
}
