using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace SUI.Client.Core.Infrastructure.CsvParsers;

public record CsvMatchDataOptions
{
    public const string SectionName = "CsvMatchData";

    [Required]
    public required string DateFormat { get; init; }

    [ValidateObjectMembers]
    public required Headers ColumnMappings { get; init; }

    public record Headers
    {
        [Required]
        public required string Id { get; init; }

        [Required]
        public required string Given { get; init; }

        [Required]
        public required string Family { get; init; }

        [Required]
        public required string BirthDate { get; init; }

        [Required]
        public required string Postcode { get; init; }

        [Required]
        public string Email { get; init; } = "Email";

        [Required]
        public string Gender { get; init; } = "Gender";

        [Required]
        public string Phone { get; init; } = "Phone";
        public string? NhsNumber { get; init; }
    }
}
