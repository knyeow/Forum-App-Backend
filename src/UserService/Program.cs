using Microsoft.EntityFrameworkCore;

var configurationPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../configuration.json"));


var configuration = new ConfigurationBuilder()
    .AddJsonFile(configurationPath, optional: false, reloadOnChange: true)
    .Build();

if (configuration is null)
{
    throw new FileNotFoundException("configuration.json file not found.");
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrEmpty(connectionString))
{
    throw new ArgumentNullException("Connection string is not set in configuration.");
}


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


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
