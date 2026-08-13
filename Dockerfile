# Stage 1: Build Angular Frontend
FROM node:20 AS frontend-build
WORKDIR /app/frontend
COPY Stationary.Frontend/package*.json ./
RUN npm install
COPY Stationary.Frontend/ ./
RUN npm run build -- --configuration production

# Stage 2: Build .NET API Hub
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["nuget.config", "./"]
COPY ["nupkg/", "nupkg/"]
COPY ["Stationary.ServiceHub/Stationary.ServiceHub.csproj", "Stationary.ServiceHub/"]
COPY ["Stationary.Context/Stationary.Context.csproj", "Stationary.Context/"]
COPY ["Stationary.DMO/Stationary.DMO.csproj", "Stationary.DMO/"]
COPY ["Stationary.DTO/Stationary.DTO.csproj", "Stationary.DTO/"]
RUN dotnet restore "Stationary.ServiceHub/Stationary.ServiceHub.csproj"

COPY . .

# Copy compiled Angular app into wwwroot of Stationary.ServiceHub
COPY --from=frontend-build /app/frontend/dist/StationaryFrontend/browser ./Stationary.ServiceHub/wwwroot

WORKDIR "/src/Stationary.ServiceHub"
RUN dotnet build "Stationary.ServiceHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Stationary.ServiceHub.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stationary.ServiceHub.dll"]
