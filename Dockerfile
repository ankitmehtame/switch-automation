FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /source
EXPOSE 80

COPY *.csproj .
RUN dotnet restore --runtime linux-x64

COPY . .
RUN dotnet publish --no-restore -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WemoSwitchAutomation.dll"]