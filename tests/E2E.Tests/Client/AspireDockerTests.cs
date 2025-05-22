namespace E2E.Tests.Client;

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Docker.DotNet;
using Docker.DotNet.Models;

public class AspireDockerTests : IAsyncLifetime
{
    private readonly DockerClient _dockerClient;
    private const string ContainerName = "aspire_test_container";
    private const string ImageName = "my-aspire-app:latest";
    private const string LogFilePath = "/app/logs/test.log";
    private const string OutputDir = "/app/output";

    public AspireDockerTests()
    {
        _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
            .CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
        {
            FromImage = ImageName
        }, null, new Progress<JSONMessage>());

        await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = ImageName,
            Name = ContainerName,
            HostConfig = new HostConfig
            {
                Binds = new[] { $"{Path.GetTempPath()}:/app/logs:rw" }
            }
        });

        await _dockerClient.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters());
    }

    [Fact]
    public async Task ValidateAspireLogsAndOutput()
    {
        await Task.Delay(TimeSpan.FromSeconds(10)); // Wait for the application to generate logs

        string localLogPath = Path.Combine(Path.GetTempPath(), "test.log");
        Assert.True(File.Exists(localLogPath), "Log file was not created.");

        string logContents = await File.ReadAllTextAsync(localLogPath);
        Assert.Contains("Application started", logContents);
    }

    public async Task DisposeAsync()
    {
        await _dockerClient.Containers.StopContainerAsync(ContainerName, new ContainerStopParameters());
        await _dockerClient.Containers.RemoveContainerAsync(ContainerName, new ContainerRemoveParameters { Force = true });
    }
}
