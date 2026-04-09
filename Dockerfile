# ── Этап 1: сборка ──────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["kursProjChesh_Torgovcev/kursProjChesh_Torgovcev.csproj", "kursProjChesh_Torgovcev/"]
RUN dotnet restore "kursProjChesh_Torgovcev/kursProjChesh_Torgovcev.csproj"

COPY kursProjChesh_Torgovcev/ kursProjChesh_Torgovcev/
WORKDIR /src/kursProjChesh_Torgovcev
RUN dotnet publish -c Release -o /app/publish

# ── Этап 2: запуск ──────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "kursProjChesh_Torgovcev.dll"]
