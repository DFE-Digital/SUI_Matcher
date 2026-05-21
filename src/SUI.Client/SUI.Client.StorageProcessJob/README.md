# SUI Client Storage Process Job

The Storage Process Job can be built and run as a Docker image from the repository root.

## Build the image

```bash
docker build \
  -f src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile \
  -t sui-client-storage-process-job:local \
  .
```

CSV mapping and the matching API base address can be baked into the image with build args. This mirrors how a workflow can pass values to `docker build` before pushing the image to ACR.

```bash
docker build \
  -f src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile \
  -t sui-client-storage-process-job:local \
  --build-arg MATCH_API_BASE_ADDRESS=http://host.docker.internal:5000 \
  --build-arg CSV_DATE_FORMAT=yyyy-MM-dd \
  --build-arg CSV_COLUMN_ID=Id \
  --build-arg CSV_COLUMN_GIVEN=GivenName \
  --build-arg CSV_COLUMN_FAMILY=FamilyName \
  --build-arg CSV_COLUMN_BIRTH_DATE=DOB \
  --build-arg CSV_COLUMN_EMAIL=EMAIL \
  --build-arg CSV_COLUMN_POSTCODE=POSTCODE \
  --build-arg CSV_COLUMN_GENDER=GENDER \
  --build-arg CSV_COLUMN_PHONE=PHONE \
  --build-arg CSV_COLUMN_NHS_NUMBER=NHS_NUMBER \
  .
```

`MATCH_API_BASE_ADDRESS` has no Dockerfile default. Supply it as a build arg when baking it into the image, or provide `StorageProcessJob__MatchApiBaseAddress` as a runtime environment variable.

The build args map to .NET configuration environment variables in the Dockerfile. For example, `CSV_COLUMN_GIVEN=GivenName` becomes `CsvMatchData__ColumnMappings__Given=GivenName`, which .NET binds to `CsvMatchData:ColumnMappings:Given`.

## Run locally

Start Azurite first. From the repository root:

```bash
docker compose up -d azurite
```

If the CSV mapping and matching API base address were supplied as build args, run the job with only the local Azurite connection:

```bash
docker run --rm \
  -e 'AzureWebJobsStorage=UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://host.docker.internal' \
  sui-client-storage-process-job:local
```

Runtime environment variables override values baked into the image. To test overrides locally, pass the .NET configuration keys with `-e`:

```bash
docker run --rm \
  -e 'AzureWebJobsStorage=UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://host.docker.internal' \
  -e StorageProcessJob__MatchApiBaseAddress=http://host.docker.internal:5000 \
  -e CsvMatchData__DateFormat=yyyy-MM-dd \
  -e CsvMatchData__ColumnMappings__Id=Id \
  -e CsvMatchData__ColumnMappings__Given=GivenName \
  -e CsvMatchData__ColumnMappings__Family=FamilyName \
  -e CsvMatchData__ColumnMappings__BirthDate=DOB \
  -e CsvMatchData__ColumnMappings__Email=EMAIL \
  -e CsvMatchData__ColumnMappings__Postcode=POSTCODE \
  -e CsvMatchData__ColumnMappings__Gender=GENDER \
  -e CsvMatchData__ColumnMappings__Phone=PHONE \
  -e CsvMatchData__ColumnMappings__NhsNumber=NHS_NUMBER \
  sui-client-storage-process-job:local
```

`host.docker.internal` lets the job container reach services exposed by the host machine. `UseDevelopmentStorage=true` without `DevelopmentStorageProxyUri` points to `127.0.0.1` inside the job container and will not reach Azurite running outside that container.

The job reads from the `storage-process-job` queue. Create that queue in Azurite before running the job if it does not already exist.
