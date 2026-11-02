using Trama.Application.Knowledge;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class TagCaseRegressionTests
{
    [Theory]
    [InlineData("so_tag", "SO_TAG", "text")]
    [InlineData("SO_TAG", "so_tag", "text")]
    [InlineData("so_tag", "SO_TAG", "tag")]
    [InlineData("SO_TAG", "so_tag", "tag")]
    [InlineData("so_tag", "SO_TAG", "both")]
    [InlineData("SO_TAG", "so_tag", "both")]
    public async Task SavedTag_IsFoundInBothSearchFieldsRegardlessOfCase(string saved, string query, string field)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = new NotebookSnapshot(Guid.NewGuid(), "Título sem a palavra da tag",
            [new NotebookTextBlock(Guid.NewGuid(), "Conteúdo independente")]);
        await using (var writer = data.CreateContext()) await new SqliteNotebookRepository(writer).SaveAsync(page);
        var path = Path.Combine(data.DirectoryPath, "trama.db");
        await new SqliteKnowledgeDiscovery(path).SetPageTagsAsync(page.Id, [saved]);

        var results = await new SqliteKnowledgeDiscovery(path).SearchAsync(
            field == "tag" ? string.Empty : query, field == "text" ? null : query);

        Assert.Equal(page.Id, Assert.Single(results).PageId);
    }
}
