using System.Text.Json;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Models;

public class SyncPersistenceFieldsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Deserialize_LegacyAccountJson_WithoutSyncFields_LeavesThemNull()
    {
        const string json = """
            [
              {
                "id": "acc-1",
                "name": "Giro",
                "type": 0,
                "isArchived": false,
                "openingBalance": 100.5
              }
            ]
            """;

        var accounts = JsonSerializer.Deserialize<List<Account>>(json, JsonOptions);

        Assert.NotNull(accounts);
        Assert.Single(accounts!);
        Assert.Equal("acc-1", accounts[0].Id);
        Assert.Equal("Giro", accounts[0].Name);
        Assert.Null(accounts[0].ExternalId);
        Assert.Null(accounts[0].Source);
        Assert.Null(accounts[0].UpdatedAt);
    }

    [Fact]
    public void Deserialize_LegacyTransactionJson_WithoutSyncFields_LeavesThemNull()
    {
        const string json = """
            [
              {
                "id": "tx-1",
                "betrag": 12.3,
                "titel": "Brot",
                "datum": "2026-03-10T00:00:00",
                "kategorieId": "cat-1",
                "typ": 1,
                "verwendungszweck": ""
              }
            ]
            """;

        var transactions = JsonSerializer.Deserialize<List<Transaction>>(json, JsonOptions);

        Assert.NotNull(transactions);
        Assert.Single(transactions!);
        Assert.Equal("tx-1", transactions[0].Id);
        Assert.Null(transactions[0].ExternalId);
        Assert.Null(transactions[0].Source);
        Assert.Null(transactions[0].UpdatedAt);
    }

    [Theory]
    [InlineData(typeof(Account))]
    [InlineData(typeof(Transaction))]
    [InlineData(typeof(Category))]
    [InlineData(typeof(RecurringTransaction))]
    [InlineData(typeof(SparZiel))]
    public void Roundtrip_PersistsExternalIdSourceAndUpdatedAt(Type entityType)
    {
        var updatedAt = new DateTime(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc);
        object entity = entityType.Name switch
        {
            nameof(Account) => new Account
            {
                Id = "acc-1",
                Name = "Giro",
                ExternalId = "ck-acc-1",
                Source = EntitySources.CloudKit,
                UpdatedAt = updatedAt
            },
            nameof(Transaction) => new Transaction
            {
                Id = "tx-1",
                Titel = "Test",
                Betrag = 1m,
                ExternalId = "ob-tx-1",
                Source = EntitySources.OpenBanking,
                UpdatedAt = updatedAt
            },
            nameof(Category) => new Category
            {
                Id = "cat-1",
                Name = "Essen",
                ExternalId = "ck-cat-1",
                Source = EntitySources.CloudKit,
                UpdatedAt = updatedAt
            },
            nameof(RecurringTransaction) => new RecurringTransaction
            {
                Id = "rt-1",
                Titel = "Miete",
                Betrag = 800m,
                ExternalId = "ck-rt-1",
                Source = EntitySources.CloudKit,
                UpdatedAt = updatedAt
            },
            nameof(SparZiel) => new SparZiel
            {
                Id = "sz-1",
                Titel = "Urlaub",
                ZielBetrag = 1000m,
                ExternalId = "ck-sz-1",
                Source = EntitySources.CloudKit,
                UpdatedAt = updatedAt
            },
            _ => throw new InvalidOperationException(entityType.Name)
        };

        var json = JsonSerializer.Serialize(entity, entityType, JsonOptions);
        var loaded = JsonSerializer.Deserialize(json, entityType, JsonOptions);

        Assert.NotNull(loaded);
        Assert.Equal(GetProp(entity, "ExternalId"), GetProp(loaded!, "ExternalId"));
        Assert.Equal(GetProp(entity, "Source"), GetProp(loaded!, "Source"));
        Assert.Equal(GetProp(entity, "UpdatedAt"), GetProp(loaded!, "UpdatedAt"));
    }

    private static object? GetProp(object instance, string name)
        => instance.GetType().GetProperty(name)!.GetValue(instance);
}
