using Identity.API.Configuration;
using Identity.API.Models;
using IdentityServer4.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Identity.API.Data;

public class MongoIdentityContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    public MongoIdentityContext(IMongoDatabase database, MongoDbSettings settings)
    {
        _database = database;
        _settings = settings;
    }

    public IMongoCollection<ApplicationUser> Users =>
        _database.GetCollection<ApplicationUser>(_settings.UsersCollectionName);

    public IMongoCollection<ApplicationRole> Roles =>
        _database.GetCollection<ApplicationRole>(_settings.RolesCollectionName);

    public IMongoCollection<Client> Clients =>
        _database.GetCollection<Client>(_settings.ClientsCollectionName);

    public IMongoCollection<IdentityResource> IdentityResources =>
        _database.GetCollection<IdentityResource>(_settings.IdentityResourcesCollectionName);

    public IMongoCollection<ApiResource> ApiResources =>
        _database.GetCollection<ApiResource>(_settings.ApiResourcesCollectionName);

    public IMongoCollection<ApiScope> ApiScopes =>
        _database.GetCollection<ApiScope>(_settings.ApiScopesCollectionName);

    public IMongoCollection<PersistedGrant> PersistedGrants =>
        _database.GetCollection<PersistedGrant>(_settings.PersistedGrantsCollectionName);

    public IMongoCollection<DeviceFlowCodes> DeviceFlowCodes =>
        _database.GetCollection<DeviceFlowCodes>(_settings.DeviceFlowCodesCollectionName);
}

public class MongoPersistedGrantStore : IdentityServer4.Stores.IPersistedGrantStore
{
    private readonly MongoIdentityContext _context;
    private readonly ILogger<MongoPersistedGrantStore> _logger;

    public MongoPersistedGrantStore(MongoIdentityContext context, ILogger<MongoPersistedGrantStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StoreAsync(PersistedGrant grant)
    {
        try
        {
            var existingGrant = await _context.PersistedGrants
                .Find(x => x.Key == grant.Key)
                .FirstOrDefaultAsync();

            if (existingGrant == null)
            {
                await _context.PersistedGrants.InsertOneAsync(grant);
            }
            else
            {
                await _context.PersistedGrants.ReplaceOneAsync(x => x.Key == grant.Key, grant);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store persisted grant {Key}", grant.Key);
            throw;
        }
    }

    public async Task<PersistedGrant> GetAsync(string key)
    {
        try
        {
            return await _context.PersistedGrants
                .Find(x => x.Key == key)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get persisted grant {Key}", key);
            throw;
        }
    }

    public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
        try
        {
            var filterBuilder = Builders<PersistedGrant>.Filter;
            var mongoFilter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(filter.ClientId))
                mongoFilter &= filterBuilder.Eq(x => x.ClientId, filter.ClientId);

            if (!string.IsNullOrEmpty(filter.SessionId))
                mongoFilter &= filterBuilder.Eq(x => x.SessionId, filter.SessionId);

            if (!string.IsNullOrEmpty(filter.SubjectId))
                mongoFilter &= filterBuilder.Eq(x => x.SubjectId, filter.SubjectId);

            if (!string.IsNullOrEmpty(filter.Type))
                mongoFilter &= filterBuilder.Eq(x => x.Type, filter.Type);

            return await _context.PersistedGrants
                .Find(mongoFilter)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all persisted grants");
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _context.PersistedGrants.DeleteOneAsync(x => x.Key == key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove persisted grant {Key}", key);
            throw;
        }
    }

    public async Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        try
        {
            var filterBuilder = Builders<PersistedGrant>.Filter;
            var mongoFilter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(filter.ClientId))
                mongoFilter &= filterBuilder.Eq(x => x.ClientId, filter.ClientId);

            if (!string.IsNullOrEmpty(filter.SessionId))
                mongoFilter &= filterBuilder.Eq(x => x.SessionId, filter.SessionId);

            if (!string.IsNullOrEmpty(filter.SubjectId))
                mongoFilter &= filterBuilder.Eq(x => x.SubjectId, filter.SubjectId);

            if (!string.IsNullOrEmpty(filter.Type))
                mongoFilter &= filterBuilder.Eq(x => x.Type, filter.Type);

            await _context.PersistedGrants.DeleteManyAsync(mongoFilter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all persisted grants");
            throw;
        }
    }
}
