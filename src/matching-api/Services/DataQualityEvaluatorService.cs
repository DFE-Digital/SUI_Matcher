using Shared.Models;

namespace MatchingApi.Services;

public static class DataQualityEvaluatorService
{
    sealed record PropertyMapping(
        string Name,
        string? RequiredError,
        string? InvalidError,
        Action SetInvalid,
        Action SetNotProvided
    );

    public static DataQualityResult ToQualityResult(PersonSpecification spec,
        IReadOnlyList<ValidationResponse.ValidationResult> validationResults)
    {
        var result = new DataQualityResult();

        var propertyMappings = new[]
        {
            new PropertyMapping(
                Name: nameof(PersonSpecification.Given),
                RequiredError: PersonValidationConstants.GivenNameRequired,
                InvalidError: PersonValidationConstants.GivenNameInvalid,
                SetInvalid: () => { },
                SetNotProvided: () => { }
            ),
            new PropertyMapping(
                Name: nameof(PersonSpecification.Family),
                RequiredError: PersonValidationConstants.FamilyNameRequired,
                InvalidError: PersonValidationConstants.FamilyNameInvalid,
                SetInvalid: () => { },
                SetNotProvided: () => { }
            ),
            new PropertyMapping(
                Name: nameof(PersonSpecification.BirthDate),
                RequiredError: PersonValidationConstants.BirthDateRequired,
                InvalidError: PersonValidationConstants.BirthDateInvalid,
                SetInvalid: () => { },
                SetNotProvided: () => { }
            ),
            new PropertyMapping(
                Name: nameof(PersonSpecification.Gender),
                RequiredError: null,
                InvalidError: PersonValidationConstants.GenderInvalid,
                SetInvalid: () => { spec.Gender = null; },
                SetNotProvided: () => { }
            ),
            new PropertyMapping(
                Name: nameof(PersonSpecification.Phone),
                RequiredError: null,
                InvalidError: PersonValidationConstants.PhoneInvalid,
                SetInvalid: () => { spec.Phone = null; },
                SetNotProvided: () => { }
            ),
            new PropertyMapping(
                Name: nameof(PersonSpecification.Email),
                RequiredError: null,
                InvalidError: PersonValidationConstants.EmailInvalid,
                SetInvalid: () => { spec.Email = null; },
                SetNotProvided: () => { }
            ),
            new PropertyMapping(
                Name: nameof(PersonSpecification.AddressPostalCode),
                RequiredError: null,
                InvalidError: PersonValidationConstants.PostCodeInvalid,
                SetInvalid: () => { spec.AddressPostalCode = null; },
                SetNotProvided: () => { }
            )
        };

        foreach (var mapping in propertyMappings)
        {
            var resultProp = typeof(DataQualityResult).GetProperty(mapping.Name);
            var specProp = typeof(PersonSpecification).GetProperty(mapping.Name);

            if (resultProp == null) continue;

            var currentQuality = (QualityType)resultProp.GetValue(result)!;

            if (currentQuality != QualityType.Valid) continue;

            var vResult = validationResults.FirstOrDefault(v =>
                v.MemberNames.Contains(mapping.Name, StringComparer.OrdinalIgnoreCase));

            if (vResult != null)
            {
                if (mapping.RequiredError != null && vResult.ErrorMessage == mapping.RequiredError)
                {
                    resultProp.SetValue(result, QualityType.NotProvided);
                    mapping.SetNotProvided();
                }
                else if (mapping.InvalidError != null && vResult.ErrorMessage == mapping.InvalidError)
                {
                    resultProp.SetValue(result, QualityType.Invalid);
                    mapping.SetInvalid();
                }
            }
            else if (specProp != null && (string.IsNullOrEmpty(specProp.GetValue(spec)?.ToString())))
            {
                resultProp.SetValue(result, QualityType.NotProvided);
                mapping.SetNotProvided();
            }
        }

        return result;
    }
}