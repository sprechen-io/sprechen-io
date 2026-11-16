FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app

COPY Sprechen.Forms/ .

EXPOSE 8100
EXPOSE 8101

ENTRYPOINT ["dotnet", "Sprechen.Forms.dll"]
