# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["src/RagApi/RagApi.csproj", "src/RagApi/"]
RUN dotnet restore "src/RagApi/RagApi.csproj"

# Copy the source code
COPY ["src/RagApi/", "src/RagApi/"]

# Build the application
WORKDIR /src/src/RagApi
RUN dotnet build "RagApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RagApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files from publish stage
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RagApi.dll"]
