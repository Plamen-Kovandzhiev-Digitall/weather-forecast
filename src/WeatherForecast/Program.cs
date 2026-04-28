
namespace WeatherForecast
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactDev", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient("open-meteo", client =>
            {
                client.BaseAddress = new Uri("https://api.open-meteo.com");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            builder.Services.AddScoped<IOpenMeteoClient, OpenMeteoClient>();
            builder.Services.AddScoped<ICityTemperatureService, CityTemperatureService>();

            var app = builder.Build();

            app.UseCors("AllowReactDev");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // PITFALLS #4: guard HTTPS redirect to avoid redirect loop in Docker/dev reverse proxy
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
