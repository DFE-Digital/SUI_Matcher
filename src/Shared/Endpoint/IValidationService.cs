using Shared.Models;

namespace Shared.Endpoint;

public interface IValidationService
{
    ValidationResponse Validate(object obj);
}