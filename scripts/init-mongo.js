// MongoDB Initialization Script for Identity Server
print("Starting MongoDB initialization...");

// Switch to the IdentityServerDb database
db = db.getSiblingDB("IdentityServerDb");

// Create collections with proper indexing
print("Creating collections...");

// Users collection
db.createCollection("Users");
db.Users.createIndex({ UserName: 1 }, { unique: true });
db.Users.createIndex({ Email: 1 }, { unique: true });
db.Users.createIndex({ NormalizedUserName: 1 });
db.Users.createIndex({ NormalizedEmail: 1 });

// Roles collection
db.createCollection("Roles");
db.Roles.createIndex({ Name: 1 }, { unique: true });
db.Roles.createIndex({ NormalizedName: 1 });

// Identity Server collections
db.createCollection("Clients");
db.Clients.createIndex({ ClientId: 1 }, { unique: true });

db.createCollection("IdentityResources");
db.IdentityResources.createIndex({ Name: 1 }, { unique: true });

db.createCollection("ApiResources");
db.ApiResources.createIndex({ Name: 1 }, { unique: true });

db.createCollection("ApiScopes");
db.ApiScopes.createIndex({ Name: 1 }, { unique: true });

db.createCollection("PersistedGrants");
db.PersistedGrants.createIndex({ Key: 1 }, { unique: true });
db.PersistedGrants.createIndex({ SubjectId: 1 });
db.PersistedGrants.createIndex({ ClientId: 1 });
db.PersistedGrants.createIndex({ Type: 1 });
db.PersistedGrants.createIndex({ Expiration: 1 });

db.createCollection("DeviceFlowCodes");
db.DeviceFlowCodes.createIndex({ DeviceCode: 1 }, { unique: true });
db.DeviceFlowCodes.createIndex({ UserCode: 1 }, { unique: true });

// Application logs collection
db.createCollection("ApplicationLogs");
db.ApplicationLogs.createIndex({ Timestamp: 1 });
db.ApplicationLogs.createIndex({ Level: 1 });

print("Collections created with indexes");

// Insert default roles
print("Inserting default roles...");
db.Roles.insertMany([
  {
    _id: ObjectId(),
    Name: "Admin",
    NormalizedName: "ADMIN",
    Description: "Administrator role with full access",
    CreatedDate: new Date(),
    IsActive: true,
  },
  {
    _id: ObjectId(),
    Name: "Customer",
    NormalizedName: "CUSTOMER",
    Description: "Customer role for shopping",
    CreatedDate: new Date(),
    IsActive: true,
  },
  {
    _id: ObjectId(),
    Name: "User",
    NormalizedName: "USER",
    Description: "Basic user role",
    CreatedDate: new Date(),
    IsActive: true,
  },
  {
    _id: ObjectId(),
    Name: "Tester",
    NormalizedName: "TESTER",
    Description: "Tester role for QA",
    CreatedDate: new Date(),
    IsActive: true,
  },
]);

print("Default roles inserted");

// Create TTL index for expired tokens (cleanup after 30 days)
db.PersistedGrants.createIndex(
  { Expiration: 1 },
  { expireAfterSeconds: 2592000 }, // 30 days
);

print("TTL index created for PersistedGrants");

print("MongoDB initialization completed successfully!");
