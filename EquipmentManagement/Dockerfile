FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EquipmentManagement.csproj", "./"]
RUN dotnet restore "EquipmentManagement.csproj"
COPY . .
RUN dotnet build "EquipmentManagement.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EquipmentManagement.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EquipmentManagement.dll"]