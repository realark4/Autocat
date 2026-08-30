using System.IO;
using System.Threading.Tasks;
using AutoCadAiPlugin.Infrastructure.Security;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class SecurityAndVaultTests
{
    [Fact]
    public async Task DpapiSecureStorage_EncryptsAndRetrievesSecretsCorrectly()
    {
        string tempSecretFile = Path.Combine(Path.GetTempPath(), $"secrets_{System.Guid.NewGuid()}.dat");
        var storage = new DpapiSecureStorage(tempSecretFile);

        try
        {
            await storage.SaveSecretAsync("API_KEY_OpenAI", "mock-dummy-secret-vault-test-12345");
            string? secret = await storage.GetSecretAsync("API_KEY_OpenAI");

            Assert.Equal("mock-dummy-secret-vault-test-12345", secret);

            // Raw file should NOT contain plain text key
            byte[] fileBytes = File.ReadAllBytes(tempSecretFile);
            string rawContent = System.Text.Encoding.UTF8.GetString(fileBytes);
            Assert.DoesNotContain("mock-dummy-secret-vault-test-12345", rawContent);
        }
        finally
        {
            if (File.Exists(tempSecretFile)) File.Delete(tempSecretFile);
        }
    }
}
