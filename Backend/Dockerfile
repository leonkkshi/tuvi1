# Sử dụng .NET 9.0 (10.0 chưa release)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj và restore
COPY ["Backend.csproj", "./"]
RUN dotnet restore "Backend.csproj"

# Copy tất cả và build
COPY . .
RUN dotnet build "Backend.csproj" -c Release -o /app/build
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage with Node.js
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install Node.js
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Install npm dependencies
RUN if [ -f package.json ]; then npm install --production; fi

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Backend.dll"]
