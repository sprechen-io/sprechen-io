FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY Sprechen.Voice/ .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Sprechen.Voice.Host.dll"]
