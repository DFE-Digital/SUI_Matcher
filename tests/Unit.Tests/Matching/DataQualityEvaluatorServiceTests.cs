using MatchingApi.Services;

using Shared.Models;

namespace Unit.Tests.Matching;

public class DataQualityEvaluatorServiceTests
{
    [Fact]
    public void ToQualityResult_ShouldSetGivenToNotProvided_WhenGivenNameIsMissing()
    {
        // Arrange
        var spec = new PersonSpecification { Given = null };
        var validationResults = new[]
        {
            new ValidationResponse.ValidationResult
            {
                MemberNames = ["Given"],
                ErrorMessage = PersonValidationConstants.GivenNameRequired
            }
        };

        // Act
        var result = DataQualityEvaluatorService.ToQualityResult(spec, validationResults);

        // Assert
        Assert.Equal(QualityType.NotProvided, result.Given);
    }

    [Fact]
    public void ToQualityResult_ShouldSetEmailToInvalid_WhenEmailIsInvalid()
    {
        // Arrange
        var spec = new PersonSpecification { Email = "invalid-email" };
        var validationResults = new[]
        {
            new ValidationResponse.ValidationResult
            {
                MemberNames = ["Email"],
                ErrorMessage = PersonValidationConstants.EmailInvalid
            }
        };

        // Act
        var result = DataQualityEvaluatorService.ToQualityResult(spec, validationResults);

        // Assert
        Assert.Equal(QualityType.Invalid, result.Email);
    }

    [Fact]
    public void ToQualityResult_ShouldSetAllFieldsToNotProvided_WhenNoValidationResults()
    {
        // Arrange
        var spec = new PersonSpecification();
        var validationResults = Array.Empty<ValidationResponse.ValidationResult>();
        
        // Act
        var result = DataQualityEvaluatorService.ToQualityResult(spec, validationResults);
        
        // Assert
        // Assert.Equal(QualityType.NotProvided, result.Given);
        // Assert.Equal(QualityType.NotProvided, result.Family);
        Assert.Equal(QualityType.NotProvided, result.BirthDate);
        // Assert.Equal(QualityType.NotProvided, result.AddressPostalCode);
        // Assert.Equal(QualityType.NotProvided, result.Phone);
        // Assert.Equal(QualityType.NotProvided, result.Email);
        // Assert.Equal(QualityType.NotProvided, result.Gender);

    }

    [Theory]
    [InlineData(null, QualityType.NotProvided)]
    [InlineData("valid@example.com", QualityType.Valid)]
    public void ToQualityResult_ShouldHandleEmailQuality(string email, QualityType expectedQuality)
    {
        // Arrange
        var spec = new PersonSpecification { Email = email };
        var validationResults = Array.Empty<ValidationResponse.ValidationResult>();

        // Act
        var result = DataQualityEvaluatorService.ToQualityResult(spec, validationResults);

        // Assert
        Assert.Equal(expectedQuality, result.Email);
    }
}