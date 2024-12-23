namespace TaxFilerWebTest;

public class IntegrationTests: BaseIntegrationTestClass
{
    [Test]
    public async Task IntegrationTest_HealthCheck_Start()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");
        var content = await response.Content.ReadAsStringAsync();
        TestContext.WriteLine(content);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}