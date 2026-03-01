# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

RUN dotnet restore "InitialAspireProject.ApiIdentity/InitialAspireProject.ApiIdentity.csproj"
RUN dotnet publish "InitialAspireProject.ApiIdentity/InitialAspireProject.ApiIdentity.csproj" -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "InitialAspireProject.ApiIdentity.dll"]
