FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /source
EXPOSE 80

ARG TARGETPLATFORM

COPY *.csproj .
RUN dotnet restore --runtime linux-x64

COPY . .
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
        RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
        RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ] || [ "$TARGETPLATFORM" = "linux/arm/v8" ]; then \
        RID=linux-arm ; \
    fi \
    && echo "dotnet --no-restore --runtime $RID -c Release -o /app" \
    && dotnet publish --no-restore --runtime $RID -c Release -o /app

# Build runtime image
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:3.1-buster-slim
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WemoSwitchAutomation.dll"]
