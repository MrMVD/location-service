# Этап 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Копируем только файлы проектов для лучшего кэширования
COPY *.csproj ./
RUN dotnet restore

# Копируем остальные файлы и собираем
COPY . ./
RUN dotnet publish -c Release -o publish

# Этап 2: Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LocationService.dll"]