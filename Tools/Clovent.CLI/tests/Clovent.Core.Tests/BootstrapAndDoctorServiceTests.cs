namespace Clovent.Core.Tests;

using Clovent.Core.Models;
using Clovent.Core.Services;
using Xunit;

public class BootstrapAndDoctorServiceTests
{
    [Fact]
    public void BootstrapService_Execute_WithValidFolder_ShouldSucceed()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "BootstrapTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var sut = new BootstrapService();
            var result = sut.Execute(new BootstrapOptions { RootPath = tempDir });

            Assert.True(result.Success);
            Assert.Contains(result.Messages, m => m.Contains("Repository found"));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void DoctorService_Diagnose_ShouldReturnChecks()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DoctorTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var sut = new DoctorService();
            var result = sut.Diagnose(tempDir);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Checks);
            Assert.Contains(result.Checks, c => c.Name == "Workspace Root Directory" && c.IsPassed);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
