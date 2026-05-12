FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY events.csproj ./
RUN dotnet restore events.csproj

COPY . .
RUN dotnet publish events.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=
ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "events.dll"]
