# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY ["LocalRagSystem.sln", "./"]
COPY ["src/RagApi/RagApi.csproj", "src/RagApi/"]

# Restore dependencies
RUN dotnet restore "LocalRagSystem.sln"

# Copy the entire source code
COPY . .

# Build the application
WORKDIR "/src/src/RagApi"
RUN dotnet build "RagApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RagApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy published files from publish stage
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RagApi.dll"]
