using MatchingApi.Services;
using Shared.Models;

namespace Unit.Tests.Matching
{
    public class ValidateServiceTest
    {
        [Theory]
        [InlineData("", "Doe", "2000-01-01", "1234567890", "test@example.com", "male", "AB1 2CD", "Given name is required")]
        [InlineData("John", "", "2000-01-01", "1234567890", "test@example.com", "male", "AB1 2CD", "Family name is required")]
        [InlineData("John", "Doe", "2000-01-01", "invalid-phone", "test@example.com", "male", "AB1 2CD", "Invalid phone number.")]
        [InlineData("John", "Doe", "2000-01-01", "1234567890", "invalid-email", "male", "AB1 2CD", "Invalid email address.")]
        [InlineData("John", "Doe", "2000-01-01", "1234567890", "test@example.com", "invalid-gender", "AB1 2CD", "Gender has to match FHIR standards")]
        [InlineData("John", "Doe", "2000-01-01", "1234567890", "test@example.com", "male", "invalid-postcode", "Invalid postcode.")]
        public void Validate_InvalidData(string given, string family, string birthdate, string phone, string email, string gender, string addresspostalcode, string expectedErrorMessage)
        {
            var validationService = new ValidationService();

            var model = new PersonSpecification
            {
                Given = given,
                Family = family,
                BirthDate = DateOnly.Parse(birthdate),
                Phone = phone,
                Email = email,
                Gender = gender,
                AddressPostalCode = addresspostalcode
            };

            var result = validationService.Validate(model);
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Contains(result.Results, x => x.ErrorMessage == expectedErrorMessage);
        }
    }
}