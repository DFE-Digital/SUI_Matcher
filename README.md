# CSC_Single_Unique_Identifier

Repo for the Single Unique Identifier team which is currently looking at testing the ability to match a persons records to the NHS number in order to understand if this could be implemented as a single unique identifier for children in the future.

Pre-reqs
https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=linux&pivots=dotnet-cli

Install .net SDK v9. Instructions for MacOS below.

```curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 9.0.102 --install-dir "$HOME/.dotnet
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc && source ~/.zshrc && echo $PATH
```

To build and run the project:
```
dotnet build sui-matching.sln
dotnet run --project app-host/AppHost.csproj
```
Run simple test:
```
curl -H 'Content-Type: application/json' \
      -d '{ "given":"octavia","family":"chislett", "birthdate": "2008-09-20"}' \
      -X POST \
      http://localhost:5000/matching/api/v1/matchperson
```
If you have errors connecting to the aspire host page you may need to run the below commands:
```
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Unit and integration testing

#### Prerequisites

Now running using the environmental files. All non secret material is located in the environment appsettings file. By default all external connectivity is stubbed out. If you want to use the NHS integration environment you will need to set a private key.

- Create `.env` file in the project root (this is necessary to use the stub secrets manager, like Azure KeyVault)

Add the following to the .env file and add in the secret values inside the quotes. Make sure the private key is in the PKCS#1 format. if it is in PKCS#8 you can change it with openssl as shown below. Be sure to include the prefix and suffix.

```properties
export NhsAuthConfig__NHS_DIGITAL_PRIVATE_KEY="-----BEGIN RSA PRIVATE KEY-----
{Your Private Key}
-----BEGIN RSA PRIVATE KEY-----"

export NhsAuthConfig__NHS_DIGITAL_CLIENT_ID=""
```
To change your key into correct format:
```
openssl rsa -in originalkey.pem -traditional -out newkey.pem
```
Then run the command (mac):
```
source .env
```

#### Running

- To run the whole test suite via the terminal:

```
dotnet test --settings tests.runsettings
```

or individually:

```
cd sui-tests
dotnet test <path>/<to>/<test-class>
```

