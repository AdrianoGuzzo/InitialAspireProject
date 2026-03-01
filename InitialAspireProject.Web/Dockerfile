# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

# restaura e publica o projeto específico
RUN dotnet restore "InitialAspireProject.Web/InitialAspireProject.Web.csproj"
RUN dotnet publish "InitialAspireProject.Web/InitialAspireProject.Web.csproj" -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "InitialAspireProject.Web.dll"]
