# SUI Client Storage Process Job

The Storage Process Job can be built and run as a Docker image from the repository root.

## Build the image

```bash
docker build \
  -f src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile \
  -t sui-client-storage-process-job:local \
  .
```

The Dockerfile only accepts the matching API base address as a build argument. CSV mappings and processing mode must be provided at runtime so deployment-specific schemas are not stored in the image or repository.

```bash
docker build \
  -f src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile \
  -t sui-client-storage-process-job:local \
  --build-arg MATCH_API_BASE_ADDRESS=http://host.docker.internal:5000 \
  .
```

`MATCH_API_BASE_ADDRESS` has no Dockerfile default. Supply it as a build argument when baking it into the image, or provide `StorageProcessJob__MatchApiBaseAddress` as a runtime environment variable.

Use `StorageProcessJob__ProcessingMode=Matching` for the existing flow or `StorageProcessJob__ProcessingMode=Reconciliation` when demographic and address-derived output is required.

Reconciliation mode appends generic derived columns for age group, four address-history comparisons, source-number presence, and whether the source number equals the matched number. Deployment-specific column mappings must be supplied through protected runtime configuration.

## Run locally

Start Azurite first. From the repository root:

```bash
docker compose up -d azurite
```

> [!NOTE]
> **For Linux users:** `host.docker.internal` is not automatically mapped on Linux. Add `--add-host=host.docker.internal:host-gateway` to the `docker run` command to enable it.


The production image cannot process a CSV with only the Azurite connection string. At minimum it needs the matching API base address, CSV date format, and source column mappings.

If `MATCH_API_BASE_ADDRESS` was supplied as a build argument, you can omit `StorageProcessJob__MatchApiBaseAddress` from the runtime environment. CSV mappings and processing mode still need to be supplied at runtime.

Pass runtime settings with the .NET configuration keys:

```bash
docker run --rm \
  -e 'AzureWebJobsStorage=UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://host.docker.internal' \
  -e StorageProcessJob__MatchApiBaseAddress=http://host.docker.internal:5000 \
  -e StorageProcessJob__ProcessingMode=Matching \
  -e CsvMatchData__DateFormat=yyyy-MM-dd \
  -e CsvMatchData__AddressHistoryFormat=TildePipeChronological \
  -e CsvMatchData__ColumnMappings__Id="${SOURCE_ID_COLUMN}" \
  -e CsvMatchData__ColumnMappings__Given="${GIVEN_NAME_COLUMN}" \
  -e CsvMatchData__ColumnMappings__Family="${FAMILY_NAME_COLUMN}" \
  -e CsvMatchData__ColumnMappings__BirthDate="${BIRTH_DATE_COLUMN}" \
  -e CsvMatchData__ColumnMappings__Email="${EMAIL_COLUMN}" \
  -e CsvMatchData__ColumnMappings__Postcode="${POSTCODE_COLUMN}" \
  -e CsvMatchData__ColumnMappings__Gender="${GENDER_COLUMN}" \
  -e CsvMatchData__ColumnMappings__Phone="${PHONE_COLUMN}" \
  -e CsvMatchData__ColumnMappings__NhsNumber="${SOURCE_NUMBER_COLUMN}" \
  -e CsvMatchData__ColumnMappings__Address="${ADDRESS_HISTORY_COLUMN}" \
  sui-client-storage-process-job:local
```

Runtime environment variables override values from `appsettings.json` and the baked matching API base address.

Set `CsvMatchData__AddressHistoryFormat=SemicolonCommaNewestFirst` when the mapped source address column contains semicolon-separated addresses in newest-first order, with each address represented as comma-separated address lines. The default `TildePipeChronological` format keeps the existing `|`-separated records with `~`-separated fields.

`host.docker.internal` lets the job container reach services exposed by the host machine. `UseDevelopmentStorage=true` without `DevelopmentStorageProxyUri` points to `127.0.0.1` inside the job container and will not reach Azurite running outside that container.


The job reads from the `storage-process-job` queue. Create that queue in Azurite before running the job if it does not already exist.
