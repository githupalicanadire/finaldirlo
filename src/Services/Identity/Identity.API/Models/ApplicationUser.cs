using AspNetCore.Identity.MongoDbCore.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Identity.API.Models;

public class ApplicationUser : MongoIdentityUser<ObjectId>
{
    [BsonElement("firstName")]
    public string? FirstName { get; set; }
    
    [BsonElement("lastName")]
    public string? LastName { get; set; }
    
    [BsonElement("address")]
    public string? Address { get; set; }
    
    [BsonElement("city")]
    public string? City { get; set; }
    
    [BsonElement("country")]
    public string? Country { get; set; }
    
    [BsonElement("postalCode")]
    public string? PostalCode { get; set; }
    
    [BsonElement("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [BsonElement("lastLoginDate")]
    public DateTime? LastLoginDate { get; set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
    
    [BsonElement("profileImageUrl")]
    public string? ProfileImageUrl { get; set; }
    
    [BsonElement("phoneNumber")]
    public override string? PhoneNumber { get; set; }
    
    [BsonElement("emailConfirmed")]
    public override bool EmailConfirmed { get; set; }
    
    [BsonElement("phoneNumberConfirmed")]
    public override bool PhoneNumberConfirmed { get; set; }
    
    [BsonElement("twoFactorEnabled")]
    public override bool TwoFactorEnabled { get; set; }
    
    [BsonElement("lockoutEnd")]
    public override DateTimeOffset? LockoutEnd { get; set; }
    
    [BsonElement("lockoutEnabled")]
    public override bool LockoutEnabled { get; set; }
    
    [BsonElement("accessFailedCount")]
    public override int AccessFailedCount { get; set; }

    [BsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    [BsonIgnore]
    public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : UserName ?? Email ?? Id.ToString();
}

public class ApplicationRole : MongoIdentityRole<ObjectId>
{
    [BsonElement("description")]
    public string? Description { get; set; }
    
    [BsonElement("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
