using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Services;
using ConfigStream.MongoDb.Extensions;
using ConfigStream.RabbitMq.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddMongoDbStorage(
    builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017"
);

builder.Services.AddRabbitMq(builder.Configuration);

builder.Services.AddSingleton<IFileCacheService, FileCacheService>();

builder.Services.AddSingleton<IConfigurationReader>(sp =>
    new ConfigurationReader(
        applicationName: "ConfigurationLibrary.Mvc.Web",
        connectionString: builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017",
        refreshTimerIntervalInMs: 30000 // 30 seconds
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.c
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
