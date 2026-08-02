using FluentValidation;

namespace Ephemon.Extensions;

public static class ValidatorExtensions
{
    extension<TEntity>(IRuleBuilderOptions<TEntity, string?> ruleBuilder)
    {
        public IRuleBuilderOptions<TEntity, string?> Base64String()
        {
            return ruleBuilder
                .Must(base64String =>
                {
                    if (string.IsNullOrWhiteSpace(base64String))
                        return false;

                    var buffer = new Span<byte>(new byte[base64String.Length]);
                    return Convert.TryFromBase64String(base64String, buffer, out _);
                });
        }
    }
}