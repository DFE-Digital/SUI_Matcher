# SUI UI Prototype

The purpose of this prototype is to show a browser based graphical user interface of how the SUI Client Watcher CLI works.
This currently only works when running locally and is not deployed anywhere on the web.

## How to get this prototype running

### Prerequisites

- You will need to have setup your machine to run NHS PDS. See the getting started readme in the documents folder of this project.
- Node JS installed on your machine. Recommended to use latest LTS release.

### Running the application.

1. Add the following to the 'Yarp' project Program.cs
    ```csharp
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowLocalhost", policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
    ```
    and then 
    ```csharp
    app.UseCors("AllowLocalhost");
    ```
2. Run the app host dotnet project.
3. Create a '.env' file in this directory and add this line `VITE_API_BASE_URL=http://localhost:5000` and save.
   This is the base URL for the API that the prototype will call.
4. Run `npm run dev` within this directory and go to the browser URL http://localhost:5173
5. There are 2 tabs on the UI that you can use to get data, enter details to get results.
