using FakeEclipseGraphQLApi;
using FakeEclipseGraphQLApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add GraphQL Server with mock schema definition
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddUnionType<IPersonByCriteria_PersonByCriteria_Results>()
    .AddType<Person>();

var app = builder.Build();

// Expose the integration endpoint matching original client configurations
app.MapGraphQL("/integration");

// Default to a development-friendly greeting on the root path
app.MapGet("/", () => "Eclipse GraphQL Mock Server is running! Access the GraphQL IDE at /integration");

app.Run("http://localhost:5050");