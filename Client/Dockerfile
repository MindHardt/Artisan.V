FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Client/Artisan.V.Client.csproj", "Client/"]
COPY ["Shared/Artisan.V.Shared.csproj", "Shared/"]
RUN dotnet restore "Client/Artisan.V.Client.csproj"
COPY . .
WORKDIR "/src/Client"
RUN dotnet build "Artisan.V.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Artisan.V.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Artisan.V.Client.dll"]
