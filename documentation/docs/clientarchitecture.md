# Client Architecture

## High Level View

The client is a file processing application which watches for files in a directory, processes it and makes requests to the SUI application based on the records.

### Assumptions

* Input file format will be a CSV.
* Files will be located in a directory on a server.
* Client will have direct network access to the server.
* All communication will be internal to Azure.

### Features

#### File Watcher
Watches a directory for a file. When file appears that matches the criteria it moves to load it.

Configuration Options:

* File prefix
* File suffix
* Excluded files
* Modification time

#### CLI Tool
Ability to run the tool via cmdline.

#### File Loader
Loads the file and checks initial configurations. Should assert that:

* File in format expected
* Expected field headings exist

#### Record Handler
Loops through each record to make a request to the SUI server.

* If required data fields not there it shouldn't make a request
    * Given
    * Family
    * DOB
* Makes request to SUI server and waits for response.

#### Response Handler
Handles the response coming back from the server. Will need to handle the structure sent back from the server and format it. 

### Outputs

Appends to the loaded CSV file with the NHS number if it existed.

Writes a metadata file corresponding to the original file with additional information about the process:

* Data quality
* Validation issues
* Process stage it completed at
* Match status
* Match score
* Row number

The metadata output file should be named the same as the data output file but with a metadata suffix.