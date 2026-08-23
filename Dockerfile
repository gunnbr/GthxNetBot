FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy and publish app and libraries
COPY . .
# Publish the bot project explicitly. Publishing the solution would also include the
# Aspire AppHost executable and emit ambiguous/overlapping output into /app.
RUN dotnet publish GthxNetBot/GthxNetBot.csproj -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GthxNetBot.dll"]
