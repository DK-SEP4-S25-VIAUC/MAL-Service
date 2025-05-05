using API.Services.PredictionService;
using API.Services.SensorDataService;

namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.Services.AddHttpClient<IPredictionService, PredictionService>(client =>
        {
            client.BaseAddress = new Uri("https://sep4predictionapp.azurewebsites.net/api/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            // Add API keys or headers here if needed
        });
        builder.Services.AddHttpClient<ISensorDataService, SensorDataService>(client =>
        {
            client.BaseAddress = new Uri("https://sep4api.azure-api.net/api/IoT/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            // Add API keys or headers here if needed
        });

        

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //app.UseHttpsRedirection();
        //app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
