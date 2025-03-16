FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TaskManagementSystem.API/TaskManagementSystem.API.csproj", "TaskManagementSystem.API/"]
COPY ["TaskManagementSystem.Core/TaskManagementSystem.Core.csproj", "TaskManagementSystem.Core/"]
COPY ["TaskManagementSystem.Infrastructure/TaskManagementSystem.Infrastructure.csproj", "TaskManagementSystem.Infrastructure/"]
RUN dotnet restore "TaskManagementSystem.API/TaskManagementSystem.API.csproj"
COPY . .
WORKDIR "/src/TaskManagementSystem.API"
RUN dotnet build "TaskManagementSystem.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskManagementSystem.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskManagementSystem.API.dll"]
