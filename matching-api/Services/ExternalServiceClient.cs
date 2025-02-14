using MatchingApi.Models;
using Shared.Models;

namespace MatchingApi.Services;

public class ExternalServiceClient(HttpClient httpClient)
{
    public async Task<SearchResult?> PerformQuery(PersonSpecification personSpecification)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/search");
        request.Content = JsonContent.Create(new SearchQuery
        {
            Given = [personSpecification.Given!],
            Family = personSpecification.Family,
            Email = personSpecification.Email,
            Gender = personSpecification.Gender,
            Phone = personSpecification.Phone,
            Birthdate = ["eq" + personSpecification.BirthDate.ToString("yyyy-MM-dd")],
            AddressPostalcode = personSpecification.AddressPostalCode
        });
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SearchResult>();
    }
}