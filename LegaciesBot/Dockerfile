# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY LegaciesBot/LegaciesBot.csproj LegaciesBot/
RUN dotnet restore LegaciesBot/LegaciesBot.csproj
COPY LegaciesBot/ LegaciesBot/
RUN dotnet publish LegaciesBot/LegaciesBot.csproj -c Release -o /app/publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# JSON data files are persisted via a volume mount, not baked into the image
VOLUME ["/app/data"]
WORKDIR /app/data
ENTRYPOINT ["dotnet", "/app/LegaciesBot.dll"]
