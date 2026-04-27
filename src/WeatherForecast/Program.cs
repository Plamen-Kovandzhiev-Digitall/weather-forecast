
using WeatherForecast.Utilities;

namespace WeatherForecast
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var result = StringShortener.ToReadableUniqueKey("Модул за приемане и обработване на заявления за електронна идентичност:Module for Receiving and Processing Electronic Identity Applications");
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
