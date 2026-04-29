# Start the ASP.NET Core API and the React UI dev server in separate windows

$root = $PSScriptRoot

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\src\WeatherForecast'; dotnet run --launch-profile http"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\src\weather-ui'; npm run dev"