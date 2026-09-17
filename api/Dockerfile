FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY api.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Render injects PORT at runtime; default to 8080 for local `docker run`.
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:$PORT dotnet api.dll"]
