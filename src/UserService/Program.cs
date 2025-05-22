using Microsoft.EntityFrameworkCore;
using MyBuildingBlocks.Logger;
using Microsoft.AspNetCore.Identity;
using MyBuildingBlocks.Models.User;
using MyBuildingBlocks.JWT;
using Microsoft.IdentityModel.Tokens;
using System.Text;

//implementing custom general configuration.json
var configurationPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../configuration.json"));


var configuration = new ConfigurationBuilder()
    .AddJsonFile(configurationPath, optional: false, reloadOnChange: true)
    .Build();

if (configuration is null)
{
    throw new FileNotFoundException("configuration.json file not found.");
}


var builder = WebApplication.CreateBuilder(args);


//Connection String declaring
var connectionString = configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrEmpty(connectionString))
{
    throw new ArgumentNullException("Connection string is not set in configuration.");
}

//Db Connection
try
{
    builder.Services.AddDbContext<UserDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));
    builder.Services.AddOpenApi();
}
catch (Exception ex)
{
    throw new Exception("Error while configuring the database context.", ex);
}


var JwtKey = configuration["Jwt:Key"];

if (JwtKey == null)
{
    throw new ArgumentNullException("Jwt Key  is not set in configuration.");
}

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey))
        };
    });

//Scopes

builder.Services.AddScoped(_ => new JwtTokenService(configuration));

//Logger
var logDirectory = "../../Logs";
builder.Services.AddScoped<ILoggerService>(provider =>
    new FileLoggerService(logDirectory));

//Hasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
