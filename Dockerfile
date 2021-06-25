FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

# copy and publish app and libraries
COPY . .
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GthxNetBot.dll"]
