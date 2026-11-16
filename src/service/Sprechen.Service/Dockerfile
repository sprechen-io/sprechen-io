FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app

COPY Sprechen.Service/ .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Sprechen.Service.dll"]
