FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Stationary.ServiceHub/Stationary.ServiceHub.csproj", "Stationary.ServiceHub/"]
COPY ["Stationary.Context/Stationary.Context.csproj", "Stationary.Context/"]
COPY ["Stationary.DMO/Stationary.DMO.csproj", "Stationary.DMO/"]
COPY ["Stationary.DTO/Stationary.DTO.csproj", "Stationary.DTO/"]
RUN dotnet restore "Stationary.ServiceHub/Stationary.ServiceHub.csproj"
COPY . .
WORKDIR "/src/Stationary.ServiceHub"
RUN dotnet build "Stationary.ServiceHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Stationary.ServiceHub.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Using remote Atlas connection string from appsettings.json
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stationary.ServiceHub.dll"]
