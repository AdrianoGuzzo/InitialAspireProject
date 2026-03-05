# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .

RUN dotnet restore "InitialAspireProject.ApiCore/InitialAspireProject.ApiCore.csproj"
RUN dotnet publish "InitialAspireProject.ApiCore/InitialAspireProject.ApiCore.csproj" -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "InitialAspireProject.ApiCore.dll"]
