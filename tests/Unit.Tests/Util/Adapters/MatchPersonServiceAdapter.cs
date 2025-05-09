using Shared.Models;

using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;
using SUI.Core;
using SUI.Core.Services;

namespace Unit.Tests.Util.Adapters;

public class MatchPersonServiceAdapter(IMatchingService matchingService) : IMatchPersonApiService
{
    public async Task<PersonMatchResponse?> MatchPersonAsync(MatchPersonPayload payload)
    {
        return await matchingService.SearchAsync(new SUI.Core.Domain.PersonSpecification
        {
            AddressPostalCode = payload.AddressPostalCode,
            Family = payload.Family,
            Gender = payload.Gender,
            BirthDate = payload.BirthDate.ToDateOnly(Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish),
            Email = payload.Email,
            Given = payload.Given,
            Phone = payload.Phone,
        });
    }
}