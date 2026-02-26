FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/CRM.Clients.Domain/CRM.Clients.Domain.csproj", "src/CRM.Clients.Domain/"]
COPY ["src/CRM.Clients.Application/CRM.Clients.Application.csproj", "src/CRM.Clients.Application/"]
COPY ["src/CRM.Clients.Infrastructure/CRM.Clients.Infrastructure.csproj", "src/CRM.Clients.Infrastructure/"]
COPY ["src/CRM.Clients.Api/CRM.Clients.Api.csproj", "src/CRM.Clients.Api/"]

RUN dotnet restore "src/CRM.Clients.Api/CRM.Clients.Api.csproj"

COPY . .
RUN dotnet publish "src/CRM.Clients.Api/CRM.Clients.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CRM.Clients.Api.dll"]
