FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AlertaBoletaService.csproj", "."]
RUN dotnet restore "AlertaBoletaService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AlertaBoletaService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AlertaBoletaService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertaBoletaService.dll"]