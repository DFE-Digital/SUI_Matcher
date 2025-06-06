# 002: Distribution Strategy for .NET CLI Client Tools

### Date 
2025-04-16

### Status
Proposed

### Author
Stuart Maskell

### Context

The purpose of this document is to outline and evaluate potential strategies for distributing a newly developed .NET CLI tool to public users (LA's in our case). 
The goal is to choose a method that balances ease of use for the target audience and maintainability for the DfE.

The Decision Drivers are:

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
        * The NuGet.org account is managed by the DfE, which may require some coordination for updates around certification and security.
        * As seen before, Administrators may leave the DfE and the account may not be managed properly. This is mitigated by having more administrators.

2.  Deploy as a .NET NuGet Tool to DfE GitHub Packages:
    * Pros:
        * Widely used and is within GitHub to distribute tools, libraries and containers.
    * Cons:
        * Requires a Personal Access Token (PAT) for users to download from the NuGet feed, which is not ideal for public users.
3. Azure Artifacts to Store NuGet File tool in Feed:
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

5. Store as a self contained .NET Executable onto a Storage Area (Already in GitHub pipeline):
    * Pros:
        * Users can run the `.exe` directly without needing the .NET CLI.
        * Self-contained means they also do not need a specific .NET version installed on the machine it's running on.
        * (Assumption) Most System Administrators might be more familiar with using `.exe` files.
        * More flexible configuration options as it is not a single file tool.
    * Cons:
        * Client would need to manually download and run the self-contained `.exe`.
        * Larger file size due to being a self-contained application.
        * Requires users to manually remove old versions and find/add new ones. This also requires more instructions.

6.  MyGet to Store .NET NuGet tool in Feed:
    * Pros:
        * Publicly accessible feed with no authentication required.
    * Cons:
        * Requires creating and setting up an account.
        * Storing login credentials safely within DfE for multiple users.
        * Requires creating and setting up a pipeline to push the package to MyGet.

### Additional Considerations

* **Tool Updates:** Regardless of the chosen method, a clear communication strategy for new versions and updates needs to be established for the public users.
* **Tool usage:** Requires specific versions of .NET to be installed on the user's machine.
* **Documentation:** Comprehensive documentation on how to install and use the tool will be crucial for any chosen method.
* **Support:** A mechanism for users to report issues and receive support should be considered.
* **Security Scanning:** Any publicly distributed software should undergo appropriate security scanning and review processes.

### Proposed Decision:

Option 1, Deploy as a .NET NuGet Tool to NuGet.org for the following reasons:

### Rationale:

* It is the most straightforward and user-friendly option for public users.
* It allows for easy updates and version management.
* It is a widely accepted method for distributing .NET tools.
* It minimizes the complexity of setup and management for the DfE.
* Recommended approach by the DfE for our CLI tools.