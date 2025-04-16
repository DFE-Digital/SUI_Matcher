# 002: Distribution Strategy for .NET CLI Client Tools

### Date 
2025-04-16

### Status
Draft

### Author
Stuart Maskell

### Context

The purpose of this document is to outline and evaluate potential strategies for distributing a newly developed .NET CLI tool to public users (LA's in our case). 
The goal is to choose a method that balances ease of use for the target audience and maintainability for the DfE.

### Decision Drivers:

* **Ease of Use for Public Users:** The chosen method should be straightforward for users to install, update, and run the tool, minimizing technical barriers.
* **Maintainability for DfE:** The distribution mechanism should allow for efficient deployment of new versions and updates by the development team.
* **Complexity:** The solution should avoid unnecessary complexity in setup and management.
* **Discoverability:** Users should be able to easily find and access the tool.

### Considered Options

1.  Deploy as a .NET NuGet Tool to NuGet.org:
    * Pros:
        * Does not require any user authentication to download.
        * Simple for the user to install, change version and update using `dotnet tool update -g <tool-name>`.
    * Cons:
        * Current uncertainty regarding the management of the existing `DfE.Digital` organization on NuGet.org. Previous maintainers have left the DfE.
        * Creating a new organization managed by a single individual poses a risk of lost access over time.
        * Requires specific versions of .NET to be installed on the user's machine.

2.  Deploy as a .NET NuGet Tool to DfE GitHub Packages:
    * Pros:
        * Widely used and is within GitHub to distribute tools, libraries and containers.
    * Cons:
        * Requires a Personal Access Token (PAT) for users to download from the NuGet feed, which is not ideal for public users.
3. Azure Artifacts to Store NuGet File in Feed:
    * Pros:
        * Publicly accessible feed with no authentication required.
        * Used widely across the Dfe and managed by the DfE.
    * Cons:
        * Requires creating and managing an Azure DevOps organization just for a NuGet feed. This is mitigated mostly by using the existing DfE DevOps organization.
        * Requires creating and setting up a pipeline to push the package to Azure DevOps. However, this is a simple process.

4. Store as a .NET NuGet Tool onto a Storage Area (e.g. DfE Azure blob Storage):
    * Pros:
        * DfE can push new versions to the storage area.
    * Cons:
        * Client would need to manually download the `.nupkg` file.
        * Client would need to manually run `dotnet tool install -g <tool-name> --source <local-directory>`. This requires more instructions.

5. Store as a .NET Executable onto a Storage Area (Already in Pipeline):
    * Pros:
        * Users can run the `.exe` directly without needing the .NET CLI.
        * (Assumption) Most System Administrators might be more familiar with using `.exe` files.
        * More flexible configuration options as it is not a single file tool.
    * Cons:
        * Client would need to manually download and run the self-contained `.exe`.
        * Larger file size due to being a self-contained application.
        * Requires users to manually remove old versions and find/add new ones. This requires more instructions.

6.  MyGet to Store NuGet File in Feed:
    * Pros:
        * Publicly accessible feed with no authentication required.
    * Cons:
        * Requires creating and setting up an account.
        * Storing login credentials safely within DfE for multiple users.
        * Requires creating and setting up a pipeline to push the package to MyGet.

### Additional Considerations

* **Tool Updates:** Regardless of the chosen method, a clear communication strategy for new versions and updates needs to be established for the public users.
* **Documentation:** Comprehensive documentation on how to install and use the tool will be crucial for any chosen method.
* **Support:** A mechanism for users to report issues and receive support should be considered.
* **Security Scanning:** Any publicly distributed software should undergo appropriate security scanning and review processes.

### Proposed Decision:

TBC:
* The preferred option is to deploy the tool as a .NET NuGet Tool to NuGet.org, as it is the simplest to setup and for LA's to use.
* The second preferred option is to deploy the tool as a .NET NuGet Tool to Azure Artifacts, as it provides a balance between ease of use for LA's and maintainability for the DfE.

### Rationale:

TBC

This ADR will be reviewed and updated as needed based on further information and implementation progress.