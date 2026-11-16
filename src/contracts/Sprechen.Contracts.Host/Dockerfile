FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app

COPY Sprechen.Contracts/ .

EXPOSE 8090
EXPOSE 8091

ENTRYPOINT ["dotnet", "Sprechen.Contracts.Host.dll"]

