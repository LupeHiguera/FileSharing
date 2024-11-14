namespace UserManagementService.CosmosDb;

using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CosmosDbService<T> where T : class
{
    private readonly Container _container;
    private readonly ILogger<CosmosClient> _logger;

    public CosmosDbService(string endpoint, string key, string databaseId, string containerId, ILogger<CosmosClient> logger)
    {
        var cosmosClient = new CosmosClient(endpoint, key);
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _logger = logger;
    }

    public async Task AddItemAsync(T item, string partitionKey)
    {
        try
        {
            await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
            _logger.LogInformation("CosmosClient Added" + item.ToString());
        }
        catch(CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogError("CosmosClient Error " + item.ToString() + " Already Exists");
        }
        catch (Exception e)
        {
            _logger.LogError("CosmosClient Error " + e.Message);
        }
    }

    public async Task<T> GetItemAsync(string id, string partitionKey)
    {
        try
        {
            ItemResponse<T> response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response;
        }
        catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError("CosmosClient Error " + id + " Not Found");
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError("CosmoClient Error " + e.Message);
            return null;
        }
    }

    public async Task<IEnumerable<T>> GetItemsAsync(string query)
    {
        var queryIterator = _container.GetItemQueryIterator<T>(new QueryDefinition(query));
        var result = new List<T>();
        while (queryIterator.HasMoreResults)
        {
            var response = await queryIterator.ReadNextAsync();
            result.AddRange(response);
        }
        return result;
    }

    public async Task DeleteItem(string id, string partitionKey)
    {
        try
        {
            await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
            _logger.LogInformation("Deleted Item " + id);
        }
        catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError("CosmosClient Error " + id + " Not Found");
        }
        catch (Exception e)
        {
            _logger.LogError("Unable to Delete " + e.Message);
        }
    }

    public async Task UpdateUser(string id, T item, string partitionKey)
    {
        try
        {
            await _container.ReplaceItemAsync(item, id, new PartitionKey(partitionKey));
            _logger.LogInformation("Item " + id + " Had been successfully updated");
        }
        catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError("CosmosClient Error " + id + " Not Found");
        }
        catch (Exception e)
        {
            _logger.LogError("Unable to Update" + e.Message);
        }
    }
}